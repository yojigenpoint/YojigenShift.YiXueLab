import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/useAuthStore';
import { useDictStore } from '../stores/useDictStore';
import ArenaView from '../views/ArenaView.vue';
import LibraryView from '../views/LibraryView.vue';
import LoginView from '../views/LoginView.vue';
import ProfileView from '../views/ProfileView.vue';
import PublishView from '../views/PublishView.vue';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
    { path: '/', name: 'arena', component: ArenaView },
    { path: '/library', name: 'library', component: LibraryView },
    { path: '/publish', name: 'publish', component: PublishView },
    { path: '/profile', name: 'profile', component: ProfileView }
  ]
});

router.beforeEach(async (to) => {
  const authStore = useAuthStore();
  const dictStore = useDictStore();

  // 确保全局易学多语言字典已加载缓存
  if (!dictStore.isLoaded) {
    try {
      await dictStore.ensureLoaded();
    } catch {
      // 忽略网络启动前异常
    }
  }

  if (!to.meta.public && !authStore.isAuthenticated) {
    return { name: 'login' };
  }
});

export default router;