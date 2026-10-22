import http from './axios'
import { list as people } from './people.api'

const user = () => JSON.parse(localStorage.getItem('user') || '{}')
const role = () => user().role

// دالة بتجيب الـ ID بتاع الدكتور من بياناته
// (بما إن الباك مبيرجعش الـ ID في الـ Login، هنحاول نجيبه من قائمة الدكاترة عن طريق الإيميل)
const getDoctorId = async () => {
  try {
    const email = user().name; // الـ name في الفرونت هو الإيميل
    // بنجيب كل الدكاترة ونفلتر بالإيميل عشان نعرف الـ ID
    const res = await http.get('/doctors', { params: { pageSize: 100, includeInactive: true } })
    const doc = res.data.items.find(d => d.email === email)
    return doc ? doc.id : null
  } catch {
    return null
  }
}

export const list = async () => {
  if (role() === 'doctor') {
    const docId = await getDoctorId()
    if (docId) {
      // بنجيب مواد الدكتور بس من الـ Endpoint الصح
      const res = await http.get(`/doctors/${docId}/courses`)
      return { data: res.data.map(c => ({ ...c, doctor: c.doctorName, status: c.isActive ? 'Active' : 'Disabled' })) }
    }
    // لو لسبب ما معرفناش نجيبه، نرجع مصفوفة فاضية بدل ما نبوظ الدنيا
    return { data: [] }
  } else {
    return { data: (await http.get('/courses', { params: { pageSize: 100, includeInactive: true } })).data.items.map(c => ({ ...c, doctor: c.doctorName, status: c.isActive ? 'Active' : 'Disabled' })) }
  }
}

export const doctors = () => people('doctors')

export const devices = async () => {
  // دلوقتي الدكتور مسموحله يجيب الأجهزة بعد ما عدلنا الباك إند
  const res = await http.get('/devices', { params: { includeInactive: true } })
  // هنعرض الأجهزة النشطة بس في قائمة الفتح
  return { data: res.data.map(d => ({ ...d, status: d.isActive ? 'Active' : 'Disabled' })) }
}

export const addDevice = (name, location) => http.post('/devices', { name, location })
export const addSubject = b => http.post('/courses', b)
export const updateSubject = (id, b) => http.put(`/courses/${id}`, b)

export const enrollments = async id => ({ data: (await http.get(`/courses/${id}/students`)).data.map(s => s.id) })
export const saveEnroll = async (id, ids) => {
  const cur = (await enrollments(id)).data
  const add = ids.filter(x => !cur.includes(x)), rem = cur.filter(x => !ids.includes(x))
  if (add.length) await http.post(`/courses/${id}/students`, { studentIds: add })
  for (const s of rem) await http.delete(`/courses/${id}/students/${s}`)
}