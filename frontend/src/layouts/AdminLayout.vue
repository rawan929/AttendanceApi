<script setup>
import B from "./BaseLayout.vue";
const nav = [
  ["/admin/dashboard", "Dashboard"],
  ["/admin/students", "Students"],
  ["/admin/doctors", "Doctors"],
  ["/admin/subjects", "Subjects"],
  ["/admin/enrollments", "Enrollments"],
  ["/admin/devices", "Devices"],
  ["/admin/cards", "Cards"],
  ["/admin/pending", "Pending cards"],
  ["/admin/password", "Change password"],
];
</script>
<template><B :nav="nav" /></template>
