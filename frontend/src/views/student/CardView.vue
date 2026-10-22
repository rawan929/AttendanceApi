<script setup>
import { ref, onMounted } from 'vue'
import { myCard } from '../../api/students.api'
const c = ref(null)
onMounted(async () => c.value = (await myCard()).data)
const tilt = (e) => { const el = e.currentTarget, b = el.getBoundingClientRect(); el.style.transform = `perspective(700px) rotateY(${((e.clientX - b.left) / b.width - .5) * 22}deg) rotateX(${-((e.clientY - b.top) / b.height - .5) * 22}deg)` }
</script>
<template><h1>My card</h1><div v-if="!c" class="sk"></div>
<div v-else class="stage"><i></i><i></i><i></i><div :class="['rf', 'gl', c.status]" @mousemove="tilt" @mouseleave="$event.currentTarget.style.transform = ''">
<div class="row sp"><b style="font-size:20px">TapTrack</b><span class="chip"></span></div><div><div style="opacity:.85">{{ c.name }}</div><div style="font-size:22px;font-weight:600;letter-spacing:.08em">{{ c.uid }}</div></div>
<div class="row sp"><span class="pill" style="background:#ffffff33">● {{ c.status }}</span><small>Valid thru 2027</small></div></div></div></template>
