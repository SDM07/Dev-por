import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

vi.mock('../config', () => ({
  config: { useMocks: true, adminRole: 'Portal.Admin', apiBaseUrl: '/api' },
}));

import { AuthProvider } from '../auth/AuthProvider';
import { Layout } from '../components/Layout';
import { CatalogPage } from '../pages/CatalogPage';

function renderAt(url: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[url]}>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<CatalogPage />} />
          </Route>
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

const cardNames = () =>
  screen.getAllByRole('heading', { level: 3 }).map((h) => h.textContent?.replace(/v\d+$/, '').trim());

describe('CatalogPage', () => {
  it('muestra todas las APIs sin filtros', async () => {
    renderAt('/');
    await waitFor(() => expect(screen.getByText('6 APIs')).toBeInTheDocument());
  });

  it('filtra por tecnología al hacer clic en un chip', async () => {
    renderAt('/');
    const group = await screen.findByRole('group', { name: 'Filtrar por tecnología' });
    await userEvent.click(await within(group).findByRole('button', { name: /MuleSoft/ }));
    await waitFor(() => expect(screen.getByText('2 APIs')).toBeInTheDocument());
    expect(cardNames()).toEqual(['Orquestador de pagos', 'Conector SAP']);
  });

  it('respeta el filtro de tecnologías desde la URL', async () => {
    renderAt('/?tech=dotnet,java');
    await waitFor(() => expect(screen.getByText('3 APIs')).toBeInTheDocument());
    expect(cardNames()).toEqual(['Cuentas', 'Clientes', 'Libro mayor (legacy)']);
  });
});
