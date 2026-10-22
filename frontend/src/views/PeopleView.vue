<script setup>
import { ref, computed, watch, onMounted } from "vue";
import { useRoute } from "vue-router";
import * as api from "../api/people.api";
import DataTable from "../components/DataTable.vue";
import Modal from "../components/Modal.vue";
import { toast } from "../utils/toast";
const m = useRoute().meta,
  cred = ref(null),
  rows = ref([]),
  q = ref(""),
  pg = ref(1),
  sel = ref(null),
  form = ref(null),
  size = 6,
  load = ref(true);
const fetchAll = async () => {
  rows.value = (await api.list(m.kind)).data;
  load.value = false;
};
onMounted(fetchAll);
watch(q, () => (pg.value = 1));
const f = computed(() =>
  rows.value.filter((r) =>
    ((r.name || "") + (r.email || ""))
      .toLowerCase()
      .includes(q.value.toLowerCase())
  )
);
const pages = computed(() => Math.max(1, Math.ceil(f.value.length / size)));
const view = computed(() =>
  f.value.slice((pg.value - 1) * size, pg.value * size)
);
async function go() {
  await api.toggle(m.kind, sel.value.id, sel.value.status);
  toast("Status updated");
  sel.value = null;
  fetchAll();
}
async function save() {
  const f = form.value,
    n = f.name.trim(),
    e = (f.email || "").trim(),
    stu = m.kind === "students";
  if (n.length < 3) return toast("Enter a full name.", 1);
  if (f.id)
    await api.update(
      m.kind,
      f.id,
      stu ? { code: f.code, name: n } : { name: n }
    );
  else {
    if (!/^\S+@\S+\.\S+$/.test(e)) return toast("Enter a valid email.", 1);
    if (stu && !f.code?.trim()) return toast("Enter the student code.", 1);
    if ((f.password || "").length < 8)
      return toast("Password needs 8+ characters including a digit.", 1);
    await api.add(
      m.kind,
      stu
        ? { code: f.code.trim(), name: n, email: e, password: f.password }
        : { name: n, email: e, password: f.password }
    );
  }
  if (!f.id) cred.value = { email: e, password: f.password };
  toast(f.id ? "Changes saved" : "Account created");
  form.value = null;
  fetchAll();
}
const copyCred = () => {
  navigator.clipboard?.writeText(
    "Email: " + cred.value.email + "\nPassword: " + cred.value.password
  );
  toast("Copied");
};
</script>
<template>
  <div class="row sp">
    <h1>{{ m.title }}</h1>
    <button class="btn" @click="form = { name: '', email: '' }">
      Add {{ m.title.slice(0, -1).toLowerCase() }}
    </button>
  </div>
  <div v-if="load" class="sk"></div>
  <div v-else class="card" v-reveal>
    <input
      class="inp"
      style="max-width: 280px"
      v-model="q"
      placeholder="Search…"
    />
    <DataTable
      :cols="[
        { k: 'name', t: 'Name' },
        { k: 'email', t: 'Email' },
        { k: 'status', t: 'Status', pill: 1 },
      ]"
      plain
      :rows="view"
      ><template #act="{ row }"
        ><div class="row">
          <button class="btn g" @click="form = { ...row }">Edit</button
          ><button class="btn g" @click="sel = row">
            {{ row.status === "Active" ? "Suspend" : "Activate" }}
          </button>
        </div></template
      ></DataTable
    >
    <div class="row sp" style="margin-top: 12px">
      <small>{{ f.length }} results</small>
      <div class="row">
        <button class="btn g" :disabled="pg <= 1" @click="pg--">Prev</button
        ><span>{{ pg }} / {{ pages }}</span
        ><button class="btn g" :disabled="pg >= pages" @click="pg++">
          Next
        </button>
      </div>
    </div>
  </div>
  <Modal v-if="sel" @close="sel = null"
    ><h3>
      {{ sel.status === "Active" ? "Suspend" : "Activate" }} {{ sel.name }}?
    </h3>
    <p style="color: var(--mu)">
      Accounts are never deleted. You can reverse this anytime.
    </p>
    <div class="row sp">
      <button class="btn g" @click="sel = null">Cancel</button
      ><button class="btn" @click="go">Confirm</button>
    </div></Modal
  >
  <Modal v-if="form" @close="form = null"
    ><h3 style="margin-bottom: 14px">
      {{ form.id ? "Edit" : "Add" }} {{ m.title.slice(0, -1).toLowerCase() }}
    </h3>
    <label>Full name<input class="inp" v-model="form.name" /></label
    ><label v-if="m.kind === 'students'"
      >Student code<input class="inp" v-model="form.code" /></label
    ><template v-if="!form.id"
      ><label>Email<input class="inp" v-model="form.email" /></label
      ><label
        >Password<input
          class="inp"
          type="password"
          v-model="form.password" /></label
    ></template>
    <div class="row sp">
      <button class="btn g" @click="form = null">Cancel</button
      ><button class="btn" @click="save">Save</button>
    </div></Modal
  ><Modal v-if="cred" @close="cred = null"
    ><h3>Account created</h3>
    <p style="color: var(--warn); font-weight: 600">
      Give these login details to the user. The password won’t be shown again.
    </p>
    <label>Email<input class="inp" readonly :value="cred.email" /></label
    ><label
      >Password<input class="inp" readonly :value="cred.password"
    /></label>
    <div class="row sp">
      <button class="btn g" @click="copyCred">Copy both</button
      ><button class="btn" @click="cred = null">Done</button>
    </div></Modal
  >
</template>
