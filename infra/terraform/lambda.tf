# Secreto con las credenciales key-pair de Snowflake. El valor NO se gestiona en Terraform
# (para no dejar la llave privada en el state): se carga una vez con la CLI, ver README.
resource "aws_secretsmanager_secret" "snowflake" {
  name                    = "${local.name}/snowflake"
  description             = "Credenciales key-pair de Snowflake para la Lambda del Developer Portal"
  recovery_window_in_days = 7
}

resource "aws_cloudwatch_log_group" "lambda" {
  name              = "/aws/lambda/${local.name}-api"
  retention_in_days = var.log_retention_days
}

data "aws_iam_policy_document" "lambda_assume" {
  statement {
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "lambda" {
  name               = "${local.name}-api-lambda"
  assume_role_policy = data.aws_iam_policy_document.lambda_assume.json
}

data "aws_iam_policy_document" "lambda" {
  statement {
    sid       = "Logs"
    actions   = ["logs:CreateLogStream", "logs:PutLogEvents"]
    resources = ["${aws_cloudwatch_log_group.lambda.arn}:*"]
  }

  statement {
    sid       = "ReadSnowflakeSecret"
    actions   = ["secretsmanager:GetSecretValue"]
    resources = [aws_secretsmanager_secret.snowflake.arn]
  }

  statement {
    sid       = "XRay"
    actions   = ["xray:PutTraceSegments", "xray:PutTelemetryRecords"]
    resources = ["*"]
  }
}

resource "aws_iam_role_policy" "lambda" {
  name   = "${local.name}-api-lambda"
  role   = aws_iam_role.lambda.id
  policy = data.aws_iam_policy_document.lambda.json
}

resource "aws_lambda_function" "api" {
  function_name    = "${local.name}-api"
  role             = aws_iam_role.lambda.arn
  runtime          = "dotnet8"
  architectures    = ["arm64"]
  handler          = "DeveloperPortal.Api"
  filename         = var.lambda_package_path
  source_code_hash = filebase64sha256(var.lambda_package_path)
  memory_size      = var.lambda_memory_mb
  timeout          = 29 # máximo de integración de API Gateway

  reserved_concurrent_executions = var.lambda_reserved_concurrency

  environment {
    variables = {
      ASPNETCORE_ENVIRONMENT = "Production"
      # Sin el prefijo que el runtime agrega a stdout: los logs quedan como JSON puro
      # y las líneas EMF se convierten en métricas de CloudWatch.
      AWS_LAMBDA_HANDLER_LOG_FORMAT = "Unformatted"
      AzureAd__TenantId             = var.azure_tenant_id
      AzureAd__ClientId             = var.azure_client_id
      AzureAd__AdminRole            = var.azure_admin_role
      Snowflake__SecretId           = aws_secretsmanager_secret.snowflake.arn
      OriginVerify__Secret          = random_password.origin_verify.result
    }
  }

  tracing_config {
    mode = "Active"
  }

  logging_config {
    log_format = "Text"
    log_group  = aws_cloudwatch_log_group.lambda.name
  }

  depends_on = [aws_iam_role_policy.lambda]
}
