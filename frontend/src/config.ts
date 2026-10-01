export const config = {
  useMocks: import.meta.env.VITE_USE_MOCKS === 'true',
  tenantId: import.meta.env.VITE_AZURE_TENANT_ID,
  clientId: import.meta.env.VITE_AZURE_CLIENT_ID,
  apiScope: import.meta.env.VITE_API_SCOPE,
  adminRole: import.meta.env.VITE_ADMIN_ROLE ?? 'Portal.Admin',
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api',
};
