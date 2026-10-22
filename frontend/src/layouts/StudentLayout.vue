<script setup>
import B from "./BaseLayout.vue";
const nav = [
  ["/student/dashboard", "Dashboard"],
  ["/student/attendance", "My attendance"],
  ["/student/card", "My card"],
  ["/student/password", "Change password"],
];
</script>
<template><B :nav="nav" /></template>
