<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import * as sb from '../../api/subjects.api'
import * as se from '../../api/sessions.api'
import { toast } from '../../utils/toast'
const subs = ref([]), devs = ref([]), f = ref({ subject: null, device: null, minutes: 90 }), r = useRouter()
onMounted(async () => { subs.value = (await sb.list()).data; devs.value = (await sb.devices()).data.filter(d => d.status === 'Active'); f.value.subject = subs.value[0]?.id; f.value.device = devs.value[0]?.id })
async function go() { if (!f.value.subject || !f.value.device) return toast('Pick a course and a device.', 1); const s = (await se.start(f.value)).data; toast('Session opened'); r.push('/doctor/live/' + s.id) }
</script>
<template><h1>Open session</h1><div class="card" style="max-width:520px" v-reveal><label>Subject<select class="inp" v-model="f.subject"><option v-for="s in subs" :key="s.id" :value="s.id">{{ s.name }}</option></select></label>
<label>Device<select class="inp" v-model="f.device"><option v-for="d in devs" :key="d.id" :value="d.id">{{ d.name }}</option></select></label>
<label>Duration (minutes)<input class="inp" type="number" min="1" max="480" v-model.number="f.minutes"></label><button class="btn" @click="go">Start session</button></div></template>
