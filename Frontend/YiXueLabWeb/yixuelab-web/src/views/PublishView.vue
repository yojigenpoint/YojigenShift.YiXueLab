<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { aiApi, casesApi, metaApi } from '../api/yixueApi';
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

const locationPreset = ref('四川省成都市|104.06|8');
const location = ref('四川省成都市');
const longitude = ref<number>(104.06);
const standardUtcOffsetHours = ref<number>(8);
const isTrueSolarTime = ref<boolean>(true);
const isDaylightSavingTime = ref<boolean>(false);

const cityOptions = [
  { label: '🇨🇳 四川省成都市 (104.06°E, UTC+8)', value: '四川省成都市|104.06|8' },
  { label: '🇨🇳 北京市 (116.41°E, UTC+8)', value: '北京市|116.41|8' },
  { label: '🇨🇳 上海市 (121.47°E, UTC+8)', value: '上海市|121.47|8' },
  { label: '🇨🇳 广东省广州市 (113.26°E, UTC+8)', value: '广东省广州市|113.26|8' },
  { label: '🇨🇳 广东省深圳市 (114.06°E, UTC+8)', value: '广东省深圳市|114.06|8' },
  { label: '🇨🇳 浙江省杭州市 (120.16°E, UTC+8)', value: '浙江省杭州市|120.16|8' },
  { label: '🇨🇳 重庆市 (106.55°E, UTC+8)', value: '重庆市|106.55|8' },
  { label: '🇨🇳 湖北省武汉市 (114.31°E, UTC+8)', value: '湖北省武汉市|114.31|8' },
  { label: '🇨🇳 陕西省西安市 (108.94°E, UTC+8)', value: '陕西省西安市|108.94|8' },
  { label: '🇨🇳 新疆乌鲁木齐 (87.62°E, UTC+8)', value: '新疆乌鲁木齐|87.62|8' },
  { label: '🇭🇰 中国香港 (114.17°E, UTC+8)', value: '中国香港|114.17|8' },
  { label: '🇹🇼 中国台北 (121.56°E, UTC+8)', value: '中国台北|121.56|8' },
  { label: '🇺🇸 美国纽约 New York (74.01°W, UTC-5)', value: '美国纽约 New York|-74.01|-5' },
  { label: '🇺🇸 美国洛杉矶 Los Angeles (118.24°W, UTC-8)', value: '美国洛杉矶 Los Angeles|-118.24|-8' },
  { label: '🇺🇸 美国旧金山/硅谷 San Francisco (122.42°W, UTC-8)', value: '美国旧金山 San Francisco|-122.42|-8' },
  { label: '🇺🇸 美国芝加哥 Chicago (87.63°W, UTC-6)', value: '美国芝加哥 Chicago|-87.63|-6' },
  { label: '🇨🇦 加拿大多伦多 Toronto (79.38°W, UTC-5)', value: '加拿大多伦多 Toronto|-79.38|-5' },
  { label: '🇨🇦 加拿大温哥华 Vancouver (123.12°W, UTC-8)', value: '加拿大温哥华 Vancouver|-123.12|-8' },
  { label: '🇬🇧 英国伦敦 London (0.13°W, UTC+0)', value: '英国伦敦 London|-0.13|0' },
  { label: '🇯🇵 日本东京 Tokyo (139.69°E, UTC+9)', value: '日本东京 Tokyo|139.69|9' },
  { label: '🇸🇬 新加坡 Singapore (103.82°E, UTC+8)', value: '新加坡 Singapore|103.82|8' },
  { label: '🇦🇺 澳大利亚悉尼 Sydney (151.21°E, UTC+10)', value: '澳大利亚悉尼 Sydney|151.21|10' }
];

function handlePresetChange() {
  const parts = locationPreset.value.split('|');
  if (parts.length === 3) {
    location.value = parts[0]!;
    longitude.value = parseFloat(parts[1]!);
    standardUtcOffsetHours.value = parseFloat(parts[2]!);
    checkChinaHistoricalDst();
  }
}

function isChinaHistoricalDst(dateStr: string): boolean {
  const dt = new Date(dateStr);
  if (isNaN(dt.getTime())) return false;
  const y = dt.getFullYear();
  const m = dt.getMonth() + 1;
  const d = dt.getDate();
  const md = m * 100 + d;

  if (y === 1986) return md >= 504 && md <= 914;
  if (y === 1987) return md >= 412 && md <= 913;
  if (y === 1988) return md >= 417 && md <= 911;
  if (y === 1989) return md >= 416 && md <= 917;
  if (y === 1990) return md >= 415 && md <= 916;
  if (y === 1991) return md >= 414 && md <= 915;
  return false;
}

function checkChinaHistoricalDst() {
  if (standardUtcOffsetHours.value === 8 && longitude.value >= 73 && longitude.value <= 135) {
    isDaylightSavingTime.value = isChinaHistoricalDst(castTimeLocal.value);
  }
}

watch(castTimeLocal, () => checkChinaHistoricalDst(), { immediate: true });

const liuYaoRawLines = ref<number[]>([7, 8, 9, 7, 6, 8]);
const meiHuaNum1 = ref(15);
const meiHuaNum2 = ref(28);

const backgroundInfo = ref('');
const questionText = ref('');
const realFeedback = ref('');
const tagsInput = ref('#盲断练手 #实证');
const suggestingTags = ref(false);

const previewPayload = ref<DivinationPayload | null>(null);
const errorMsg = ref('');

function selectDivinationType(t: DivinationType) {
  divinationType.value = t;
  previewPayload.value = null;
  errorMsg.value = '';
}

function buildDto(): PublishCaseRequestDto {
  const revealDate = new Date(Date.now() + 24 * 3600 * 1000).toISOString();
  return {
    divinationType: divinationType.value,
    castTimeUtc: new Date(castTimeLocal.value).toISOString(),
    backgroundInfo: backgroundInfo.value || '求测人提供真实经历供群内盲断。',
    questionText: questionText.value || '请断此事吉凶与应期。',
    realFeedback: realFeedback.value || null,
    revealTimeUtc: revealDate,
    tags: tagsInput.value.split(/[\s,，]+/).filter(Boolean),
    baziGender: divinationType.value === DivinationType.Bazi ? baziGender.value : null,
    location: location.value,
    longitude: longitude.value,
    standardUtcOffsetHours: standardUtcOffsetHours.value,
    isTrueSolarTime: isTrueSolarTime.value,
    isDaylightSavingTime: isDaylightSavingTime.value,
    liuYaoRawLines: divinationType.value === DivinationType.LiuYao ? liuYaoRawLines.value : null,
    meiHuaCastMethod: divinationType.value === DivinationType.MeiHua ? 'Numbers' : null,
    meiHuaNumbers: divinationType.value === DivinationType.MeiHua ? [meiHuaNum1.value, meiHuaNum2.value] : null
  };
}

async function handleSuggestTags() {
  suggestingTags.value = true;
  errorMsg.value = '';
  try {
    const res = await aiApi.suggestTags(
      divinationType.value,
      backgroundInfo.value,
      questionText.value,
      realFeedback.value
    );
    tagsInput.value = res.tags.join(' ');
  } catch (err: any) {
    errorMsg.value = err.message || '提取标签失败';
  } finally {
    suggestingTags.value = false;
  }
}

async function handlePreview() {
  errorMsg.value = '';
  try {
    previewPayload.value = await metaApi.previewPlot(buildDto());
  } catch (err: any) {
    errorMsg.value = err.message;
  }
}

async function handlePublish() {
  errorMsg.value = '';
  try {
    const caseId = await casesApi.publishCase(buildDto());
    router.push({ path: '/', query: { caseId } });
  } catch (err: any) {
    errorMsg.value = err.message;
  }
}
</script>

<template>
  <div class="space-y-4">
    <div class="bg-paper-card border border-paper-border rounded-xl p-4 space-y-4">
      <h2 class="font-serif font-bold text-lg text-ink">发布新盲断案例</h2>

      <div v-if="errorMsg" class="p-3 rounded-xl bg-cinnabar-light text-cinnabar text-xs font-medium">
        ⚠️ {{ errorMsg }}
      </div>

      <div class="grid grid-cols-4 gap-2">
        <button
          v-for="(label, t) in { 0: '八字', 1: '六爻', 2: '奇门', 3: '梅花' }"
          :key="t"
          type="button"
          @click="selectDivinationType(Number(t) as DivinationType)"
          class="py-2 rounded-lg text-xs font-bold border transition"
          :class="divinationType === Number(t) ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
        >
          {{ label }}
        </button>
      </div>

      <div>
        <label class="block text-xs font-bold text-ink mb-1">核心公历时间 (出生或起卦当地钟表时间)</label>
        <input v-model="castTimeLocal" type="datetime-local" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
      </div>

      <!-- 海内外地点、经度、真太阳时与夏令时校准区 -->
      <div class="p-3 rounded-xl bg-paper-sub/70 border border-paper-border space-y-2.5">
        <div class="text-xs font-bold text-cinnabar">🧭 出生地/起卦地与真太阳时校准 (覆盖海内外)</div>

        <div>
          <label class="block text-xs text-ink-light mb-1">常用海内外城市快选：</label>
          <select
            v-model="locationPreset"
            @change="handlePresetChange"
            class="w-full rounded-lg bg-white border border-paper-border p-2 text-xs text-ink"
          >
            <option v-for="c in cityOptions" :key="c.value" :value="c.value">{{ c.label }}</option>
          </select>
        </div>

        <div class="grid grid-cols-3 gap-2">
          <div>
            <label class="block text-[11px] text-ink-muted mb-0.5">自定义地点名</label>
            <input v-model="location" type="text" class="w-full rounded-lg bg-white border border-paper-border p-1.5 text-xs" />
          </div>
          <div>
            <label class="block text-[11px] text-ink-muted mb-0.5">经度 (东正西负)</label>
            <input v-model.number="longitude" type="number" step="0.01" class="w-full rounded-lg bg-white border border-paper-border p-1.5 text-xs" />
          </div>
          <div>
            <label class="block text-[11px] text-ink-muted mb-0.5">标准时区(UTC)</label>
            <input v-model.number="standardUtcOffsetHours" type="number" step="0.5" class="w-full rounded-lg bg-white border border-paper-border p-1.5 text-xs" />
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-4 pt-1">
          <label class="flex items-center gap-1.5 text-xs font-medium text-ink cursor-pointer">
            <input v-model="isTrueSolarTime" type="checkbox" class="accent-cinnabar" />
            <span>启用真太阳时排盘</span>
          </label>
          <label class="flex items-center gap-1.5 text-xs font-medium text-ink cursor-pointer">
            <input v-model="isDaylightSavingTime" type="checkbox" class="accent-cinnabar" />
            <span>夏令时 (自动扣减1小时)</span>
          </label>
        </div>
      </div>

      <div v-if="divinationType === DivinationType.Bazi" class="p-3 rounded-xl bg-paper-sub space-y-2">
        <div class="flex gap-4 text-sm">
          <label><input type="radio" :value="Gender.Male" v-model="baziGender" /> 男命 (乾造)</label>
          <label><input type="radio" :value="Gender.Female" v-model="baziGender" /> 女命 (坤造)</label>
        </div>
      </div>

      <div v-if="divinationType === DivinationType.LiuYao" class="p-3 rounded-xl bg-paper-sub space-y-2">
        <div class="text-xs font-bold text-cinnabar">六爻铜钱结果 (从初爻到上爻)</div>
        <div class="grid grid-cols-2 gap-2">
          <div v-for="(_, idx) in liuYaoRawLines" :key="idx" class="flex items-center justify-between bg-white p-1.5 rounded border text-xs">
            <span>第 {{ idx + 1 }} 爻</span>
            <select v-model="liuYaoRawLines[idx]" class="text-cinnabar font-bold">
              <option :value="7">7 少阳 (—)</option>
              <option :value="8">8 少阴 (--)</option>
              <option :value="9">9 老阳 (○动)</option>
              <option :value="6">6 老阴 (×动)</option>
            </select>
          </div>
        </div>
      </div>

      <div v-if="divinationType === DivinationType.MeiHua" class="p-3 rounded-xl bg-paper-sub grid grid-cols-2 gap-2">
        <input v-model.number="meiHuaNum1" type="number" placeholder="上卦数" class="rounded bg-white p-2 text-sm border" />
        <input v-model.number="meiHuaNum2" type="number" placeholder="下卦数" class="rounded bg-white p-2 text-sm border" />
      </div>

      <textarea v-model="backgroundInfo" rows="2" placeholder="求测背景提示（发帖即公开）" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm"></textarea>
      <input v-model="questionText" type="text" placeholder="盲断核心问题（发帖即公开）" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
      <textarea v-model="realFeedback" rows="2" placeholder="真实反馈底牌（开奖前严密隐藏）" class="w-full rounded-lg bg-jade-light/40 border border-jade/40 p-2.5 text-sm"></textarea>

      <div class="space-y-1.5">
        <div class="flex items-center justify-between">
          <label class="text-xs font-bold text-ink">归档考点标签</label>
          <button
            type="button"
            :disabled="suggestingTags"
            @click="handleSuggestTags"
            class="px-2.5 py-1 rounded-lg bg-gold-light border border-gold/40 text-gold text-xs font-bold hover:bg-gold/15 transition"
          >
            {{ suggestingTags ? 'AI 提炼中...' : '✨ AI 智能提炼标签' }}
          </button>
        </div>
        <input v-model="tagsInput" type="text" placeholder="标签（空格分隔，如 #辰戌冲 #工作变动）" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
      </div>

      <div class="grid grid-cols-2 gap-3">
        <button type="button" @click="handlePreview" class="py-2.5 rounded-xl bg-paper-sub border border-paper-border font-serif font-bold text-sm">
          👁️ 预览排盘
        </button>
        <button type="button" @click="handlePublish" class="py-2.5 rounded-xl bg-cinnabar text-white font-serif font-bold text-sm">
          🚀 正式发布
        </button>
      </div>
    </div>

    <div v-if="previewPayload" class="space-y-2">
      <div class="text-xs font-bold text-cinnabar px-1">👇 实时排盘预览结果：</div>
      <DivinationChartHost :payload="previewPayload" />
    </div>
  </div>
</template>