<script setup>
import { ref, watch, onMounted } from 'vue'
import * as rp from '../../api/reports.api'
import * as sb from '../../api/subjects.api'
import { toast } from '../../utils/toast'
const subs = ref([]), sid = ref(null), rows = ref([])
const load = async () => { if (sid.value) rows.value = (await rp.subject(sid.value)).data }
onMounted(async () => { subs.value = (await sb.list()).data; sid.value = subs.value[0]?.id; load() }); watch(sid, load)
function csv() {
  const t = 'Student,Attended,Total,Rate\n' + rows.value.map(r => [r.name, r.attended, r.total, r.rate].join(',')).join('\n')
  const a = document.createElement('a'); a.href = URL.createObjectURL(new Blob([t], { type: 'text/csv' })); a.download = 'attendance.csv'; a.click(); toast('CSV exported')
}
</script>
<template><div class="row sp"><h1>Reports</h1><button class="btn g" @click="csv">Export CSV</button></div><div class="card" v-reveal><select class="inp" style="max-width:260px" v-model="sid"><option v-for="s in subs" :key="s.id" :value="s.id">{{ s.name }}</option></select>
<div class="tw"><table><tr><th>Student</th><th>Attended</th><th>Rate</th></tr><tr v-for="r in rows" :key="r.id"><td>{{ r.name }}</td><td>{{ r.attended }} / {{ r.total }}</td><td><div class="row"><div class="bar" style="flex:1"><i :style="{ width: r.rate }"></i></div>{{ r.rate }}</div></td></tr></table></div></div></template>
