<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { RouterLink, useRoute } from 'vue-router';
import {
  LayoutDashboard, MessageSquare, FolderOpen,
  Settings as SettingsIcon, Moon, Sun, Menu, PanelLeftClose,
} from 'lucide-vue-next';
import type { PageDefinition, AppSettings } from '@/bridge/services';
import { appShellService, settingsService } from '@/bridge/services';
import { applySettings } from '@/utils/applySettings';
import { useI18n } from '@/composables/useI18n';
import type { TranslationKey } from '@/i18n/translations';
import type { Component } from 'vue';

const ICONS: Record<string, Component> = {
  LayoutDashboard,
  MessageSquare,
  FolderOpen,
  Settings: SettingsIcon,
};

const props = defineProps<{ pages: PageDefinition[] }>();

const { t } = useI18n();
const route = useRoute();
const collapsed = ref(false);
const dark = ref(false);
const appName = ref('Hybrid Demo');

const mainPages = computed(() => props.pages.filter((p) => p.id !== 'settings'));
const settingsPage = computed(() => props.pages.find((p) => p.id === 'settings'));

function isActive(pageRoute: string): boolean {
  return route.path === pageRoute;
}

function navClass(pageRoute: string): string {
  return isActive(pageRoute)
    ? 'bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400 font-medium'
    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800';
}

function toggleCollapsed() {
  collapsed.value = !collapsed.value;
  settingsService.getSettings().then((s) => {
    settingsService.updateSettings({ ...s, sidebarCollapsed: collapsed.value }).catch(() => {});
  }).catch(() => {});
}

function toggleDark() {
  dark.value = !dark.value;
  document.documentElement.classList.toggle('dark', dark.value);
  settingsService.getSettings().then((s) => {
    settingsService.updateSettings({ ...s, theme: dark.value ? 'dark' : 'light' }).catch(() => {});
  }).catch(() => {});
}

function showToast(message: string, type: string) {
  const container = document.getElementById('toast-container');
  if (!container) return;
  const colors: Record<string, string> = {
    info: 'bg-blue-500', success: 'bg-green-500', warning: 'bg-amber-500', error: 'bg-red-500',
  };
  const toast = document.createElement('div');
  toast.className = `${colors[type] ?? colors['info']} text-white px-4 py-2 rounded-lg shadow-lg text-sm transform transition-all duration-300 translate-y-2 opacity-0`;
  toast.textContent = message;
  container.appendChild(toast);
  requestAnimationFrame(() => toast.classList.remove('translate-y-2', 'opacity-0'));
  setTimeout(() => {
    toast.classList.add('translate-y-2', 'opacity-0');
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

const onSettingsChanged = (e: Event) => {
  const s = (e as CustomEvent<AppSettings>).detail;
  collapsed.value = s.sidebarCollapsed;
  dark.value = document.documentElement.classList.contains('dark');
};

onMounted(() => {
  settingsService.getSettings().then((s) => {
    collapsed.value = s.sidebarCollapsed;
    applySettings(s);
    dark.value = document.documentElement.classList.contains('dark');
  }).catch(() => {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    dark.value = prefersDark;
    if (prefersDark) document.documentElement.classList.add('dark');
  });

  appShellService.getAppInfo().then((info) => { appName.value = info.name; }).catch(() => {});

  window.addEventListener('app-settings-changed', onSettingsChanged);

  // Register JsImport handlers for C# → JS callbacks
  const w = window as unknown as {
    agWebView?: { rpc?: { handle: (method: string, handler: (params: unknown) => unknown) => void } };
  };
  const rpc = w.agWebView?.rpc;
  if (rpc) {
    rpc.handle('UiNotificationService.showNotification', (params: unknown) => {
      const p = params as { message?: string; type?: string };
      showToast(p.message ?? 'Notification', p.type ?? 'info');
      return undefined;
    });
    rpc.handle('ThemeService.setTheme', (params: unknown) => {
      const p = params as { theme?: string };
      const isDark = p.theme === 'dark';
      dark.value = isDark;
      document.documentElement.classList.toggle('dark', isDark);
      return undefined;
    });
  }
});

onUnmounted(() => {
  window.removeEventListener('app-settings-changed', onSettingsChanged);
});
</script>

<template>
  <div class="flex flex-col md:flex-row h-screen overflow-hidden bg-gray-50 dark:bg-gray-950">

    <!-- Mobile: Top Header -->
    <header class="flex md:hidden items-center justify-between px-4 h-12 border-b border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 shrink-0">
      <div class="flex items-center gap-2">
        <div class="w-6 h-6 rounded-md bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-[10px] font-bold shrink-0">
          A
        </div>
        <span class="text-sm font-semibold truncate">{{ appName }}</span>
      </div>
      <button
        @click="toggleDark"
        class="p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
      >
        <Sun v-if="dark" class="w-4 h-4" />
        <Moon v-else class="w-4 h-4" />
      </button>
    </header>

    <!-- Desktop: Sidebar -->
    <aside
      class="hidden md:flex flex-col border-r border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 transition-all duration-200"
      :class="collapsed ? 'w-16' : 'w-56'"
    >
      <!-- Header: Hamburger + Logo -->
      <div class="flex items-center gap-2 px-3 h-14 border-b border-gray-200 dark:border-gray-800">
        <button
          @click="toggleCollapsed"
          class="p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 shrink-0"
          :title="collapsed ? 'Expand sidebar' : 'Collapse sidebar'"
        >
          <Menu v-if="collapsed" class="w-5 h-5" />
          <PanelLeftClose v-else class="w-5 h-5" />
        </button>
        <template v-if="!collapsed">
          <div class="w-7 h-7 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
            A
          </div>
          <span class="text-sm font-semibold truncate">{{ appName }}</span>
        </template>
      </div>

      <!-- Main navigation -->
      <nav class="flex-1 py-2 space-y-0.5 px-2">
        <RouterLink
          v-for="page in mainPages"
          :key="page.id"
          :to="page.route"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors"
          :class="navClass(page.route)"
        >
          <component :is="ICONS[page.icon] ?? LayoutDashboard" class="w-5 h-5 shrink-0" />
          <span v-if="!collapsed" class="truncate">{{ t(`page.${page.id}` as TranslationKey) }}</span>
        </RouterLink>
      </nav>

      <!-- Bottom: Dark mode + Settings -->
      <div class="border-t border-gray-200 dark:border-gray-800 p-2 space-y-0.5">
        <button
          @click="toggleDark"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 w-full"
        >
          <Sun v-if="dark" class="w-5 h-5 shrink-0" />
          <Moon v-else class="w-5 h-5 shrink-0" />
          <span v-if="!collapsed">{{ dark ? t('layout.lightMode') : t('layout.darkMode') }}</span>
        </button>
        <RouterLink
          v-if="settingsPage"
          :to="settingsPage.route"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors w-full"
          :class="navClass(settingsPage.route)"
        >
          <SettingsIcon class="w-5 h-5 shrink-0" />
          <span v-if="!collapsed" class="truncate">{{ t('page.settings' as TranslationKey) }}</span>
        </RouterLink>
      </div>
    </aside>

    <!-- Main content -->
    <main class="flex-1 overflow-auto pb-14 md:pb-0">
      <router-view />
    </main>

    <!-- Mobile: Bottom Tab Bar -->
    <nav class="flex md:hidden items-stretch border-t border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 h-14 shrink-0">
      <RouterLink
        v-for="page in pages"
        :key="page.id"
        :to="page.route"
        class="flex-1 flex flex-col items-center justify-center gap-0.5 text-[10px] transition-colors"
        :class="isActive(page.route) ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'"
      >
        <component :is="ICONS[page.icon] ?? LayoutDashboard" class="w-5 h-5" />
        <span>{{ t(`page.${page.id}` as TranslationKey) }}</span>
      </RouterLink>
    </nav>

    <!-- Toast container -->
    <div id="toast-container" class="fixed bottom-18 md:bottom-4 right-4 space-y-2 z-50" />
  </div>
</template>
