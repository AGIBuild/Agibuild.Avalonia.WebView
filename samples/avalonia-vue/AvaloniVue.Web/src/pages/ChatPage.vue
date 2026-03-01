<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue';
import { Send, Trash2, Bot, User } from 'lucide-vue-next';
import { chatService, type ChatMessage } from '@/bridge/services';
import { useI18n } from '@/composables/useI18n';

const { t } = useI18n();
const messages = ref<ChatMessage[]>([]);
const input = ref('');
const sending = ref(false);
const bottomEl = ref<HTMLElement | null>(null);

onMounted(async () => {
  try {
    messages.value = await chatService.getHistory();
  } catch { /* ignore */ }
});

function scrollToBottom() {
  nextTick(() => {
    bottomEl.value?.scrollIntoView({ behavior: 'smooth' });
  });
}

async function handleSend() {
  const text = input.value.trim();
  if (!text || sending.value) return;

  input.value = '';
  sending.value = true;

  const userMsg: ChatMessage = {
    id: crypto.randomUUID(),
    role: 'user',
    content: text,
    timestamp: new Date().toISOString(),
  };
  messages.value.push(userMsg);
  scrollToBottom();

  try {
    const response = await chatService.sendMessage({ message: text });
    messages.value.push({
      id: response.id,
      role: 'assistant',
      content: response.message,
      timestamp: response.timestamp,
    });
  } catch (e) {
    messages.value.push({
      id: crypto.randomUUID(),
      role: 'assistant',
      content: `Error: ${e instanceof Error ? e.message : 'Unknown error'}`,
      timestamp: new Date().toISOString(),
    });
  } finally {
    sending.value = false;
    scrollToBottom();
  }
}

async function handleClear() {
  await chatService.clearHistory();
  messages.value = [];
}
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- Header -->
    <div class="flex items-center justify-between px-6 h-14 border-b border-gray-200 dark:border-gray-800 shrink-0">
      <div>
        <h1 class="text-lg font-bold">{{ t('chat.title') }}</h1>
        <p class="text-xs text-gray-500 dark:text-gray-400">
          {{ t('chat.subtitle') }} — <code class="bg-gray-100 dark:bg-gray-800 px-1 rounded">[JsExport] IChatService</code>
        </p>
      </div>
      <button
        @click="handleClear"
        class="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
      >
        <Trash2 class="w-3.5 h-3.5" /> {{ t('chat.clear') }}
      </button>
    </div>

    <!-- Messages -->
    <div class="flex-1 overflow-auto px-6 py-4 space-y-4">
      <div v-if="messages.length === 0" class="text-center py-16 text-gray-400 dark:text-gray-500">
        <Bot class="w-10 h-10 mx-auto mb-3 opacity-50" />
        <p class="text-sm">{{ t('chat.emptyTitle') }}</p>
        <p class="text-xs mt-1">{{ t('chat.emptyHint') }}</p>
      </div>
      <div
        v-for="msg in messages"
        :key="msg.id"
        class="flex gap-3"
        :class="msg.role === 'user' ? 'justify-end' : ''"
      >
        <div v-if="msg.role === 'assistant'" class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shrink-0">
          <Bot class="w-4 h-4 text-white" />
        </div>
        <div
          class="max-w-[70%] px-4 py-2.5 rounded-2xl text-sm whitespace-pre-wrap"
          :class="msg.role === 'user'
            ? 'bg-blue-500 text-white rounded-br-md'
            : 'bg-gray-100 dark:bg-gray-800 rounded-bl-md'"
        >
          {{ msg.content }}
        </div>
        <div v-if="msg.role === 'user'" class="w-8 h-8 rounded-full bg-gray-200 dark:bg-gray-700 flex items-center justify-center shrink-0">
          <User class="w-4 h-4" />
        </div>
      </div>
      <div v-if="sending" class="flex gap-3">
        <div class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center shrink-0">
          <Bot class="w-4 h-4 text-white" />
        </div>
        <div class="bg-gray-100 dark:bg-gray-800 px-4 py-2.5 rounded-2xl rounded-bl-md">
          <div class="flex gap-1">
            <div class="w-2 h-2 rounded-full bg-gray-400 animate-bounce" style="animation-delay: 0ms" />
            <div class="w-2 h-2 rounded-full bg-gray-400 animate-bounce" style="animation-delay: 150ms" />
            <div class="w-2 h-2 rounded-full bg-gray-400 animate-bounce" style="animation-delay: 300ms" />
          </div>
        </div>
      </div>
      <div ref="bottomEl" />
    </div>

    <!-- Input -->
    <div class="border-t border-gray-200 dark:border-gray-800 px-6 py-3 shrink-0">
      <form class="flex gap-2" @submit.prevent="handleSend">
        <input
          v-model="input"
          :placeholder="t('chat.placeholder')"
          class="flex-1 px-4 py-2.5 rounded-xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/40"
          :disabled="sending"
        />
        <button
          type="submit"
          :disabled="!input.trim() || sending"
          class="px-4 py-2.5 rounded-xl bg-blue-500 text-white text-sm font-medium disabled:opacity-40 hover:bg-blue-600 transition-colors"
        >
          <Send class="w-4 h-4" />
        </button>
      </form>
    </div>
  </div>
</template>
