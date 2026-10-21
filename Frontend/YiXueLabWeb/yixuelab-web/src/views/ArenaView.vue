<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { casesApi } from '../api/yixueApi';
import DivinationChartHost from '../components/divination/DivinationChartHost.vue';
import { CaseStatus, DivinationType, type CaseDetailDto } from '../types/yixue';

const route = useRoute();

const selectedType = ref<DivinationType | undefined>(undefined);
const currentCase = ref<CaseDetailDto | null>(null);
const loading = ref(false);
const errorMsg = ref('');
const readingInput = ref('');
const submitting = ref(false);

const typeTabs = [
  { label: '全部最新', value: undefined },
  { label: '四柱八字', value: DivinationType.Bazi },
  { label: '六爻纳甲', value: DivinationType.LiuYao },
  { label: '奇门遁甲', value: DivinationType.QiMen },
  { label: '梅花易数', value: DivinationType.MeiHua }
];

async function loadCase() {
  loading.value = true;
  errorMsg.value = '';
  try {
    const queryCaseId = route.query.caseId as string | undefined;
    if (queryCaseId) {
      currentCase.value = await casesApi.getCaseDetail(queryCaseId);
    } else {
      currentCase.value = await casesApi.getLatestBlindCase(selectedType.value);
    }
  } catch (err: any) {
    currentCase.value = null;
    errorMsg.value = err.message || '暂无进行中的盲断案例';
  } finally {
    loading.value = false;
  }
}

async function handleSubmitReading() {
  if (!currentCase.value) return;
  if (readingInput.value.trim().length < 5) {
    errorMsg.value = '断语内容过于简短，请至少输入 5 个字的分析思路';
    return;
  }
  submitting.value = true;
  try {
    await casesApi.submitReading(currentCase.value.id, readingInput.value.trim());
    readingInput.value = '';
    // 交卷后重新拉取案例详情，自动解锁群友断语列表！
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

function selectTab(val?: DivinationType) {
  selectedType.value = val;
  loadCase();
}

watch(() => route.query.caseId, () => loadCase());
onMounted(() => loadCase());
</script>

<template>
  <div class="space-y-4">
    <!-- 门类切换胶囊 -->
    <div class="flex items-center gap-1 bg-paper-sub p-1 rounded-xl border border-paper-border overflow-x-auto no-scrollbar">
      <button
        v-for="(tab, idx) in typeTabs"
        :key="idx"
        @click="selectTab(tab.value)"
        class="flex-1 py-2 px-2.5 rounded-lg text-xs font-serif font-bold shrink-0 transition"
        :class="selectedType === tab.value ? 'bg-cinnabar text-white shadow-sm' : 'text-ink-light hover:text-ink'"
      >
        {{ tab.label }}
      </button>
    </div>

    <div v-if="loading" class="bg-paper-card border border-paper-border rounded-xl p-10 text-center text-sm text-ink-muted">
      正在调取排盘卷宗...
    </div>

    <div v-else-if="!currentCase" class="bg-paper-card border border-paper-border rounded-xl p-8 text-center space-y-2">
      <p class="text-base font-serif font-bold text-ink">{{ errorMsg || '当前暂无盲断考题' }}</p>
      <p class="text-xs text-ink-muted">您可以前往【发布案例】录入一道新盲断题，或去【藏经阁】翻阅历史归档</p>
    </div>

    <template v-else>
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
            <p class="text-xs text-ink-light">
              发布人：<span class="font-medium text-ink">{{ currentCase.publisherNickname }}</span>
            </p>
            <p class="text-xs text-ink-muted">
              起盘时间：{{ new Date(currentCase.divinationData.castTimeUtc).toLocaleString() }}
            </p>
          </div>
        </div>
        <div class="text-right">
          <p class="text-xs text-ink-muted">开奖时间</p>
          <p class="text-xs font-mono font-bold text-cinnabar">
            {{ new Date(currentCase.revealTime).toLocaleDateString() }}
          </p>
        </div>
      </div>

      <!-- 动态专业排盘渲染 -->
      <DivinationChartHost :payload="currentCase.divinationData" />

      <!-- 背景与盲断问题 -->
      <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-3">
        <div class="flex items-start gap-2">
          <span class="px-2 py-0.5 rounded bg-paper-sub border border-paper-border text-ink-light text-xs font-bold shrink-0 mt-0.5">
            背景提示
          </span>
          <p class="text-base text-ink leading-relaxed">{{ currentCase.backgroundInfo }}</p>
        </div>

        <div class="border-t border-dashed border-paper-border pt-3 flex items-start gap-2">
          <span class="px-2 py-0.5 rounded bg-cinnabar-light border border-cinnabar/30 text-cinnabar text-xs font-bold shrink-0 mt-0.5">
            盲断考题
          </span>
          <p class="text-base font-bold text-cinnabar leading-relaxed">{{ currentCase.questionText }}</p>
        </div>

        <div v-if="currentCase.tags.length > 0" class="pt-1 flex flex-wrap gap-1.5">
          <span
            v-for="tag in currentCase.tags"
            :key="tag"
            class="px-2 py-0.5 rounded-full bg-paper-sub text-ink-light text-xs"
          >
            #{{ tag }}
          </span>
        </div>
      </div>

      <!-- 【第二层锁】：真实反馈底牌区 (后端在未揭晓时强制返回 null) -->
      <div
        v-if="currentCase.realFeedback"
        class="rounded-xl p-4 bg-jade-light border-2 border-jade space-y-2 shadow-sm"
      >
        <div class="flex items-center justify-between">
          <h3 class="font-serif font-bold text-jade text-base">✓ 【真实反馈底牌 · 已揭晓】</h3>
          <span class="text-xs text-jade font-medium">已归入藏经阁</span>
        </div>
        <p class="text-base text-ink leading-relaxed font-medium bg-white/80 p-3 rounded-lg border border-jade/20 whitespace-pre-line">
          {{ currentCase.realFeedback }}
        </p>
      </div>

      <!-- 交卷输入框 (当前用户尚未提交时显示) -->
      <div
        v-if="!currentCase.hasCurrentUserSubmitted && currentCase.status === CaseStatus.Blind"
        class="bg-paper-card rounded-xl p-4 space-y-3 border-2 border-cinnabar/30"
      >
        <h3 class="font-serif font-bold text-base text-ink">✍️ 提交您的盲断分析</h3>
        <textarea
          v-model="readingInput"
          rows="4"
          placeholder="请写下您的断卦思路（至少5个字）。提交后将立即解锁查看群内其他易友的断语..."
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

      <!-- 【第一层锁】：群友断语切磋区 -->
      <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-3">
        <div class="flex items-center justify-between border-b border-paper-border pb-2.5">
          <h3 class="font-serif font-bold text-base text-ink">
            群友断语切磋 ({{ currentCase.readings.length }}条)
          </h3>
          <span class="text-xs text-ink-muted">按获赞高低排序</span>
        </div>

        <!-- 未交卷时的防跟风锁 -->
        <div
          v-if="currentCase.isReadingsLocked"
          class="py-10 px-4 text-center space-y-3 bg-paper-bg rounded-lg border border-dashed border-paper-border"
        >
          <div class="w-12 h-12 rounded-full bg-cinnabar-light text-cinnabar mx-auto flex items-center justify-center text-2xl">
            🔒
          </div>
          <h4 class="font-serif font-bold text-base text-ink">独立思考 · 盲断防跟风锁已开启</h4>
          <p class="text-sm text-ink-light max-w-xs mx-auto leading-relaxed">
            为保证练功房实战效果，在您提交自己的断语之前，其他易友的分析处于隐藏状态。请在上方交卷后解锁！
          </p>
        </div>

        <!-- 解锁后的断语列表 -->
        <div v-else class="space-y-3">
          <div
            v-for="r in currentCase.readings"
            :key="r.id"
            class="p-3.5 rounded-xl bg-paper-bg border border-paper-border space-y-2"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="w-6 h-6 rounded-full bg-ink text-white text-xs flex items-center justify-center font-serif">
                  {{ r.userNickname[0] }}
                </span>
                <span class="font-bold text-sm text-ink">{{ r.userNickname }}</span>
                <span v-if="r.isCurrentUser" class="px-1.5 py-0.5 rounded bg-gold-light text-gold text-xs">我</span>
              </div>
              <button
                @click="handleLike(r.id)"
                class="px-2.5 py-1 rounded-full bg-paper-card border border-paper-border text-xs font-medium text-cinnabar hover:bg-cinnabar-light transition"
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