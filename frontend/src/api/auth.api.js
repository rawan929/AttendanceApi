import http from './axios'
export const login = body => http.post('/auth/login', body) // -> { token, expiresAt, email, roles }
export const changePassword = body => http.post('/auth/change-password', body) // { currentPassword, newPassword } -> 204