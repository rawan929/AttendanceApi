import http from './axios'
const row = x => ({ ...x, status: x.isActive ? 'Active' : 'Suspended' })
// k = students | doctors. Fetches up to 100 and lets DataTable search/page on the client.
export const list = async k => ({ data: (await http.get('/' + k, { params: { pageSize: 100, includeInactive: true } })).data.items.map(row) })
export const add = (k, b) => http.post('/' + k, b)                  // students: {code,name,email,password} | doctors: {name,email,password}
export const update = (k, id, b) => http.put(`/${k}/${id}`, b)      // students: {code,name} | doctors: {name}
export const toggle = (k, id, status) => http.post(`/${k}/${id}/${status === 'Active' ? 'deactivate' : 'activate'}`)
