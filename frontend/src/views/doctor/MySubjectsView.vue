<script setup>
import { ref, onMounted } from 'vue'
import { list } from '../../api/subjects.api'
const rows = ref(null)
onMounted(async () => rows.value = (await list()).data)
</script>
<template><h1>My subjects</h1><div v-if="!rows" class="grid"><div class="sk"></div><div class="sk"></div></div>
<div v-else class="grid"><div v-for="s in rows" :key="s.id" class="card grad" v-reveal><h3>{{ s.name }}</h3><p style="color:var(--mu)">{{ s.code }}</p><router-link class="btn" to="/doctor/open" style="text-decoration:none">Open session</router-link></div></div></template>
