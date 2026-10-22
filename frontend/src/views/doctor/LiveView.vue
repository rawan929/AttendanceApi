<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as signalR from '@microsoft/signalr'
import * as api from '../../api/sessions.api'
import Modal from '../../components/Modal.vue'
import { toast } from '../../utils/toast'
import { confetti } from '../../utils/confetti'
const route = useRoute(), r = useRouter()
const sess = ref(null), present = ref([]), notYet = ref([]), who = ref(''), ask = ref(false), now = ref(Date.now()), empty = ref(false); let conn, tm
const closed = computed(() => sess.value?.status === 'Closed')
const elapsed = computed(() => { const s = Math.max(0, (now.value - new Date(sess.value.startsAt)) / 1000 | 0); return String(s / 60 | 0).padStart(2, '0') + ':' + String(s % 60).padStart(2, '0') })
async function load() { const d = (await api.live(sess.value?.id || id)).data; sess.value = d.session; present.value = [...d.present].reverse(); notYet.value = d.notYet; who.value ||= d.notYet[0]?.studentId }
let id = route.params.id
onMounted(async () => {
  if (!id) { id = (await api.open()).data[0]?.id; if (!id) return empty.value = true }
  await load(); tm = setInterval(() => now.value = Date.now(), 1000)
  conn = new signalR.HubConnectionBuilder().withUrl(`${import.meta.env.VITE_API_URL}/hubs/attendance`, { accessTokenFactory: () => localStorage.getItem('token') }).withAutomaticReconnect().build()
  for (const ev of ['attendanceRecorded', 'attendanceCancelled', 'sessionClosed']) conn.on(ev, load)
  conn.onreconnected(async () => { await conn.invoke('JoinSession', +id); load() })
  await conn.start(); await conn.invoke('JoinSession', +id)
})
onUnmounted(() => { clearInterval(tm); conn?.invoke('LeaveSession', +id).catch(() => {}).finally(() => conn.stop()) })
async function manual() { await api.mark(sess.value.id, who.value); toast('Marked present'); who.value = ''; load() }
async function undo(p) { await api.unmark(sess.value.id, p.studentId); toast('Manual entry removed'); load() }
async function extend() { await api.extend(sess.value.id); toast('Extended by 15 minutes'); load() }
async function close() { await api.close(sess.value.id); ask.value = false; confetti(); toast('Session closed'); setTimeout(() => r.push('/doctor/dashboard'), 1800) }
</script>
<template><h1>Live session</h1>
<div v-if="empty" class="card empty"><div style="font-size:40px">📡</div>No open session.<br><br><router-link class="btn" to="/doctor/open" style="text-decoration:none">Open a session</router-link></div>
<template v-else-if="sess">
<div class="card grad row sp" style="margin-bottom:16px"><div><h2>{{ sess.courseName }}</h2><div style="color:var(--mu)">{{ elapsed }} elapsed · ends {{ new Date(sess.endsAt).toLocaleTimeString() }} · {{ sess.deviceName }}</div></div><span :class="['pill', closed ? 'Blocked' : 'Live']"><span v-if="!closed" class="dot"></span>{{ closed ? 'Closed' : 'Live' }}</span></div>
<div class="grid"><div class="card stat"><b>{{ present.length }}</b><span>Present</span></div><div class="card stat"><b>{{ notYet.length }}</b><span>Not yet</span></div><div class="card stat"><b>{{ present.length + notYet.length }}</b><span>Enrolled</span></div></div>
<div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(300px,1fr))"><div class="card"><b>Live feed</b><div class="feed"><div v-for="p in present" :key="p.studentId"><span><b>{{ p.name }}</b> <small style="color:var(--mu)">{{ p.code }} · {{ p.method }}<template v-if="p.method === 'Manual' && !closed"> <a href="#" @click.prevent="undo(p)">undo</a></template></small></span><span class="pill Active">{{ new Date(p.recordedAt).toLocaleTimeString() }}</span></div></div></div>
<div v-if="!closed" class="card"><b>Manual attendance</b><select class="inp" v-model="who" style="margin-top:10px"><option v-for="s in notYet" :key="s.studentId" :value="s.studentId">{{ s.name }} ({{ s.code }})</option></select><button class="btn" :disabled="!who" @click="manual">Mark present</button>
<div class="row" style="margin-top:20px"><button class="btn g" @click="extend">Extend +15 min</button><button class="btn r" @click="ask = true">Close session</button></div></div></div>
</template>
<Modal v-if="ask" @close="ask = false"><h3>Close this session?</h3><p style="color:var(--mu)">Absent students will be recorded.</p><div class="row sp"><button class="btn g" @click="ask = false">Keep open</button><button class="btn r" @click="close">Close session</button></div></Modal></template>
