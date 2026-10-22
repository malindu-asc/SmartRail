import { lazy, type ComponentType } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { ConsoleLayout } from '@/layouts/ConsoleLayout'
import { Permissions, RequirePermission } from '@/shared/auth'
import { HomeRedirect, NoAccessPage, NotFoundPage } from '@/pages/SystemPages'

// Pages load on demand, so a controller does not download the administrator's screens.
function page<M extends Record<string, ComponentType>>(load: () => Promise<M>, name: keyof M & string) {
  return lazy(async () => ({ default: (await load())[name] as ComponentType }))
}

const DashboardPage = page(() => import('@/pages/DashboardPage'), 'DashboardPage')
const LiveBoardPage = page(() => import('@/pages/LiveBoardPage'), 'LiveBoardPage')
const JourneyDetailPage = page(() => import('@/pages/JourneyDetailPage'), 'JourneyDetailPage')
const RoutesPage = page(() => import('@/pages/RoutesPage'), 'RoutesPage')
const RouteDetailPage = page(() => import('@/pages/RouteDetailPage'), 'RouteDetailPage')
const DisruptionsPage = page(() => import('@/pages/DisruptionsPage'), 'DisruptionsPage')
const DisruptionDetailPage = page(() => import('@/pages/DisruptionDetailPage'), 'DisruptionDetailPage')
const DriverMessagesPage = page(() => import('@/pages/DriverMessagesPage'), 'DriverMessagesPage')
const PlanningPage = page(() => import('@/pages/PlanningPage'), 'PlanningPage')
const IncidentsPage = page(() => import('@/pages/IncidentsPage'), 'IncidentsPage')
const IncidentDetailPage = page(() => import('@/pages/IncidentDetailPage'), 'IncidentDetailPage')
const TimetablesPage = page(() => import('@/pages/TimetablesPage'), 'TimetablesPage')
const TimetableDetailPage = page(() => import('@/pages/TimetableDetailPage'), 'TimetableDetailPage')
const AdminAuditPage = page(() => import('@/pages/AuditPage'), 'AdminAuditPage')
const AdminDashboardPage = page(() => import('@/pages/AdminPages'), 'AdminDashboardPage')
const UsersPage = page(() => import('@/pages/AdminPages'), 'UsersPage')
const RolesPage = page(() => import('@/pages/AdminPages'), 'RolesPage')
const DevicesPage = page(() => import('@/pages/AdminPages'), 'DevicesPage')

/**
 * The only place that says which URL shows which screen and who may open it. Each group is guarded by the permission the
 * API asks for on the same data; the sidebar reads the same permissions (app/navigation.ts).
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/no-access" element={<NoAccessPage />} />
      <Route element={<ConsoleLayout />}>
        <Route index element={<HomeRedirect />} />

        <Route element={<RequirePermission permission={Permissions.OpsRead} />}>
          <Route path="live-board" element={<LiveBoardPage />} />
          <Route path="live-board/:id" element={<JourneyDetailPage />} />
          <Route path="routes" element={<RoutesPage />} />
        </Route>

        <Route element={<RequirePermission permission={Permissions.PlanningRead} />}>
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="routes/:key" element={<RouteDetailPage />} />
          <Route path="planning" element={<PlanningPage />} />
        </Route>

        <Route element={<RequirePermission permission={[Permissions.DisruptionCreate, Permissions.DisruptionCreatePlanned]} />}>
          <Route path="disruptions" element={<DisruptionsPage />} />
        </Route>
        <Route element={<RequirePermission permission={Permissions.DisruptionCreate} />}>
          <Route path="disruptions/:id" element={<DisruptionDetailPage />} />
        </Route>

        <Route element={<RequirePermission permission={Permissions.MessageSendToDriver} />}>
          <Route path="driver-messages" element={<DriverMessagesPage />} />
        </Route>

        <Route element={<RequirePermission permission={Permissions.IncidentDeclare} />}>
          <Route path="incidents" element={<IncidentsPage />} />
          <Route path="incidents/:id" element={<IncidentDetailPage />} />
        </Route>

        <Route element={<RequirePermission permission={Permissions.TimetableRead} />}>
          <Route path="timetables" element={<TimetablesPage />} />
          <Route path="timetables/:id" element={<TimetableDetailPage />} />
        </Route>

        <Route element={<RequirePermission permission={Permissions.ReportRead} />}>
        </Route>

        <Route element={<RequirePermission permission={Permissions.AuditReadOperational} />}>
        </Route>

        <Route path="admin">
          <Route element={<RequirePermission permission={Permissions.UserManage} />}>
            <Route index element={<AdminDashboardPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="roles" element={<RolesPage />} />
          </Route>
          <Route element={<RequirePermission permission={Permissions.DeviceHealthRead} />}>
            <Route path="devices" element={<DevicesPage />} />
          </Route>
          <Route element={<RequirePermission permission={Permissions.AuditReadAccess} />}>
            <Route path="audit" element={<AdminAuditPage />} />
          </Route>
        </Route>

        <Route path="live" element={<Navigate to="/live-board" replace />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
