<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuth } from '../stores/auth'
defineProps({ nav: Array })
const a = useAuth(), r = useRouter(), open = ref(false), dark = ref(false)
const theme = () => { dark.value = !dark.value; document.documentElement.dataset.theme = dark.value ? 'dark' : '' }
const out = () => { a.logout(); r.push('/login') }
</script>
<template><div class="app"><aside :class="{ open }"><div class="logo"><i></i><span class="lw">TapTrack</span></div>
<router-link v-for="n in nav" :key="n[0]" :to="n[0]" class="nv" active-class="on" @click="open = false">{{ n[1] }}</router-link></aside>
<main><div class="top"><button class="btn g" id="mb" @click="open = !open">☰</button>
<div class="row"><button class="btn g" @click="theme">{{ dark ? 'Light' : 'Dark' }}</button><span class="pill Pending">{{ a.role }}</span><b class="nm">{{ a.user?.name }}</b><button class="btn g" @click="out">Sign out</button></div></div>
<div class="pg"><router-view :key="$route.fullPath" /></div></main></div></template>
