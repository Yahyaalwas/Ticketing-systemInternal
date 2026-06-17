import React from 'react';
import ReactDOM from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { SnackbarProvider } from 'notistack';
import { ThemeProvider } from '@/components/providers/ThemeProvider';
import { router } from '@/router';
import { ErrorBoundary } from '@/components/common/ErrorBoundary';
import '@/styles/global.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider>
          <SnackbarProvider
            maxSnack={4}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            autoHideDuration={4000}
          >
            <RouterProvider router={router} />
          </SnackbarProvider>
        </ThemeProvider>
        <ReactQueryDevtools initialIsOpen={false} />
      </QueryClientProvider>
    </ErrorBoundary>
  </React.StrictMode>
);
