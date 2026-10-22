<script setup>
import { ref, computed, watch } from 'vue'
const p = defineProps({ cols: Array, rows: Array, plain: Boolean, size: { type: Number, default: 8 } })
const q = ref(''), pg = ref(1)
const f = computed(() => p.plain ? p.rows : p.rows.filter(r => JSON.stringify(Object.values(r)).toLowerCase().includes(q.value.toLowerCase())))
const pages = computed(() => Math.max(1, Math.ceil(f.value.length / p.size)))
const view = computed(() => p.plain ? f.value : f.value.slice((pg.value - 1) * p.size, pg.value * p.size))
watch(q, () => pg.value = 1); watch(pages, n => { if (pg.value > n) pg.value = n })
</script>
<template><div class="tw"><input v-if="!plain && rows.length > 4" class="inp" style="max-width:260px" v-model="q" placeholder="Search…">
<table><tr><th v-for="c in cols" :key="c.k">{{ c.t }}</th><th v-if="$slots.act"></th></tr>
<tr v-for="r in view" :key="r.id"><td v-for="c in cols" :key="c.k"><span v-if="c.pill" :class="['pill', r[c.k]]">{{ r[c.k] }}</span><template v-else>{{ r[c.k] }}</template></td><td v-if="$slots.act"><slot name="act" :row="r" /></td></tr></table>
<div v-if="!view.length" class="empty"><div style="font-size:40px">📭</div>Nothing here yet.</div>
<div v-if="!plain && pages > 1" class="row sp" style="margin-top:12px"><small>{{ f.length }} results</small><div class="row"><button class="btn g" :disabled="pg <= 1" @click="pg--">Prev</button><span>{{ pg }} / {{ pages }}</span><button class="btn g" :disabled="pg >= pages" @click="pg++">Next</button></div></div></div></template>
