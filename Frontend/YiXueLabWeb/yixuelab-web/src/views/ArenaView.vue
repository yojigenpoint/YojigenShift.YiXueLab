<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { aiApi, casesApi } from '../api/yixueApi';
import DivinationChartHost from '../components/divination/DivinationChartHost.vue';
import {
  CaseStatus,
  DivinationType,
  type CaseDetailDto
} from '../types/yixue';

const route = useRoute();
const router = useRouter();

const selectedType = ref<DivinationType | undefined>(undefined);
const currentCase = ref<CaseDetailDto | null>(null);
const loading = ref(false);
const errorMsg = ref('');
const readingInput = ref('');
const rawDraftBeforePolish = ref<string | null>(null);
const polishing = ref(false);
const polishNotice = ref('');
const submitting = ref(false);

async function loadCase(queryCaseId?: string) {
  loading.value = true;
  errorMsg.value = '';
  if (!queryCaseId) {
    currentCase.value = null;
  }

  try {
    currentCase.value = queryCaseId
      ? await casesApi.getCaseDetail(queryCaseId)
      : await casesApi.getLatestBlindCase(selectedType.value);
  } catch (err: any) {
    currentCase.value = null;
    errorMsg.value = err.message || '当前门类暂无进行中的盲断案例';
  } finally {
    loading.value = false;
  }
}

function switchType(type?: DivinationType) {
  selectedType.value = type;
  if (route.query.caseId) {
    router.replace({ path: '/', query: {} });
  }
  currentCase.value = null;
  loadCase();
}

watch(
  () => route.query.caseId,
  (newCaseId) => {
    loadCase(newCaseId as string | undefined);
  }
);

onMounted(() => {
  loadCase(route.query.caseId as string | undefined);
});

async function handleAiPolish() {
  if (!currentCase.value) return;
  const raw = readingInput.value.trim();
  if (raw.length < 5) {
    polishNotice.value = '⚠️ 请先写下至少 5 个字的断卦思路草稿，再点击 AI 梳理';
    return;
  }

  polishing.value = true;
  polishNotice.value = '';
  try {
    // 保存润色前的原稿，支持一键撤回
    rawDraftBeforePolish.value = readingInput.value;
    const res = await aiApi.polishReading(
      currentCase.value.divinationType,
      raw,
      currentCase.value.questionText
    );
    readingInput.value = res.polishedContent;
    polishNotice.value = res.isRuleFallback
      ? '✨ 已为您按三段式标准模板梳理排版（本地教务引擎）'
      : '✨ AI 教务助手已为您完成三段式专业润色';
  } catch (err: any) {
    polishNotice.value = `⚠️ ${err.message || 'AI 梳理失败'}`;
  } finally {
    polishing.value = false;
  }
}

function handleUndoPolish() {
  if (rawDraftBeforePolish.value !== null) {
    readingInput.value = rawDraftBeforePolish.value;
    rawDraftBeforePolish.value = null;
    polishNotice.value = '↩️ 已还原为您最初的手写原稿';
  }
}

async function handleSubmitReading() {
  if (!currentCase.value || readingInput.value.trim().length < 5) return;
  submitting.value = true;
  try {
    await casesApi.submitReading(currentCase.value.id, readingInput.value.trim());
    readingInput.value = '';
    rawDraftBeforePolish.value = null;
    polishNotice.value = '';
    currentCase.value = await casesApi.getCaseDetail(currentCase.value.id);
  } catch (err: any) {
    errorMsg.value = err.message;
  } finally {
    submitting.value = false;
  }
}

async function handleLike(readingId: string) {
  if (!currentCase.value) return;
  await casesApi.likeReading(readingId);
  const target = currentCase.value.readings.find((r) => r.id === readingId);
  if (target) target.likesCount += 1;
}
</script>

<template>
  <div class="space-y-4">
    <!-- 顶部门类筛选胶囊 -->
    <div class="flex items-center justify-between bg-paper-sub p-1 rounded-xl border border-paper-border">
      <button
        @click="switchType(undefined)"
        class="flex-1 py-1.5 rounded-lg text-xs font-serif font-bold transition"
        :class="selectedType === undefined ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
      >
        最新全部
      </button>
      <button
        @click="switchType(DivinationType.Bazi)"
        class="flex-1 py-1.5 rounded-lg text-xs font-serif font-bold transition"
        :class="selectedType === DivinationType.Bazi ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
      >
        八字
      </button>
      <button
        @click="switchType(DivinationType.LiuYao)"
        class="flex-1 py-1.5 rounded-lg text-xs font-serif font-bold transition"
        :class="selectedType === DivinationType.LiuYao ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
      >
        六爻
      </button>
      <button
        @click="switchType(DivinationType.QiMen)"
        class="flex-1 py-1.5 rounded-lg text-xs font-serif font-bold transition"
        :class="selectedType === DivinationType.QiMen ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
      >
        奇门
      </button>
      <button
        @click="switchType(DivinationType.MeiHua)"
        class="flex-1 py-1.5 rounded-lg text-xs font-serif font-bold transition"
        :class="selectedType === DivinationType.MeiHua ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light'"
      >
        梅花
      </button>
    </div>

    <div v-if="loading" class="p-10 text-center text-sm text-ink-muted">正在调阅盘面...</div>

    <div v-else-if="errorMsg && !currentCase" class="bg-paper-card border border-paper-border rounded-xl p-8 text-center space-y-2">
      <div class="text-2xl">📭</div>
      <p class="text-sm text-ink-light">{{ errorMsg }}</p>
    </div>

    <template v-else-if="currentCase">
      <!-- 状态横幅 -->
      <div class="bg-paper-card border border-paper-border rounded-xl p-3.5 flex items-center justify-between">
        <div class="flex items-center gap-2.5">
          <span
            class="px-2.5 py-1 rounded-md text-white text-xs font-bold"
            :class="currentCase.status === CaseStatus.Revealed ? 'bg-jade' : 'bg-cinnabar'"
          >
            {{ currentCase.status === CaseStatus.Revealed ? '已揭晓答案' : '盲断进行中' }}
          </span>
          <div>
            <p class="text-xs text-ink-light">发布人：<b class="text-ink">{{ currentCase.publisherNickname }}</b></p>
            <p class="text-xs text-ink-muted">揭晓时间：{{ new Date(currentCase.revealTime).toLocaleString() }}</p>
          </div>
        </div>
      </div>

      <!-- 多态排盘渲染 -->
      <DivinationChartHost :payload="currentCase.divinationData" />

      <!-- 背景与盲断考题 -->
      <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-3">
        <div>
          <span class="px-2 py-0.5 rounded bg-paper-sub text-ink-light text-xs font-bold mr-2">背景提示</span>
          <span class="text-base text-ink leading-relaxed">{{ currentCase.backgroundInfo }}</span>
        </div>
        <div class="border-t border-dashed border-paper-border pt-3">
          <span class="px-2 py-0.5 rounded bg-cinnabar-light text-cinnabar text-xs font-bold mr-2">盲断考题</span>
          <span class="text-base font-bold text-cinnabar leading-relaxed">{{ currentCase.questionText }}</span>
        </div>
      </div>

      <!-- 第二层锁：真实反馈底牌 -->
      <div
        v-if="currentCase.realFeedback"
        class="rounded-xl p-4 bg-jade-light border-2 border-jade space-y-2"
      >
        <h3 class="font-serif font-bold text-jade text-base">✓ 【真实反馈底牌 · 已揭晓】</h3>
        <p class="text-base text-ink leading-relaxed bg-white/80 p-3 rounded-lg">
          {{ currentCase.realFeedback }}
        </p>
      </div>

      <!-- 交卷输入框（集成 AI 梳理排版与一键还原） -->
      <div
        v-if="!currentCase.hasCurrentUserSubmitted && currentCase.status === CaseStatus.Blind"
        class="bg-paper-card rounded-xl p-4 space-y-3 border-2 border-cinnabar/30"
      >
        <div class="flex flex-wrap items-center justify-between gap-2">
          <h3 class="font-serif font-bold text-base text-ink">✍️ 提交您的盲断分析</h3>
          <div class="flex items-center gap-1.5">
            <button
              v-if="rawDraftBeforePolish !== null"
              type="button"
              @click="handleUndoPolish"
              class="px-2.5 py-1 rounded-lg bg-paper-sub border border-paper-border text-ink-light text-xs font-medium hover:text-ink transition"
            >
              ↩️ 还原原稿
            </button>
            <button
              type="button"
              :disabled="polishing"
              @click="handleAiPolish"
              class="px-2.5 py-1 rounded-lg bg-gold-light border border-gold/40 text-gold text-xs font-bold hover:bg-gold/15 transition flex items-center gap-1"
            >
              <span>✨</span>
              <span>{{ polishing ? 'AI 梳理中...' : 'AI 梳理排版' }}</span>
            </button>
          </div>
        </div>

        <p v-if="polishNotice" class="text-xs text-gold bg-gold-light/70 px-2.5 py-1.5 rounded-lg border border-gold/20">
          {{ polishNotice }}
        </p>

        <textarea
          v-model="readingInput"
          rows="5"
          placeholder="请写下您的断卦思路（至少5个字）。写完草稿可点击右上角【✨ AI 梳理排版】自动整理为三段式专业结构，交卷后立即解锁其他易友的断语..."
          class="w-full rounded-lg bg-paper-bg border border-paper-border p-3 text-base text-ink focus:outline-none focus:border-cinnabar"
        ></textarea>

        <button
          @click="handleSubmitReading"
          :disabled="submitting"
          class="w-full py-3 rounded-xl bg-cinnabar hover:bg-cinnabar-dark text-white font-serif font-bold text-base shadow-md transition"
        >
          {{ submitting ? '正在交卷...' : '交卷并解锁群友断语' }}
        </button>
      </div>

      <!-- 第一层锁：群友断语讨论区 -->
      <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between border-b border-paper-border pb-2.5">
          <h3 class="font-serif font-bold text-base text-ink">
            群友断语切磋 ({{ currentCase.readings.length }}条)
          </h3>
        </div>

        <div
          v-if="currentCase.isReadingsLocked"
          class="py-10 px-4 text-center space-y-2 bg-paper-bg rounded-lg border border-dashed border-paper-border"
        >
          <div class="text-2xl">🔒</div>
          <h4 class="font-serif font-bold text-base text-ink">独立思考 · 盲断防跟风锁已开启</h4>
          <p class="text-sm text-ink-light">
            在您提交自己的断语之前，其他易友的分析处于隐藏状态。请在上方交卷后解锁！
          </p>
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="r in currentCase.readings"
            :key="r.id"
            class="p-3.5 rounded-xl bg-paper-bg border border-paper-border space-y-2"
          >
            <div class="flex items-center justify-between">
              <span class="font-bold text-sm text-ink">{{ r.userNickname }}</span>
              <button
                @click="handleLike(r.id)"
                class="px-2.5 py-1 rounded-full bg-paper-card border border-paper-border text-xs text-cinnabar"
              >
                👍 赞同 ({{ r.likesCount }})
              </button>
            </div>
            <p class="text-base text-ink leading-relaxed whitespace-pre-line">{{ r.content }}</p>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>