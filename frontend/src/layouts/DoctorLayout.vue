<script setup>
import B from "./BaseLayout.vue";
const nav = [
  ["/doctor/dashboard", "Dashboard"],
  ["/doctor/subjects", "My subjects"],
  ["/doctor/open", "Open session"],
  ["/doctor/live", "Live session"],
  ["/doctor/pending", "Pending cards"],
  ["/doctor/reports", "Reports"],
  ["/doctor/password", "Change password"],
];
</script>
<template><B :nav="nav" /></template>
