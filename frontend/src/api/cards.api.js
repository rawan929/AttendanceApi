import http from './axios'
export const list = async () => ({ data: (await http.get('/cards', { params: { pageSize: 100 } })).data.items.map(c => ({ id: c.id, student: c.studentName || '—', uid: c.uid, status: c.status })) })
export const act = (id, a, newUid) => a === 'replace' ? http.post(`/cards/${id}/replace`, { newUid }) : http.post(`/cards/${id}/${a}`) // a = block | activate | replace
export const link = (id, studentId) => http.post(`/cards/${id}/link`, { studentId })
export const create = (uid, studentId) => http.post('/cards', { uid, studentId })
