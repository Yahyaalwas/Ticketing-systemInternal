import { createBrowserRouter, Navigate, Outlet } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { AppLayout } from '@/components/layout/AppLayout';
import { PageSkeleton } from '@/components/common/PageSkeleton';
import { useAuthStore } from '@/stores/authStore';

const LoginPage = lazy(() => import('@/features/auth/LoginPage'));
const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'));
const ProjectListPage = lazy(() => import('@/features/projects/ProjectListPage'));
const ProjectDetailPage = lazy(() => import('@/features/projects/ProjectDetailPage'));
const CreateProjectPage = lazy(() => import('@/features/projects/CreateProjectPage'));
const TicketListPage = lazy(() => import('@/features/tickets/TicketListPage'));
const TicketDetailPage = lazy(() => import('@/features/tickets/TicketDetailPage'));
const KanbanPage = lazy(() => import('@/features/kanban/KanbanPage'));
const ReportsPage = lazy(() => import('@/features/reports/ReportsPage'));
const AdminPage = lazy(() => import('@/features/admin/AdminPage'));

function RequireAuth() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <Outlet />;
}

function S({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageSkeleton />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <S><LoginPage /></S>,
  },
  {
    element: <RequireAuth />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: 'dashboard', element: <S><DashboardPage /></S> },
          { path: 'projects', element: <S><ProjectListPage /></S> },
          { path: 'projects/new', element: <S><CreateProjectPage /></S> },
          { path: 'projects/:projectId', element: <S><ProjectDetailPage /></S> },
          { path: 'projects/:projectId/tickets', element: <S><TicketListPage /></S> },
          { path: 'projects/:projectId/board', element: <S><KanbanPage /></S> },
          { path: 'tickets/:ticketId', element: <S><TicketDetailPage /></S> },
          { path: 'reports', element: <S><ReportsPage /></S> },
          { path: 'admin', element: <S><AdminPage /></S> },
          { path: 'admin/:section', element: <S><AdminPage /></S> },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/dashboard" replace /> },
]);
