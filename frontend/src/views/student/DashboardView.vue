<script setup>
import { ref, onMounted } from 'vue'
import { myAttendance } from '../../api/students.api'
const rows = ref(null)
onMounted(async () => rows.value = (await myAttendance()).data)
</script>
<template><h1>Welcome back 👋</h1><div v-if="!rows" class="grid"><div class="sk"></div><div class="sk"></div></div>
<div v-else class="grid"><div v-for="s in rows" :key="s.subject" class="card" style="text-align:center" v-reveal><div class="ring" :style="{ background: `conic-gradient(${s.pct < 75 ? 'var(--warn)' : 'var(--ok)'} ${s.pct * 3.6}deg,var(--bd) 0)` }"><b>{{ s.pct }}%</b></div><b>{{ s.subject }}</b></div></div><div class="card grad row sp" v-reveal><div><b>Your card</b><div><span class="pill Active">Active</span></div></div><router-link class="btn g" to="/student/card" style="text-decoration:none">View card</router-link></div></template>
