import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import './assets/style.css'
const io = new IntersectionObserver(es => es.forEach(e => { if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target) } }), { threshold: .1 })
createApp(App).use(createPinia()).use(router)
  .directive('reveal', { mounted(el, b) { el.classList.add('rv', b.value || 'u'); io.observe(el) } })
  .mount('#app')
