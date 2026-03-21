import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { authGuard } from '@auth0/auth0-vue'

const Home = () => import('@/views/Home.vue')
const Drafts = () => import('@/views/Drafts.vue')
const Blogs = () => import('@/views/Blogs.vue')
const Write = () => import('@/views/Write.vue')

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    beforeEnter: authGuard,
    component: Home,
  },
  {
    path: '/drafts',
    name: 'drafts',
    beforeEnter: authGuard,
    component: Drafts,
  },
  {
    path: '/blogs',
    name: 'blogs',
    beforeEnter: authGuard,
    component: Blogs,
  },
  {
    path: '/write',
    name: 'write',
    beforeEnter: authGuard,
    component: Write,
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
})

export default router
