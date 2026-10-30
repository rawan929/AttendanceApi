<script setup>
import { ref, onMounted } from "vue";
import * as sb from "../../api/subjects.api";
import * as api from "../../api/sessions.api";

const courses = ref([]);
const activeSessionsCount = ref(0);
const loading = ref(true);

onMounted(async () => {
  try {
    // بنجيب مواد الدكتور
    const res = await sb.list();
    courses.value = res.data;

    // بنجيب الجلسات المفتوحة حالياً للدكتور
    const sessionsRes = await api.open();
    activeSessionsCount.value = sessionsRes.data.length;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <h1>Welcome back</h1>

  <div
    v-if="loading"
    class="sk"
    style="height: 120px; border-radius: 16px"
  ></div>

  <div
    v-else
    class="grid"
    style="
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      margin-bottom: 24px;
    "
  >
    <!-- كارت عدد المواد -->
    <div class="card stat" v-reveal>
      <b>{{ courses.length }}</b>
      <span>My Subjects</span>
    </div>

    <!-- كارت الجلسات المفتوحة -->
    <div class="card stat" v-reveal>
      <b
        :style="{ color: activeSessionsCount > 0 ? 'var(--ok)' : 'inherit' }"
        >{{ activeSessionsCount }}</b
      >
      <span>Active Sessions</span>
    </div>

    <!-- زرار سريع -->
    <div
      class="card grad"
      v-reveal
      style="
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        text-align: center;
      "
    >
      <div style="font-size: 24px; margin-bottom: 8px">⚡</div>
      <router-link class="btn" to="/doctor/open" style="text-decoration: none"
        >Start New Session</router-link
      >
    </div>
  </div>

  <!-- لو فيه جلسات مفتوحة، نعرضها هنا -->
  <div v-if="activeSessionsCount > 0" class="card" v-reveal>
    <h3 style="margin-bottom: 12px">You have active sessions!</h3>
    <p style="color: var(--mu); margin-bottom: 12px">
      Go to the live view to monitor attendance.
    </p>
    <router-link class="btn" to="/doctor/live" style="text-decoration: none"
      >Go to Live Session</router-link
    >
  </div>
</template>
