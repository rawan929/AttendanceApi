import axios from 'axios'
import { toast } from '../utils/toast'
const MSG = { invalid_credentials: 'Wrong email or password.', account_locked_or_disabled: 'Account suspended, or locked for 5 minutes after 5 wrong attempts.', device_has_open_session: 'This device already has an open session.', edit_window_closed_admin_only: 'The 15-minute edit window has closed. Only an admin can edit now.', only_manual_can_be_cancelled: 'Card scans cannot be cancelled.', student_already_has_active_card: 'This student already has an active card.', doctor_has_active_courses: 'This doctor still has active courses.' }
const http = axios.create({ baseURL: import.meta.env.VITE_API_URL + '/api' })
http.interceptors.request.use(c => { const t = localStorage.getItem('token'); if (t) c.headers.Authorization = 'Bearer ' + t; return c })
http.interceptors.response.use(r => r, e => {
  const d = e.response?.data, st = e.response?.status
  if (st === 401 && !e.config.url.includes('/auth/login')) { localStorage.clear(); location.href = '/login'; return Promise.reject(e) }
  const msg = d?.error ? (MSG[d.error] || d.error.replace(/_/g, ' ')) : d?.errors ? (Array.isArray(d.errors) ? d.errors : Object.values(d.errors).flat()).join(' ') : !e.response ? 'Cannot reach the server. Is the backend running?' : st === 403 ? 'You do not have permission for this.' : null
  if (msg) toast(msg, 1)
  return Promise.reject(e)
})
export default http
