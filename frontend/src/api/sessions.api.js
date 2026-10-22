import http from './axios'
export const start = ({ subject, device, minutes }) => http.post('/sessions', { courseId: subject, deviceId: device, durationMinutes: minutes })
export const open = async () => ({ data: (await http.get('/sessions', { params: { status: 'Open', pageSize: 20 } })).data.items })
export const live = id => http.get(`/sessions/${id}/live`)
export const close = id => http.post(`/sessions/${id}/close`)
export const extend = (id, extraMinutes = 15) => http.post(`/sessions/${id}/extend`, { extraMinutes })
export const mark = (id, studentId) => http.post(`/sessions/${id}/attendance/manual`, { studentId })
export const unmark = (id, studentId) => http.delete(`/sessions/${id}/attendance/${studentId}`)
