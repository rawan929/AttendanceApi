import { createRouter, createWebHistory } from 'vue-router'
import { useAuth } from '../stores/auth'
// Static imports on purpose: dynamic `import(`${var}`)` breaks when the project folder name contains "(1)".
const people = (path, kind, title) => ({ path, component: () => import('../views/PeopleView.vue'), meta: { kind, title } })
const pw = { path: 'password', component: () => import('../views/ChangePasswordView.vue') }
const pend = { path: 'pending', component: () => import('../views/PendingView.vue') }
const router = createRouter({ history: createWebHistory(), routes: [
  { path: '/login', component: () => import('../views/LoginView.vue') },
  { path: '/admin', component: () => import('../layouts/AdminLayout.vue'), meta: { role: 'admin' }, redirect: '/admin/dashboard', children: [
    { path: 'dashboard', component: () => import('../views/admin/DashboardView.vue') }, people('students', 'students', 'Students'), people('doctors', 'doctors', 'Doctors'),
    { path: 'subjects', component: () => import('../views/admin/SubjectsView.vue') }, { path: 'enrollments', component: () => import('../views/admin/EnrollView.vue') },
    { path: 'devices', component: () => import('../views/admin/DevicesView.vue') }, { path: 'cards', component: () => import('../views/admin/CardsView.vue') }, pend, pw] },
  { path: '/doctor', component: () => import('../layouts/DoctorLayout.vue'), meta: { role: 'doctor' }, redirect: '/doctor/dashboard', children: [
    { path: 'dashboard', component: () => import('../views/doctor/DashboardView.vue') }, { path: 'subjects', component: () => import('../views/doctor/MySubjectsView.vue') },
    { path: 'open', component: () => import('../views/doctor/OpenView.vue') }, { path: 'live/:id?', component: () => import('../views/doctor/LiveView.vue') }, pend,
    { path: 'reports', component: () => import('../views/doctor/ReportsView.vue') }, pw] },
  { path: '/student', component: () => import('../layouts/StudentLayout.vue'), meta: { role: 'student' }, redirect: '/student/dashboard', children: [
    { path: 'dashboard', component: () => import('../views/student/DashboardView.vue') }, { path: 'attendance', component: () => import('../views/student/AttendanceView.vue') },
    { path: 'card', component: () => import('../views/student/CardView.vue') }, pw] },
  { path: '/:p(.*)*', redirect: '/login' }] })
router.beforeEach(to => {
  const a = useAuth(), need = to.matched.find(m => m.meta.role)?.meta.role
  if (to.path === '/login') return a.token ? `/${a.role}/dashboard` : true
  if (!a.token) return '/login'
  if (need && need !== a.role) return `/${a.role}/dashboard`
})
export default router