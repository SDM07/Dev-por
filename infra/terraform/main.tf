locals {
  name = "${var.project}-${var.environment}"
  tags = {
    Project     = var.project
    Environment = var.environment
    ManagedBy   = "terraform"
  }
  azure_issuer = "https://login.microsoftonline.com/${var.azure_tenant_id}/v2.0"
}

data "aws_caller_identity" "current" {}

data "aws_route53_zone" "main" {
  name         = var.hosted_zone_name
  private_zone = false
}

# Secreto compartido entre CloudFront y la Lambda: las peticiones que no traen este
# header (es decir, que no pasan por CloudFront + WAF) son rechazadas por la API.
resource "random_password" "origin_verify" {
  length  = 48
  special = false
}
