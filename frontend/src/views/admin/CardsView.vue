<script setup>
import { ref, onMounted } from "vue";
import * as api from "../../api/cards.api";
import * as st from "../../api/students.api";
import DataTable from "../../components/DataTable.vue";
import Modal from "../../components/Modal.vue";
import { toast } from "../../utils/toast";
const rows = ref([]),
  stu = ref([]),
  sel = ref(null),
  form = ref(null),
  uid = ref("");
const load = async () => (rows.value = (await api.list()).data);
onMounted(async () => {
  load();
  stu.value = (await st.list()).data.filter((s) => s.status === "Active");
});
async function go() {
  await api.act(sel.value.row.id, sel.value.a, uid.value);
  uid.value = "";
  toast("Card updated");
  sel.value = null;
  load();
}
async function link() {
  if (!/^[0-9A-F:\- ]{6,}$/i.test(form.value.uid.trim()))
    return toast("Use the UID format 04:9C:11:2B.", 1);
  await api.create(form.value.uid.trim(), form.value.studentId);
  toast("Card linked");
  form.value = null;
  load();
}
</script>
<template>
  <div class="row sp">
    <h1>Cards</h1>
  </div>
  <div class="card" v-reveal>
    <DataTable
      :cols="[
        { k: 'student', t: 'Student' },
        { k: 'uid', t: 'Card UID' },
        { k: 'status', t: 'Status', pill: 1 },
      ]"
      :rows="rows"
      ><template #act="{ row }"
        ><div class="row">
          <button
            class="btn g"
            v-if="row.status !== 'Blocked'"
            @click="sel = { row, a: 'block' }"
          >
            Block</button
          ><button class="btn g" v-else @click="sel = { row, a: 'activate' }">
            Unblock</button
          ><button class="btn g" @click="sel = { row, a: 'replace' }">
            Replace
          </button>
        </div></template
      ></DataTable
    >
  </div>
  <Modal v-if="sel" @close="sel = null"
    ><h3>
      {{ { block: "Block", replace: "Replace", activate: "Unblock" }[sel.a] }}
      card for {{ sel.row.student }}?
    </h3>
    <p style="color: var(--mu)">
      {{
        sel.a === "block"
          ? "The card stops working immediately."
          : "The old card is blocked and the new one waits to be scanned."
      }}
    </p>
    <input
      v-if="sel.a === 'replace'"
      class="inp"
      v-model="uid"
      placeholder="New card UID"
    />
    <div class="row sp">
      <button class="btn g" @click="sel = null">Cancel</button
      ><button class="btn" @click="go">Confirm</button>
    </div></Modal
  >
  <Modal v-if="form" @close="form = null"
    ><h3 style="margin-bottom: 14px">Link new card</h3>
    <label
      >Card UID<input
        class="inp"
        v-model="form.uid"
        placeholder="04:9C:11:2B" /></label
    ><label
      >Student<select class="inp" v-model="form.studentId">
        <option v-for="s in stu" :key="s.id" :value="s.id">{{ s.name }}</option>
      </select></label
    >
    <div class="row sp">
      <button class="btn g" @click="form = null">Cancel</button
      ><button class="btn" @click="link">Link card</button>
    </div></Modal
  >
</template>
