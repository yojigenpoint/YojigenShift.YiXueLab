<script setup lang="ts">
import { useRoute } from 'vue-router';
import { useAuthStore } from './stores/useAuthStore';

const authStore = useAuthStore();
const route = useRoute();
</script>

<template>
  <div
    class="min-h-screen bg-[#E5E0D5] flex flex-col items-center"
    :class="{ 'text-[108%]': authStore.isElderFontLarge }"
  >
    <div class="w-full max-w-md bg-paper-bg min-h-screen flex flex-col justify-between shadow-2xl border-x border-paper-border">
      <!-- 顶部古风导航栏 (登录页隐藏) -->
      <header
        v-if="route.name !== 'login'"
        class="bg-paper-card border-b border-paper-border px-4 py-3 sticky top-0 z-30 flex items-center justify-between"
      >
        <div class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-lg bg-cinnabar flex items-center justify-center text-white font-serif font-bold text-base">
            易
          </div>
          <div>
            <h1 class="font-serif font-bold text-base text-ink leading-tight">易学练功房</h1>
            <p class="text-[11px] text-ink-muted">内部实证 · 盲断演武</p>
          </div>
        </div>
        <button
          @click="authStore.toggleElderFont()"
          class="px-2.5 py-1 rounded-lg border border-paper-border bg-paper-sub text-xs font-medium text-ink"
        >
          Aa {{ authStore.isElderFontLarge ? '大字版' : '标准字' }}
        </button>
      </header>

      <!-- 路由视图容器 -->
      <main class="flex-1 p-3.5 pb-20">
        <router-view />
      </main>

      <!-- 底部固定导航栏 (登录页隐藏) -->
      <nav
        v-if="route.name !== 'login'"
        class="bg-paper-card border-t border-paper-border px-2 py-1.5 sticky bottom-0 z-30 grid grid-cols-4 gap-1"
      >
        <router-link to="/" class="flex flex-col items-center py-1 rounded-xl text-xs" active-class="text-cinnabar font-bold">
          <span class="text-base">⚔️</span>
          <span>演武场</span>
        </router-link>
        <router-link to="/library" class="flex flex-col items-center py-1 rounded-xl text-xs" active-class="text-cinnabar font-bold">
          <span class="text-base">📜</span>
          <span>藏经阁</span>
        </router-link>
        <router-link to="/publish" class="flex flex-col items-center py-1 rounded-xl text-xs" active-class="text-cinnabar font-bold">
          <span class="text-base">➕</span>
          <span>发布案例</span>
        </router-link>
        <router-link to="/profile" class="flex flex-col items-center py-1 rounded-xl text-xs" active-class="text-cinnabar font-bold">
          <span class="text-base">🏮</span>
          <span>通行证</span>
        </router-link>
      </nav>
    </div>
  </div>
</template>