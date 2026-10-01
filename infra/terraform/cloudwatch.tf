# ---------------------------------------------------------------------------
# Notificaciones. Se necesita un tópico por región porque las métricas de
# CloudFront y WAF viven en us-east-1 y una alarma solo notifica a SNS de su región.
# ---------------------------------------------------------------------------
resource "aws_sns_topic" "alarms" {
  name = "${local.name}-alarms"
}

resource "aws_sns_topic_subscription" "alarms_email" {
  for_each  = toset(var.alarm_emails)
  topic_arn = aws_sns_topic.alarms.arn
  protocol  = "email"
  endpoint  = each.value
}

resource "aws_sns_topic" "alarms_edge" {
  provider = aws.us_east_1
  name     = "${local.name}-alarms-edge"
}

resource "aws_sns_topic_subscription" "alarms_edge_email" {
  provider  = aws.us_east_1
  for_each  = toset(var.alarm_emails)
  topic_arn = aws_sns_topic.alarms_edge.arn
  protocol  = "email"
  endpoint  = each.value
}

# ---------------------------------------------------------------------------
# Alarmas: Lambda / API Gateway / Snowflake (región principal)
# ---------------------------------------------------------------------------
locals {
  regional_alarms = {
    lambda-errors = {
      namespace   = "AWS/Lambda"
      metric      = "Errors"
      statistic   = "Sum"
      threshold   = 5
      dimensions  = { FunctionName = aws_lambda_function.api.function_name }
      description = "La Lambda del portal está fallando."
    }
    lambda-throttles = {
      namespace   = "AWS/Lambda"
      metric      = "Throttles"
      statistic   = "Sum"
      threshold   = 1
      dimensions  = { FunctionName = aws_lambda_function.api.function_name }
      description = "La Lambda alcanzó su concurrencia reservada."
    }
    api-5xx = {
      namespace   = "AWS/ApiGateway"
      metric      = "5xx"
      statistic   = "Sum"
      threshold   = 5
      dimensions  = { ApiId = aws_apigatewayv2_api.portal.id, Stage = "$default" }
      description = "API Gateway está devolviendo errores 5xx."
    }
    api-latency = {
      namespace   = "AWS/ApiGateway"
      metric      = "Latency"
      statistic   = "Average"
      threshold   = 5000
      dimensions  = { ApiId = aws_apigatewayv2_api.portal.id, Stage = "$default" }
      description = "Latencia promedio de la API por encima de 5 s."
    }
    unhandled-errors = {
      namespace   = "DeveloperPortal"
      metric      = "UnhandledErrors"
      statistic   = "Sum"
      threshold   = 3
      dimensions  = null
      description = "Errores no controlados en la API (métrica EMF)."
    }
  }
}

resource "aws_cloudwatch_metric_alarm" "regional" {
  for_each = local.regional_alarms

  alarm_name          = "${local.name}-${each.key}"
  alarm_description   = each.value.description
  namespace           = each.value.namespace
  metric_name         = each.value.metric
  statistic           = each.value.statistic
  dimensions          = each.value.dimensions
  period              = 300
  evaluation_periods  = 1
  threshold           = each.value.threshold
  comparison_operator = "GreaterThanOrEqualToThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.alarms.arn]
  ok_actions          = [aws_sns_topic.alarms.arn]
}

# Errores no controlados: filtro de logs como respaldo de la métrica EMF.
resource "aws_cloudwatch_log_metric_filter" "lambda_errors" {
  name           = "${local.name}-error-logs"
  log_group_name = aws_cloudwatch_log_group.lambda.name
  pattern        = "{ $.LogLevel = \"Error\" || $.LogLevel = \"Critical\" }"

  metric_transformation {
    name      = "ErrorLogs"
    namespace = "DeveloperPortal"
    value     = "1"
  }
}

# ---------------------------------------------------------------------------
# Alarmas: CloudFront y WAF (us-east-1)
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_metric_alarm" "cloudfront_5xx" {
  provider            = aws.us_east_1
  alarm_name          = "${local.name}-cloudfront-5xx-rate"
  alarm_description   = "Tasa de errores 5xx en CloudFront superior al 5%."
  namespace           = "AWS/CloudFront"
  metric_name         = "5xxErrorRate"
  statistic           = "Average"
  dimensions          = { DistributionId = aws_cloudfront_distribution.portal.id, Region = "Global" }
  period              = 300
  evaluation_periods  = 2
  threshold           = 5
  comparison_operator = "GreaterThanOrEqualToThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.alarms_edge.arn]
  ok_actions          = [aws_sns_topic.alarms_edge.arn]
}

resource "aws_cloudwatch_metric_alarm" "waf_blocked" {
  provider            = aws.us_east_1
  alarm_name          = "${local.name}-waf-blocked-spike"
  alarm_description   = "Pico de peticiones bloqueadas por el WAF (posible ataque)."
  namespace           = "AWS/WAFV2"
  metric_name         = "BlockedRequests"
  statistic           = "Sum"
  dimensions          = { WebACL = aws_wafv2_web_acl.portal.name, Rule = "ALL" }
  period              = 300
  evaluation_periods  = 1
  threshold           = 500
  comparison_operator = "GreaterThanOrEqualToThreshold"
  treat_missing_data  = "notBreaching"
  alarm_actions       = [aws_sns_topic.alarms_edge.arn]
}

# ---------------------------------------------------------------------------
# Dashboard
# ---------------------------------------------------------------------------
resource "aws_cloudwatch_dashboard" "portal" {
  dashboard_name = "${local.name}-portal"

  dashboard_body = jsonencode({
    widgets = [
      {
        type = "metric", x = 0, y = 0, width = 12, height = 6
        properties = {
          title  = "Lambda: invocaciones y errores"
          region = var.aws_region
          stat   = "Sum"
          metrics = [
            ["AWS/Lambda", "Invocations", "FunctionName", aws_lambda_function.api.function_name],
            [".", "Errors", ".", "."],
            [".", "Throttles", ".", "."],
          ]
        }
      },
      {
        type = "metric", x = 12, y = 0, width = 12, height = 6
        properties = {
          title  = "Lambda: duración (p50 / p95)"
          region = var.aws_region
          metrics = [
            ["AWS/Lambda", "Duration", "FunctionName", aws_lambda_function.api.function_name, { stat = "p50" }],
            ["...", { stat = "p95" }],
          ]
        }
      },
      {
        type = "metric", x = 0, y = 6, width = 12, height = 6
        properties = {
          title  = "Snowflake: duración de consultas (ms, p95)"
          region = var.aws_region
          stat   = "p95"
          metrics = [
            for op in ["ListApis", "ListTechnologies", "GetApi", "SaveApi"] :
            ["DeveloperPortal", "SnowflakeQueryDuration", "Operation", op]
          ]
        }
      },
      {
        type = "metric", x = 12, y = 6, width = 12, height = 6
        properties = {
          title  = "API Gateway: 4xx / 5xx"
          region = var.aws_region
          stat   = "Sum"
          metrics = [
            ["AWS/ApiGateway", "4xx", "ApiId", aws_apigatewayv2_api.portal.id, "Stage", "$default"],
            [".", "5xx", ".", ".", ".", "."],
          ]
        }
      },
      {
        type = "metric", x = 0, y = 12, width = 12, height = 6
        properties = {
          title  = "CloudFront: peticiones y tasa de error"
          region = "us-east-1"
          metrics = [
            ["AWS/CloudFront", "Requests", "DistributionId", aws_cloudfront_distribution.portal.id, "Region", "Global", { stat = "Sum" }],
            [".", "4xxErrorRate", ".", ".", ".", ".", { stat = "Average", yAxis = "right" }],
            [".", "5xxErrorRate", ".", ".", ".", ".", { stat = "Average", yAxis = "right" }],
          ]
        }
      },
      {
        type = "metric", x = 12, y = 12, width = 12, height = 6
        properties = {
          title  = "WAF: permitidas vs bloqueadas"
          region = "us-east-1"
          stat   = "Sum"
          metrics = [
            ["AWS/WAFV2", "AllowedRequests", "WebACL", aws_wafv2_web_acl.portal.name, "Rule", "ALL"],
            [".", "BlockedRequests", ".", ".", ".", "."],
          ]
        }
      },
      {
        type = "log", x = 0, y = 18, width = 24, height = 6
        properties = {
          title  = "Últimos errores de la API"
          region = var.aws_region
          query  = "SOURCE '${aws_cloudwatch_log_group.lambda.name}' | fields @timestamp, Message, Exception | filter LogLevel in ['Error', 'Critical'] | sort @timestamp desc | limit 50"
        }
      },
    ]
  })
}
