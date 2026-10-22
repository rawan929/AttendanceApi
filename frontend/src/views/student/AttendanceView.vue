<script setup>
import { ref, onMounted } from 'vue'
import { myAttendance, mySessions } from '../../api/students.api'
const rows = ref(null), open = ref(-1)
onMounted(async () => rows.value = (await myAttendance()).data)
async function toggle(i) { open.value = open.value === i ? -1 : i; if (open.value === i && !rows.value[i].sessions) rows.value[i].sessions = (await mySessions(rows.value[i].courseId)).data }
</script>
<template><h1>My attendance</h1><div v-if="!rows" class="sk"></div>
<div v-else><div v-for="(s, i) in rows" :key="s.subject" class="card" style="margin-bottom:14px" v-reveal><div class="row sp"><div style="flex:1;min-width:180px"><b>{{ s.subject }}</b><div class="bar" style="margin-top:8px"><i :style="{ width: s.pct + '%' }"></i></div></div><b>{{ s.pct }}%</b><button class="btn g" @click="toggle(i)">{{ open === i ? 'Hide' : 'View' }} sessions</button></div>
<div v-if="open === i" class="feed"><div v-for="x in s.sessions" :key="x.date"><span>{{ x.date }}</span><span :class="['pill', x.present ? 'Active' : 'Blocked']">{{ x.present ? 'Present' : 'Absent' }}</span></div></div></div></div></template>
