import { createRouter, createWebHistory } from 'vue-router';
import DashboardPage from '@/pages/DashboardPage.vue';
import ChatPage from '@/pages/ChatPage.vue';
import FilesPage from '@/pages/FilesPage.vue';
import SettingsPage from '@/pages/SettingsPage.vue';

/** Map page IDs to Vue components. Add new pages here. */
export const PAGE_COMPONENTS: Record<string, ReturnType<typeof import('vue')['defineComponent']>> = {
  dashboard: DashboardPage,
  chat: ChatPage,
  files: FilesPage,
  settings: SettingsPage,
};

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/dashboard', name: 'dashboard', component: DashboardPage },
    { path: '/chat', name: 'chat', component: ChatPage },
    { path: '/files', name: 'files', component: FilesPage },
    { path: '/settings', name: 'settings', component: SettingsPage },
    { path: '/:pathMatch(.*)*', redirect: '/dashboard' },
  ],
});

export default router;
