-- =====================================================================
-- Developer Portal - Configuración de Snowflake
-- Ejecutar con un rol con privilegios de administración (SECURITYADMIN / SYSADMIN).
-- Ajusta los nombres a tus convenciones (DEV/QA/PRD).
-- =====================================================================

USE ROLE SYSADMIN;

CREATE WAREHOUSE IF NOT EXISTS PORTAL_WH
  WAREHOUSE_SIZE = 'XSMALL'
  AUTO_SUSPEND = 60
  AUTO_RESUME = TRUE
  INITIALLY_SUSPENDED = TRUE
  COMMENT = 'Warehouse del Developer Portal';

CREATE DATABASE IF NOT EXISTS DEV_PORTAL;
CREATE SCHEMA IF NOT EXISTS DEV_PORTAL.CATALOG;

USE ROLE SECURITYADMIN;

-- Rol con privilegios mínimos que usa la Lambda.
CREATE ROLE IF NOT EXISTS PORTAL_APP;
GRANT USAGE ON WAREHOUSE PORTAL_WH TO ROLE PORTAL_APP;
GRANT USAGE ON DATABASE DEV_PORTAL TO ROLE PORTAL_APP;
GRANT USAGE ON SCHEMA DEV_PORTAL.CATALOG TO ROLE PORTAL_APP;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA DEV_PORTAL.CATALOG TO ROLE PORTAL_APP;
GRANT SELECT, INSERT, UPDATE, DELETE ON FUTURE TABLES IN SCHEMA DEV_PORTAL.CATALOG TO ROLE PORTAL_APP;

-- Usuario de servicio con autenticación key-pair (sin password).
-- Genera las llaves con:
--   openssl genrsa 2048 | openssl pkcs8 -topk8 -inform PEM -out rsa_key.p8 -nocrypt
--   openssl rsa -in rsa_key.p8 -pubout -out rsa_key.pub
-- y pega la llave pública (sin cabeceras ni saltos de línea) abajo.
CREATE USER IF NOT EXISTS SVC_DEV_PORTAL
  TYPE = SERVICE
  DEFAULT_ROLE = PORTAL_APP
  DEFAULT_WAREHOUSE = PORTAL_WH
  DEFAULT_NAMESPACE = DEV_PORTAL.CATALOG
  RSA_PUBLIC_KEY = '<PEGAR_LLAVE_PUBLICA>'
  COMMENT = 'Usuario de servicio de la Lambda del Developer Portal';

GRANT ROLE PORTAL_APP TO USER SVC_DEV_PORTAL;

-- Opcional (recomendado): restringir el acceso a las IPs de salida de la Lambda (NAT Gateway).
-- CREATE NETWORK POLICY PORTAL_LAMBDA_POLICY ALLOWED_IP_LIST = ('x.x.x.x/32');
-- ALTER USER SVC_DEV_PORTAL SET NETWORK_POLICY = PORTAL_LAMBDA_POLICY;
