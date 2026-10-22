import http from './axios'
import { list as people } from './people.api'
export const list = () => people('students')
export const pending = async () => ({ data: (await http.get('/cards/pending', { params: { pageSize: 100 } })).data.items.map(c => ({ id: c.id, uid: c.uid, scanned: c.firstSeenAt })) })
export const myAttendance = async () => ({ data: (await http.get('/me/attendance')).data.map(c => ({ courseId: c.courseId, subject: c.name, pct: c.percentage })) })
export const mySessions = async id => ({ data: (await http.get(`/me/courses/${id}/sessions`)).data.map(x => ({ date: new Date(x.startsAt).toLocaleString(), present: x.present })) })
export const myCard = async () => { const d = (await http.get('/me/card')).data; return { data: { uid: d.cards[0]?.uid || '—', status: d.status === 'None' ? 'Pending' : d.status, name: JSON.parse(localStorage.getItem('user') || '{}').name } } }
