import { defineStore } from 'pinia'
import * as api from '../api/auth.api'
export const useAuth = defineStore('auth', {
  state: () => ({ token: localStorage.getItem('token'), user: JSON.parse(localStorage.getItem('user') || 'null') }),
  getters: { role: s => s.user?.role },
  actions: {
    async login(email, password) {
      const { data } = await api.login({ email, password })
      const role = data.roles.includes('Admin') ? 'admin' : data.roles.includes('Doctor') ? 'doctor' : 'student'
      this.token = data.token; this.user = { role, name: data.email }
      localStorage.setItem('token', data.token); localStorage.setItem('roles', JSON.stringify(data.roles)); localStorage.setItem('user', JSON.stringify(this.user))
    },
    logout() { this.token = null; this.user = null; localStorage.clear() }
  }
})
