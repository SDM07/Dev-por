import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthProvider';
import { Layout } from './components/Layout';
import { AdminPage } from './pages/AdminPage';
import { ApiDetailPage } from './pages/ApiDetailPage';
import { CatalogPage } from './pages/CatalogPage';

export function App() {
  return (
    <AuthProvider>
      <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<CatalogPage />} />
            <Route path="apis/:slug" element={<ApiDetailPage />} />
            <Route path="admin" element={<AdminPage />} />
            <Route path="*" element={<div className="page">Página no encontrada.</div>} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
