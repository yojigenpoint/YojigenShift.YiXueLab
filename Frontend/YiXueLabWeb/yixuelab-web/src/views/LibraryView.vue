<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { casesApi } from '../api/yixueApi';
import { CaseStatus, type CaseSummaryDto } from '../types/yixue';

const router = useRouter();
const keyword = ref('');
const items = ref<CaseSummaryDto[]>([]);
const loading = ref(false);

async function search() {
  loading.value = true;
  try {
    const res = await casesApi.searchCases({
      keyword: keyword.value.trim() || undefined,
      pageIndex: 1,
      pageSize: 30
    });
    items.value = res.items;
  } finally {
    loading.value = false;
  }
}

function openCase(id: string) {
  router.push({ path: '/', query: { caseId: id } });
}

onMounted(() => search());
</script>

<template>
  <div class="space-y-4">
    <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-3">
      <h2 class="font-serif font-bold text-lg text-ink">藏经阁 · 实证案例库</h2>
      <div class="flex gap-2">
        <input
          v-model="keyword"
          @keyup.enter="search"
          type="text"
          placeholder="搜索格局、断语或反馈关键词（如：辰戌冲、项链）..."
          class="flex-1 px-3.5 py-2.5 rounded-xl bg-paper-bg border border-paper-border text-sm focus:outline-none focus:border-cinnabar"
        />
        <button
          @click="search"
          class="px-4 py-2.5 rounded-xl bg-cinnabar text-white font-serif font-bold text-sm"
        >
          检索
        </button>
      </div>
    </div>

    <div class="space-y-3">
      <div
        v-for="c in items"
        :key="c.id"
        @click="openCase(c.id)"
        class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-2 cursor-pointer hover:border-cinnabar transition"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold text-ink-light">发布人：{{ c.publisherNickname }}</span>
          <span
            class="text-xs font-medium"
            :class="c.status === CaseStatus.Revealed ? 'text-jade' : 'text-cinnabar'"
          >
            {{ c.status === CaseStatus.Revealed ? '✓ 已揭晓归档' : '🔥 盲断进行中' }}
          </span>
        </div>
        <h3 class="font-serif font-bold text-base text-ink">{{ c.questionText }}</h3>
        <p class="text-sm text-ink-light line-clamp-2">{{ c.backgroundInfo }}</p>
        <div class="flex flex-wrap gap-1 pt-1">
          <span
            v-for="t in c.tags"
            :key="t"
            class="px-2 py-0.5 rounded-full bg-paper-bg border border-paper-border text-xs text-cinnabar"
          >
            #{{ t }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>