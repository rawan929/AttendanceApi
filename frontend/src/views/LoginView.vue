<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../stores/auth'
import { toast } from '../utils/toast'
const email = ref('admin@attendance.local'), pw = ref('Admin@12345'), busy = ref(false), a = useAuth(), r = useRouter()
async function submit() {
  busy.value = true
  try { await a.login(email.value, pw.value); r.push(`/${a.role}/dashboard`) }
  catch (e) { /* error toast is shown by the axios interceptor */ }
  busy.value = false
}
const pu = ref(null)
const mv = e => { const el = pu.value; if (!el) return; const b = el.getBoundingClientRect(), a = Math.atan2(e.clientY - b.y, e.clientX - b.x); el.style.transform = `translate(${Math.cos(a) * 9}px,${Math.sin(a) * 6}px)` }
onMounted(() => addEventListener('mousemove', mv)); onUnmounted(() => removeEventListener('mousemove', mv))
</script>
<template><div class="login"><div class="hero"><div class="scene"><div class="gy"><b class="rg"></b><b class="rg r2"></b><b class="rg r3"></b></div><div class="orb"><div class="eye"><i ref="pu"></i></div></div><em class="oc">RFID</em></div>
<div class="brand3d"><span v-for="(c, i) in 'TapTrack'" :key="i" :style="{ '--i': i }">{{ c }}</span></div><p>Tap your card. The orb is watching, and so is your attendance.</p><div class="wave"><i></i><i></i><i></i></div></div>
<form class="fm" @submit.prevent="submit"><h2 style="font-size:26px;margin-bottom:6px">Sign in</h2><p style="color:var(--mu);margin:0 0 20px">Sign in with an account created on the backend.</p>
<label>Email<input class="inp" v-model="email"></label><label>Password<input class="inp" type="password" v-model="pw"></label>
<button class="btn" :disabled="busy">{{ busy ? 'Signing in…' : 'Sign in' }}</button></form></div></template>
