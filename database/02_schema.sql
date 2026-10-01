-- =====================================================================
-- Developer Portal - Modelo de datos del catálogo
-- =====================================================================
USE ROLE SYSADMIN;
USE SCHEMA DEV_PORTAL.CATALOG;

-- Tecnologías por las que se filtra el portal (.NET, MuleSoft, Java, ...).
CREATE TABLE IF NOT EXISTS TECHNOLOGIES (
  SLUG        VARCHAR(40)  NOT NULL PRIMARY KEY,   -- usado en la URL: ?tech=dotnet
  NAME        VARCHAR(80)  NOT NULL,
  COLOR       VARCHAR(7)   NOT NULL DEFAULT '#4f46e5',
  SORT_ORDER  NUMBER(5)    NOT NULL DEFAULT 100
);

-- APIs / integraciones del catálogo.
CREATE TABLE IF NOT EXISTS APIS (
  SLUG             VARCHAR(80)   NOT NULL PRIMARY KEY,
  NAME             VARCHAR(120)  NOT NULL,
  SUMMARY          VARCHAR(300)  NOT NULL,
  TECHNOLOGY_SLUG  VARCHAR(40)   NOT NULL REFERENCES TECHNOLOGIES (SLUG),
  CATEGORY         VARCHAR(80)   NOT NULL,
  VERSION          VARCHAR(20)   NOT NULL,
  OWNER_TEAM       VARCHAR(120)  NOT NULL,
  STATUS           VARCHAR(12)   NOT NULL DEFAULT 'STABLE',  -- STABLE | BETA | DEPRECATED
  TAGS             ARRAY,
  BASE_URL         VARCHAR(500)  NOT NULL,
  DESCRIPTION_MD   VARCHAR,                                  -- documentación en Markdown
  UPDATED_BY       VARCHAR(200),
  UPDATED_AT       TIMESTAMP_TZ  NOT NULL DEFAULT CURRENT_TIMESTAMP()
)
CLUSTER BY (TECHNOLOGY_SLUG);

-- Endpoints de cada API con sus ejemplos de request/response.
CREATE TABLE IF NOT EXISTS API_ENDPOINTS (
  ID                VARCHAR(64)   NOT NULL,
  API_SLUG          VARCHAR(80)   NOT NULL REFERENCES APIS (SLUG),
  METHOD            VARCHAR(7)    NOT NULL,
  PATH              VARCHAR(500)  NOT NULL,
  TITLE             VARCHAR(200)  NOT NULL,
  DESCRIPTION_MD    VARCHAR,
  REQUEST_EXAMPLE   VARCHAR,
  RESPONSE_EXAMPLE  VARCHAR,
  SORT_ORDER        NUMBER(5)     NOT NULL DEFAULT 0,
  PRIMARY KEY (API_SLUG, ID)
);

-- Nota: Snowflake no hace cumplir PK/FK; la API valida la existencia de la tecnología
-- antes de guardar y borra endpoints y API dentro de la misma transacción.
