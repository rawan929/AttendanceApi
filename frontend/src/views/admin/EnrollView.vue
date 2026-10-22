<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import * as sb from '../../api/subjects.api'
import * as st from '../../api/students.api'
import { toast } from '../../utils/toast'
const subs = ref([]), stu = ref([]), sid = ref(null), sel = ref([]), q = ref('')
const loadE = async () => { if (sid.value) sel.value = (await sb.enrollments(sid.value)).data }
onMounted(async () => { subs.value = (await sb.list()).data; stu.value = (await st.list()).data; sid.value = subs.value[0]?.id; loadE() }); watch(sid, loadE)
const shown = computed(() => stu.value.filter(s => s.name.toLowerCase().includes(q.value.toLowerCase())))
async function save() { await sb.saveEnroll(sid.value, sel.value); toast('Enrollment saved') }
</script>
<template><h1>Enrollments</h1><div class="card" style="max-width:560px" v-reveal><label>Subject<select class="inp" v-model="sid"><option v-for="s in subs" :key="s.id" :value="s.id">{{ s.name }}</option></select></label>
<input class="inp" v-model="q" placeholder="Search students…"><div class="feed" style="max-height:300px;overflow:auto"><div v-for="s in shown" :key="s.id"><label style="display:flex;gap:10px"><input type="checkbox" :value="s.id" v-model="sel">{{ s.name }}</label></div></div>
<div class="row sp" style="margin-top:14px"><small>{{ sel.length }} enrolled</small><button class="btn" @click="save">Save enrollment</button></div></div></template>
