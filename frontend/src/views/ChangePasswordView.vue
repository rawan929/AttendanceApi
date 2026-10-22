<script setup>
import { ref } from "vue";
import * as api from "../api/auth.api";
import { toast } from "../utils/toast";
const cur = ref(""),
  nw = ref(""),
  cf = ref(""),
  busy = ref(false);
async function save() {
  if (nw.value.length < 8)
    return toast("New password needs 8+ characters including a digit.", 1);
  if (nw.value !== cf.value)
    return toast("The two new passwords do not match.", 1);
  busy.value = true;
  try {
    await api.changePassword({
      currentPassword: cur.value,
      newPassword: nw.value,
    });
    toast("Password changed");
    cur.value = nw.value = cf.value = "";
  } catch (e) {
    /* toast shown by interceptor */
  }
  busy.value = false;
}
</script>
<template>
  <h1>Change password</h1>
  <div class="card" style="max-width: 460px" v-reveal>
    <label
      >Current password<input
        class="inp"
        type="password"
        v-model="cur" /></label
    ><label
      >New password<input class="inp" type="password" v-model="nw" /></label
    ><label
      >Confirm new password<input class="inp" type="password" v-model="cf"
    /></label>
    <button class="btn" :disabled="busy" @click="save">
      {{ busy ? "Saving…" : "Update password" }}
    </button>
  </div>
</template>
