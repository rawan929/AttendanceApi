<script setup>
import { ref, onMounted } from 'vue'
import * as sb from '../../api/subjects.api'
import DataTable from '../../components/DataTable.vue'
import Modal from '../../components/Modal.vue'
import { toast } from '../../utils/toast'
const rows = ref([]), docs = ref([]), form = ref(null)
const load = async () => rows.value = (await sb.list()).data
onMounted(async () => { load(); docs.value = (await sb.doctors()).data.filter(d => d.status === 'Active') })
async function save() {
  const f = form.value
  if (f.name.trim().length < 3) return toast('Enter a subject name (3+ letters).', 1)
  const b = { code: f.code, name: f.name, doctorId: f.doctorId }; if (f.id) await sb.updateSubject(f.id, b); else await sb.addSubject(b)
  toast(f.id ? 'Changes saved' : 'Subject added'); form.value = null; load()
}
</script>
<template><div class="row sp"><h1>Subjects</h1><button class="btn" @click="form = { code: '', name: '', doctorId: docs[0]?.id }">Add subject</button></div>
<div class="card" v-reveal><DataTable :cols="[{ k: 'code', t: 'Code' }, { k: 'name', t: 'Course' }, { k: 'doctor', t: 'Doctor' }, { k: 'status', t: 'Status', pill: 1 }]" :rows="rows"><template #act="{ row }"><button class="btn g" @click="form = { ...row }">Edit</button></template></DataTable></div>
<Modal v-if="form" @close="form = null"><h3 style="margin-bottom:14px">{{ form.id ? 'Edit' : 'Add' }} subject</h3><label>Course code<input class="inp" v-model="form.code"></label><label>Course name<input class="inp" v-model="form.name"></label><label>Assigned doctor<select class="inp" v-model="form.doctorId"><option v-for="d in docs" :key="d.id" :value="d.id">{{ d.name }}</option></select></label>
<div class="row sp"><button class="btn g" @click="form = null">Cancel</button><button class="btn" @click="save">Save</button></div></Modal></template>
