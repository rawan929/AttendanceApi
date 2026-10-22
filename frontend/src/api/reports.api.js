import http from './axios'
export const subject = async id => ({ data: (await http.get(`/reports/courses/${id}`)).data.students.map(s => ({ id: s.studentId, name: s.name, attended: s.attended, total: s.held, rate: s.percentage + '%' })) })
