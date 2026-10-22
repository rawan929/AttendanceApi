<script setup>
import { ref, onMounted } from 'vue'
import * as s from '../api/students.api'
import * as c from '../api/cards.api'
import DataTable from '../components/DataTable.vue'
import Modal from '../components/Modal.vue'
import { toLocal } from '../utils/date'
import { toast } from '../utils/toast'
const rows = ref([]), stu = ref([]), sel = ref(null), who = ref('')
const load = async () => rows.value = (await s.pending()).data.map(r => ({ ...r, scanned: toLocal(r.scanned) }))
onMounted(async () => { load(); try { stu.value = (await s.list()).data.filter(x => x.status === 'Active'); who.value = stu.value[0]?.id } catch {} })
async function go() { await c.link(sel.value.id, who.value); toast('Card linked'); sel.value = null; load() }
</script>
<template><h1>Pending cards</h1><div class="card" v-reveal><DataTable :cols="[{ k: 'uid', t: 'Card UID' }, { k: 'scanned', t: 'Scanned (local time)' }]" :rows="rows"><template #act="{ row }"><button class="btn" @click="sel = row">Link to student</button></template></DataTable></div>
<Modal v-if="sel" @close="sel = null"><h3 style="margin-bottom:6px">Link card {{ sel.uid }}</h3><label>Student<select class="inp" v-model="who"><option v-for="x in stu" :key="x.id" :value="x.id">{{ x.name }}</option></select></label><div class="row sp"><button class="btn g" @click="sel = null">Cancel</button><button class="btn" @click="go">Link card</button></div></Modal></template>
