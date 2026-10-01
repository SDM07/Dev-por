-- =====================================================================
-- Datos iniciales de ejemplo
-- =====================================================================
USE SCHEMA DEV_PORTAL.CATALOG;

MERGE INTO TECHNOLOGIES t
USING (
  SELECT * FROM VALUES
    ('dotnet',   '.NET',          '#512bd4', 10),
    ('mulesoft', 'MuleSoft',      '#00a0df', 20),
    ('java',     'Java / Spring', '#e76f00', 30),
    ('nodejs',   'Node.js',       '#3c873a', 40),
    ('python',   'Python',        '#3776ab', 50)
  AS s(SLUG, NAME, COLOR, SORT_ORDER)
) s ON t.SLUG = s.SLUG
WHEN MATCHED THEN UPDATE SET NAME = s.NAME, COLOR = s.COLOR, SORT_ORDER = s.SORT_ORDER
WHEN NOT MATCHED THEN INSERT (SLUG, NAME, COLOR, SORT_ORDER) VALUES (s.SLUG, s.NAME, s.COLOR, s.SORT_ORDER);

INSERT INTO APIS (SLUG, NAME, SUMMARY, TECHNOLOGY_SLUG, CATEGORY, VERSION, OWNER_TEAM, STATUS, TAGS, BASE_URL, DESCRIPTION_MD, UPDATED_BY)
SELECT 'accounts', 'Cuentas', 'Crea o conecta cuentas bancarias y consulta sus saldos.', 'dotnet', 'Core bancario', 'v2',
       'Equipo Core', 'STABLE', ARRAY_CONSTRUCT('cuentas', 'saldos'), 'https://api.empresa.com/accounts/v2',
       '## Introducción\n\nLa API de **Cuentas** permite crear cuentas nuevas o conectar cuentas existentes.\n\n### Requisitos\n\n- Token Bearer válido.',
       'seed'
WHERE NOT EXISTS (SELECT 1 FROM APIS WHERE SLUG = 'accounts');

INSERT INTO APIS (SLUG, NAME, SUMMARY, TECHNOLOGY_SLUG, CATEGORY, VERSION, OWNER_TEAM, STATUS, TAGS, BASE_URL, DESCRIPTION_MD, UPDATED_BY)
SELECT 'payments-orchestrator', 'Orquestador de pagos', 'Flujo MuleSoft para dispersión de pagos masivos.', 'mulesoft', 'Pagos', 'v1',
       'Integraciones', 'STABLE', ARRAY_CONSTRUCT('pagos', 'dispersión'), 'https://mule.empresa.com/payments/v1',
       '## Descripción\n\nOrquesta pagos masivos hacia múltiples bancos mediante **MuleSoft Anypoint**.', 'seed'
WHERE NOT EXISTS (SELECT 1 FROM APIS WHERE SLUG = 'payments-orchestrator');

INSERT INTO API_ENDPOINTS (ID, API_SLUG, METHOD, PATH, TITLE, DESCRIPTION_MD, REQUEST_EXAMPLE, RESPONSE_EXAMPLE, SORT_ORDER)
SELECT * FROM VALUES
  ('acc-1', 'accounts', 'POST', '/accounts', 'Crear o conectar una cuenta',
   'Crea una cuenta nueva o conecta una existente.',
   '{\n  "alias": "Cuenta operativa",\n  "bank_code": "007",\n  "account_number": "1234567890",\n  "connect": true\n}',
   '{\n  "id": "acc_9f8e7d",\n  "status": "pending_validation"\n}', 0),
  ('acc-2', 'accounts', 'GET', '/accounts/{id}/balance', 'Consultar saldo',
   'Devuelve el saldo disponible y contable.', NULL,
   '{\n  "available": 1520000.5,\n  "ledger": 1600000,\n  "currency": "COP"\n}', 1),
  ('pay-1', 'payments-orchestrator', 'POST', '/payouts/batch', 'Crear lote de pagos',
   'Recibe hasta 1.000 pagos por lote.',
   '{\n  "reference": "NOM-2026-09",\n  "items": [{ "account_id": "acc_1", "amount": 1000 }]\n}',
   '{\n  "batch_id": "bat_123",\n  "status": "queued"\n}', 0)
AS s(ID, API_SLUG, METHOD, PATH, TITLE, DESCRIPTION_MD, REQUEST_EXAMPLE, RESPONSE_EXAMPLE, SORT_ORDER)
WHERE NOT EXISTS (SELECT 1 FROM API_ENDPOINTS e WHERE e.API_SLUG = s.API_SLUG AND e.ID = s.ID);
