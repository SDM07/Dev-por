variable "project" {
  description = "Nombre corto del proyecto, usado como prefijo de recursos."
  type        = string
  default     = "devportal"
}

variable "environment" {
  description = "Ambiente: dev, qa, prd."
  type        = string
}

variable "aws_region" {
  description = "Región principal (Lambda, API Gateway, S3, Secrets Manager)."
  type        = string
  default     = "us-east-1"
}

variable "domain_name" {
  description = "Dominio público del portal, ej: developers.empresa.com"
  type        = string
}

variable "hosted_zone_name" {
  description = "Zona hospedada de Route 53 que contiene el dominio, ej: empresa.com"
  type        = string
}

variable "azure_tenant_id" {
  description = "Tenant ID de Azure AD (Entra ID)."
  type        = string
}

variable "azure_client_id" {
  description = "Client ID de la App Registration del portal (audiencia del access token v2)."
  type        = string
}

variable "azure_admin_role" {
  description = "App role de Azure AD que permite administrar el catálogo."
  type        = string
  default     = "Portal.Admin"
}

variable "lambda_package_path" {
  description = "Ruta al .zip publicado de la Lambda .NET 8."
  type        = string
  default     = "../../backend/artifacts/lambda.zip"
}

variable "lambda_memory_mb" {
  type    = number
  default = 1024
}

variable "lambda_reserved_concurrency" {
  description = "Concurrencia reservada (-1 = sin reserva). Limita las conexiones simultáneas a Snowflake."
  type        = number
  default     = 50
}

variable "log_retention_days" {
  type    = number
  default = 90
}

variable "alarm_emails" {
  description = "Correos que reciben las alarmas de CloudWatch."
  type        = list(string)
  default     = []
}

variable "waf_rate_limit" {
  description = "Máximo de peticiones por IP en 5 minutos antes de bloquear."
  type        = number
  default     = 2000
}

variable "waf_allowed_country_codes" {
  description = "Si no está vacío, solo se permite tráfico desde estos países (ISO 3166-1 alpha-2), ej: [\"CO\"]."
  type        = list(string)
  default     = []
}

variable "cloudfront_price_class" {
  type    = string
  default = "PriceClass_100"
}
