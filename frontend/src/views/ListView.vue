<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import DataTable from '../components/DataTable.vue'
import * as s from '../api/students.api'
import * as b from '../api/subjects.api'
import * as rp from '../api/reports.api'
import { toLocal } from '../utils/date'
const m = useRoute().meta, rows = ref([]), load = ref(true)
const src = { doctors: b.doctors, subjects: b.list, pending: s.pending, reports: rp.subject }
onMounted(async () => { rows.value = (await src[m.src]()).data.map(r => r.scanned ? { ...r, scanned: toLocal(r.scanned) } : r); load.value = false })
</script>
<template><h1>{{ m.title }}</h1><div v-if="load" class="sk"></div><div v-else class="card" v-reveal><DataTable :cols="m.cols" :rows="rows" /></div></template>
