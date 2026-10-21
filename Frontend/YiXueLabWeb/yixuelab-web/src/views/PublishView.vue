<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { casesApi, metaApi } from '../api/yixueApi';
import DivinationChartHost from '../components/divination/DivinationChartHost.vue';
import {
  DivinationType,
  Gender,
  type DivinationPayload,
  type PublishCaseRequestDto
} from '../types/yixue';

const router = useRouter();

const divinationType = ref<DivinationType>(DivinationType.Bazi);
const castTimeLocal = ref('1990-05-12T14:30');
const baziGender = ref<Gender>(Gender.Male);
const location = ref('四川省成都市');
const liuYaoLines = ref<number[]>([7, 8, 9, 7, 6, 8]); // 初爻到上爻
const meiHuaNum1 = ref(15);
const meiHuaNum2 = ref(28);

const backgroundInfo = ref('');
const questionText = ref('');
const realFeedback = ref('');
const tagsText = ref('#实证案例 #盲断练手');

const previewData = ref<DivinationPayload | null>(null);
const errorMsg = ref('');

function buildPayload(): PublishCaseRequestDto {
  const revealTomorrow = new Date(Date.now() + 24 * 3600 * 1000).toISOString();
  return {
    divinationType: divinationType.value,
    castTimeUtc: new Date(castTimeLocal.value).toISOString(),
    backgroundInfo: backgroundInfo.value || '求测人提供真实案例',
    questionText: questionText.value || '请断吉凶事项',
    realFeedback: realFeedback.value || null,
    revealTimeUtc: revealTomorrow,
    tags: tagsText.value.split(/[\s,，]+/).filter(Boolean),
    baziGender: divinationType.value === DivinationType.Bazi ? baziGender.value : null,
    location: location.value,
    liuYaoRawLines: divinationType.value === DivinationType.LiuYao ? liuYaoLines.value : null,
    meiHuaCastMethod: 'Numbers',
    meiHuaNumbers: [meiHuaNum1.value, meiHuaNum2.value]
  };
}

async function handlePreview() {
  errorMsg.value = '';
  try {
    previewData.value = await metaApi.previewPlot(buildPayload());
  } catch (err: any) {
    errorMsg.value = err.message;
  }
}

async function handlePublish() {
  errorMsg.value = '';
  try {
    const caseId = await casesApi.publishCase(buildPayload());
    router.push({ path: '/', query: { caseId } });
  } catch (err: any) {
    errorMsg.value = err.message;
  }
}
</script>

<template>
  <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-4">
    <h2 class="font-serif font-bold text-lg text-ink border-b border-paper-border pb-2.5">
      发布新盲断案例
    </h2>

    <div v-if="errorMsg" class="p-3 rounded-xl bg-cinnabar-light text-cinnabar text-xs font-medium">
      ⚠️ {{ errorMsg }}
    </div>

    <!-- 1. 门类选择 -->
    <div class="grid grid-cols-4 gap-2">
      <button
        type="button"
        @click="divinationType = DivinationType.Bazi"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.Bazi ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >八字</button>
      <button
        type="button"
        @click="divinationType = DivinationType.LiuYao"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.LiuYao ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >六爻</button>
      <button
        type="button"
        @click="divinationType = DivinationType.QiMen"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.QiMen ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >奇门</button>
      <button
        type="button"
        @click="divinationType = DivinationType.MeiHua"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.MeiHua ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >梅花</button>
    </div>

    <!-- 2. 公历时间 -->
    <div>
      <label class="block text-xs font-bold text-ink mb-1">核心公历时间 (出生/起卦时间)</label>
      <input v-model="castTimeLocal" type="datetime-local" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
    </div>

    <!-- 3. 八字专属参数 -->
    <div v-if="divinationType === DivinationType.Bazi" class="p-3 rounded-xl bg-paper-sub/70 border border-paper-border space-y-2">
      <div class="flex gap-4 text-sm">
        <label><input type="radio" v-model="baziGender" :value="Gender.Male" /> 乾造 (男)</label>
        <label><input type="radio" v-model="baziGender" :value="Gender.Female" /> 坤造 (女)</label>
      </div>
      <input v-model="location" type="text" placeholder="出生地点" class="w-full rounded-lg bg-white border border-paper-border p-2 text-sm" />
    </div>

    <!-- 4. 六爻专属参数 (初爻到上爻) -->
    <div v-if="divinationType === DivinationType.LiuYao" class="p-3 rounded-xl bg-paper-sub/70 border border-paper-border space-y-1.5">
      <p class="text-xs font-bold text-cinnabar mb-1">六爻摇卦结果 (从初爻往上至六爻)：</p>
      <div v-for="i in 6" :key="i" class="flex items-center justify-between text-xs bg-white px-2.5 py-1.5 rounded border border-paper-border">
        <span>第 {{ i }} 爻</span>
        <select v-model="liuYaoLines[i - 1]" class="font-bold text-cinnabar bg-transparent">
          <option :value="7">7 - 少阳 (— 阳静)</option>
          <option :value="8">8 - 少阴 (-- 阴静)</option>
          <option :value="9">9 - 老阳 (○ 阳动)</option>
          <option :value="6">6 - 老阴 (× 阴动)</option>
        </select>
      </div>
    </div>

    <!-- 5. 文本信息 -->
    <textarea v-model="backgroundInfo" rows="2" placeholder="求测背景提示 (发帖公开)..." class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm"></textarea>
    <input v-model="questionText" type="text" placeholder="盲断核心问题 (发帖公开)..." class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
    <textarea v-model="realFeedback" rows="2" placeholder="真实反馈底牌 (揭晓前严密隐藏)..." class="w-full rounded-lg bg-jade-light/50 border border-jade/40 p-2.5 text-sm"></textarea>
    <input v-model="tagsText" type="text" placeholder="考点标签 (空格分隔)" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />

    <!-- 实时排盘预览展示区 -->
    <div v-if="previewData" class="pt-2">
      <p class="text-xs font-bold text-jade mb-1">✓ 后端 YiFramework 实时排盘预览：</p>
      <DivinationChartHost :payload="previewData" />
    </div>

    <div class="grid grid-cols-2 gap-3 pt-2">
      <button type="button" @click="handlePreview" class="py-3 rounded-xl bg-paper-sub text-ink font-serif font-bold text-sm border border-paper-border">
        👁️ 预览排盘
      </button>
      <button type="button" @click="handlePublish" class="py-3 rounded-xl bg-cinnabar text-white font-serif font-bold text-sm shadow-md">
        🚀 正式发布
      </button>
    </div>
  </div>
</template>