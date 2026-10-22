<script setup>
import { ref, onMounted } from 'vue'
import * as api from '../../api/subjects.api'
import DataTable from '../../components/DataTable.vue'
import Modal from '../../components/Modal.vue'
import { toast } from '../../utils/toast'
const rows = ref([]), name = ref(''), loc = ref(''), form = ref(false), key = ref('')
const load = async () => rows.value = (await api.devices()).data
onMounted(load)
async function create() { if (!name.value.trim()) return toast('Name the device first.', 1); key.value = (await api.addDevice(name.value, loc.value)).data.apiKey; form.value = false; name.value = ''; load() }
const copy = () => { navigator.clipboard?.writeText(key.value); toast('Key copied') }
</script>
<template><div class="row sp"><h1>Devices</h1><button class="btn" @click="form = true">Add device</button></div>
<div class="card" v-reveal><DataTable :cols="[{ k: 'name', t: 'Device' }, { k: 'status', t: 'Status', pill: 1 }]" :rows="rows" /></div>
<Modal v-if="form" @close="form = false"><h3 style="margin-bottom:14px">Add device</h3><label>Device name<input class="inp" v-model="name"></label><label>Location<input class="inp" v-model="loc"></label><div class="row sp"><button class="btn g" @click="form = false">Cancel</button><button class="btn" @click="create">Create device</button></div></Modal>
<Modal v-if="key" @close="key = ''"><h3>Device created</h3><p style="color:var(--warn);font-weight:600">Save this key now — it won’t be shown again.</p><input class="inp" readonly :value="key"><div class="row sp"><button class="btn g" @click="copy">Copy key</button><button class="btn" @click="key = ''">I’ve saved it</button></div></Modal></template>
