import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const Home = () => import('@/views/Home.vue')
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: Home,
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

export default router
