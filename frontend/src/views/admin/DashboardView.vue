<script setup>
import { ref, onMounted } from 'vue'
import * as s from '../../api/students.api'
import * as b from '../../api/subjects.api'
const st = ref(null)
onMounted(async () => { const [a, d, c, e] = await Promise.all([s.list(), b.doctors(), b.list(), b.devices()]); st.value = [['Students', a.data.length], ['Doctors', d.data.length], ['Subjects', c.data.length], ['Active devices', e.data.filter(x => x.status === 'Active').length]] })
</script>
<template><h1>Dashboard</h1><div v-if="!st" class="grid"><div class="sk"></div><div class="sk"></div><div class="sk"></div></div>
<div v-else class="grid"><div v-for="x in st" :key="x[0]" class="card stat" v-reveal><b>{{ x[1] }}</b><span>{{ x[0] }}</span></div></div><div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(300px,1fr))"><div class="card" v-reveal><b>Attendance this week</b><svg viewBox="0 0 300 100" style="width:100%;margin-top:10px"><rect v-for="(h, i) in [62, 78, 71, 88, 80, 93, 86]" :key="i" :x="i * 43 + 8" :y="100 - h" width="28" :height="h" rx="7" fill="var(--p)" opacity=".8" /></svg></div><div class="card feed" v-reveal><b>Recent activity</b><div><span>Dr. Khalil opened Data Structures</span><small>5m</small></div><div><span>Card 04:9C:11:2B scanned</span><small>12m</small></div><div><span>Lab 3 Reader disabled</span><small>1h</small></div></div></div></template>
