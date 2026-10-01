using DeveloperPortal.Api.Models;

namespace DeveloperPortal.Api.Data;

/// <summary>Construcción de las consultas del catálogo sobre Snowflake.</summary>
public static class CatalogQueries
{
    private const string SummaryColumns = """
        a.SLUG, a.NAME, a.SUMMARY, a.TECHNOLOGY_SLUG, t.NAME AS TECHNOLOGY_NAME, a.CATEGORY,
        a.VERSION, a.OWNER_TEAM, a.STATUS, TO_JSON(a.TAGS) AS TAGS, a.UPDATED_AT
        """;

    public static SqlQuery ListTechnologies() => new("""
        SELECT t.SLUG, t.NAME, t.COLOR, COUNT(a.SLUG) AS API_COUNT
        FROM TECHNOLOGIES t
        LEFT JOIN APIS a ON a.TECHNOLOGY_SLUG = t.SLUG
        GROUP BY t.SLUG, t.NAME, t.COLOR, t.SORT_ORDER
        ORDER BY t.SORT_ORDER, t.NAME
        """);

    public static SqlQuery ListApis(CatalogFilter filter)
    {
        var query = new SqlQuery($"""
            SELECT {SummaryColumns}
            FROM APIS a
            JOIN TECHNOLOGIES t ON t.SLUG = a.TECHNOLOGY_SLUG
            WHERE 1 = 1
            """);

        if (filter.Technologies.Count > 0)
        {
            var placeholders = string.Join(", ", filter.Technologies.Select(_ => "?"));
            query.Append($"\n  AND a.TECHNOLOGY_SLUG IN ({placeholders})", filter.Technologies.Cast<object?>().ToArray());
        }

        if (filter.Category is not null)
        {
            query.Append("\n  AND a.CATEGORY = ?", filter.Category);
        }

        if (filter.Query is not null)
        {
            var pattern = $"%{EscapeLike(filter.Query)}%";
            query.Append("""

                  AND (a.NAME ILIKE ? ESCAPE '\\'
                    OR a.SUMMARY ILIKE ? ESCAPE '\\'
                    OR a.OWNER_TEAM ILIKE ? ESCAPE '\\'
                    OR ARRAY_TO_STRING(a.TAGS, ' ') ILIKE ? ESCAPE '\\')
                """, pattern, pattern, pattern, pattern);
        }

        return query.Append("\nORDER BY a.CATEGORY, a.NAME");
    }

    public static SqlQuery GetApi(string slug) => new SqlQuery($"""
        SELECT {SummaryColumns}, a.DESCRIPTION_MD, a.BASE_URL
        FROM APIS a
        JOIN TECHNOLOGIES t ON t.SLUG = a.TECHNOLOGY_SLUG
        WHERE a.SLUG = ?
        """).Append("", slug);

    public static SqlQuery GetEndpoints(string slug) => new SqlQuery("""
        SELECT ID, METHOD, PATH, TITLE, DESCRIPTION_MD, REQUEST_EXAMPLE, RESPONSE_EXAMPLE
        FROM API_ENDPOINTS
        WHERE API_SLUG = ?
        ORDER BY SORT_ORDER, ID
        """).Append("", slug);

    public static SqlQuery ApiExists(string slug) =>
        new SqlQuery("SELECT COUNT(*) FROM APIS WHERE SLUG = ?").Append("", slug);

    public static SqlQuery TechnologyExists(string slug) =>
        new SqlQuery("SELECT COUNT(*) FROM TECHNOLOGIES WHERE SLUG = ?").Append("", slug);

    public static SqlQuery UpsertApi(ApiUpsertRequest api, string tagsJson, string updatedBy) => new SqlQuery("""
        MERGE INTO APIS tgt
        USING (
            SELECT ? AS SLUG, ? AS NAME, ? AS SUMMARY, ? AS TECHNOLOGY_SLUG, ? AS CATEGORY, ? AS VERSION,
                   ? AS OWNER_TEAM, ? AS STATUS, PARSE_JSON(?) AS TAGS, ? AS BASE_URL, ? AS DESCRIPTION_MD,
                   ? AS UPDATED_BY
        ) src
        ON tgt.SLUG = src.SLUG
        WHEN MATCHED THEN UPDATE SET
            NAME = src.NAME, SUMMARY = src.SUMMARY, TECHNOLOGY_SLUG = src.TECHNOLOGY_SLUG, CATEGORY = src.CATEGORY,
            VERSION = src.VERSION, OWNER_TEAM = src.OWNER_TEAM, STATUS = src.STATUS, TAGS = src.TAGS,
            BASE_URL = src.BASE_URL, DESCRIPTION_MD = src.DESCRIPTION_MD,
            UPDATED_BY = src.UPDATED_BY, UPDATED_AT = CURRENT_TIMESTAMP()
        WHEN NOT MATCHED THEN INSERT
            (SLUG, NAME, SUMMARY, TECHNOLOGY_SLUG, CATEGORY, VERSION, OWNER_TEAM, STATUS, TAGS, BASE_URL,
             DESCRIPTION_MD, UPDATED_BY, UPDATED_AT)
        VALUES
            (src.SLUG, src.NAME, src.SUMMARY, src.TECHNOLOGY_SLUG, src.CATEGORY, src.VERSION, src.OWNER_TEAM,
             src.STATUS, src.TAGS, src.BASE_URL, src.DESCRIPTION_MD, src.UPDATED_BY, CURRENT_TIMESTAMP())
        """).Append("",
        api.Slug, api.Name, api.Summary, api.Technology, api.Category, api.Version,
        api.OwnerTeam, api.Status.ToString(), tagsJson, api.BaseUrl, api.DescriptionMd, updatedBy);

    public static SqlQuery DeleteEndpoints(string slug) =>
        new SqlQuery("DELETE FROM API_ENDPOINTS WHERE API_SLUG = ?").Append("", slug);

    public static SqlQuery InsertEndpoint(string slug, ApiEndpoint e, int sortOrder) => new SqlQuery("""
        INSERT INTO API_ENDPOINTS
            (ID, API_SLUG, METHOD, PATH, TITLE, DESCRIPTION_MD, REQUEST_EXAMPLE, RESPONSE_EXAMPLE, SORT_ORDER)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        """).Append("", e.Id, slug, e.Method, e.Path, e.Title, e.DescriptionMd, e.RequestExample, e.ResponseExample, sortOrder);

    public static SqlQuery DeleteApi(string slug) =>
        new SqlQuery("DELETE FROM APIS WHERE SLUG = ?").Append("", slug);

    internal static string EscapeLike(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
