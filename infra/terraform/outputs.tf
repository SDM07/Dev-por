output "portal_url" {
  value = "https://${var.domain_name}"
}

output "site_bucket" {
  description = "Bucket donde el pipeline sube el build del frontend."
  value       = aws_s3_bucket.site.bucket
}

output "cloudfront_distribution_id" {
  description = "Usado por el pipeline para invalidar la caché."
  value       = aws_cloudfront_distribution.portal.id
}

output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.portal.domain_name
}

output "api_gateway_endpoint" {
  value = aws_apigatewayv2_api.portal.api_endpoint
}

output "lambda_function_name" {
  value = aws_lambda_function.api.function_name
}

output "snowflake_secret_arn" {
  description = "Cargar aquí el JSON con las credenciales key-pair de Snowflake."
  value       = aws_secretsmanager_secret.snowflake.arn
}

output "dashboard_url" {
  value = "https://${var.aws_region}.console.aws.amazon.com/cloudwatch/home?region=${var.aws_region}#dashboards:name=${aws_cloudwatch_dashboard.portal.dashboard_name}"
}
