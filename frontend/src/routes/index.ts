import { createRouter, createWebHistory } from "vue-router";
import { authGuard } from "@auth0/auth0-vue";

const DashboardView = () => import("@/views/DashboardView.vue");
const BlogListView = () => import("@/views/BlogListView.vue");
const BlogEditorView = () => import("@/views/BlogEditorView.vue");
const PostListView = () => import("@/views/PostListView.vue");
const PostEditorView = () => import("@/views/PostEditorView.vue");
const PostView = () => import("@/views/PostView.vue");

export const router = createRouter({
  routes: [
    {
      path: "/",
      name: "Dashboard",
      beforeEnter: authGuard,
      component: DashboardView,
    },
    {
      path: "/blogs",
      name: "Blogs",
      beforeEnter: authGuard,
      component: BlogListView,
    },
    {
      path: "/blogs/new",
      name: "NewBlog",
      beforeEnter: authGuard,
      component: BlogEditorView,
    },
    {
      path: "/blogs/:id",
      name: "EditBlog",
      beforeEnter: authGuard,
      component: BlogEditorView,
    },
    {
      path: "/blogs/:blogId/posts",
      name: "Posts",
      beforeEnter: authGuard,
      component: PostListView,
    },
    {
      path: "/blogs/:blogId/posts/new",
      name: "NewPost",
      beforeEnter: authGuard,
      component: PostEditorView,
    },
    {
      path: "/blogs/:blogId/posts/:id/edit",
      name: "EditPost",
      beforeEnter: authGuard,
      component: PostEditorView,
    },
    {
      path: "/p/:slug",
      name: "PublicPost",
      component: PostView,
    },
    {
      path: "/:pathMatch(.*)*",
      redirect: "/",
    },
  ],
  history: createWebHistory(),
});
