import { useMemo } from 'react';
import { useAuth } from '../auth/AuthProvider';
import { createPortalApi } from '../api/client';

export function usePortalApi() {
  const { getAccessToken } = useAuth();
  return useMemo(() => createPortalApi(getAccessToken), [getAccessToken]);
}
