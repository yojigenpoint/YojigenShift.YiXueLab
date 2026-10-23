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

// 国内外地点、经度、时区、真太阳时与夏令时控制
const location = ref('四川省成都市');
const longitude = ref<number | null>(104.06);
const standardUtcOffsetHours = ref<number>(8);
const isTrueSolarTime = ref(true);
const isDaylightSavingTime = ref(false);

// 常用国内外城市快捷选择预设 (选定后自动填入经度与标准时区，用户也可自由修改)
const presetCities = [
  { label: '🇨🇳 四川成都 (104.06°E)', name: '四川省成都市', lon: 104.06, tz: 8 },
  { label: '🇨🇳 北京 (116.41°E)', name: '北京市', lon: 116.41, tz: 8 },
  { label: '🇨🇳 上海 (121.47°E)', name: '上海市', lon: 121.47, tz: 8 },
  { label: '🇨🇳 广东广州 (113.26°E)', name: '广东省广州市', lon: 113.26, tz: 8 },
  { label: '🇨🇳 广东深圳 (114.06°E)', name: '广东省深圳市', lon: 114.06, tz: 8 },
  { label: '🇨🇳 浙江杭州 (120.16°E)', name: '浙江省杭州市', lon: 120.16, tz: 8 },
  { label: '🇨🇳 湖北武汉 (114.31°E)', name: '湖北省武汉市', lon: 114.31, tz: 8 },
  { label: '🇨🇳 陕西西安 (108.94°E)', name: '陕西省西安市', lon: 108.94, tz: 8 },
  { label: '🇨🇳 重庆 (106.55°E)', name: '重庆市', lon: 106.55, tz: 8 },
  { label: '🇨🇳 新疆乌鲁木齐 (87.62°E)', name: '新疆乌鲁木齐市', lon: 87.62, tz: 8 },
  { label: '🇭🇰 中国香港 (114.17°E)', name: '中国香港', lon: 114.17, tz: 8 },
  { label: '🇹🇼 中国台北 (121.56°E)', name: '中国台北', lon: 121.56, tz: 8 },
  { label: '🇺🇸 美国纽约 (-74.01°W / UTC-5)', name: '美国纽约 (New York)', lon: -74.01, tz: -5 },
  { label: '🇺🇸 美国洛杉矶 (-118.24°W / UTC-8)', name: '美国洛杉矶 (Los Angeles)', lon: -118.24, tz: -8 },
  { label: '🇺🇸 美国旧金山/硅谷 (-122.42°W / UTC-8)', name: '美国旧金山 (San Francisco)', lon: -122.42, tz: -8 },
  { label: '🇺🇸 美国芝加哥 (-87.63°W / UTC-6)', name: '美国芝加哥 (Chicago)', lon: -87.63, tz: -6 },
  { label: '🇨🇦 加拿大多伦多 (-79.38°W / UTC-5)', name: '加拿大多伦多 (Toronto)', lon: -79.38, tz: -5 },
  { label: '🇨🇦 加拿大温哥华 (-123.12°W / UTC-8)', name: '加拿大温哥华 (Vancouver)', lon: -123.12, tz: -8 },
  { label: '🇬🇧 英国伦敦 (-0.13°W / UTC+0)', name: '英国伦敦 (London)', lon: -0.13, tz: 0 },
  { label: '🇯🇵 日本东京 (139.69°E / UTC+9)', name: '日本东京 (Tokyo)', lon: 139.69, tz: 9 },
  { label: '🇸🇬 新加坡 (103.82°E / UTC+8)', name: '新加坡 (Singapore)', lon: 103.82, tz: 8 },
  { label: '🇦🇺 澳洲悉尼 (151.21°E / UTC+10)', name: '澳大利亚悉尼 (Sydney)', lon: 151.21, tz: 10 }
];

function onSelectPresetCity(event: Event) {
  const idx = Number((event.target as HTMLSelectElement).value);
  const city = presetCities[idx];
  if (city) {
    location.value = city.name;
    longitude.value = city.lon;
    standardUtcOffsetHours.value = city.tz;
  }
}

// 当用户手动输入任意城市名时，清空写死的经度，交由后端根据城市名称智能匹配（用户也可手动填经度）
function onManualLocationInput() {
  longitude.value = null;
}

const liuYaoLines = ref<number[]>([7, 8, 9, 7, 6, 8]); // 初爻到上爻
const meiHuaNum1 = ref(15);
const meiHuaNum2 = ref(28);

const backgroundInfo = ref('');
const questionText = ref('');
const realFeedback = ref('');
const tagsText = ref('#实证案例 #盲断练手');

const previewData = ref<DivinationPayload | null>(null);
const previewLoading = ref(false);
const errorMsg = ref('');

function buildPayload(): PublishCaseRequestDto {
  const revealTomorrow = new Date(Date.now() + 24 * 3600 * 1000).toISOString();
  // 直接将用户填写的当地钟表年月日时分封装为 ISO 字符串传给后端排盘器
  const clockTimeUtc = `${castTimeLocal.value}:00.000Z`;

  return {
    divinationType: divinationType.value,
    castTimeUtc: clockTimeUtc,
    backgroundInfo: backgroundInfo.value || '求测人提供真实案例',
    questionText: questionText.value || '请断吉凶事项',
    realFeedback: realFeedback.value || null,
    revealTimeUtc: revealTomorrow,
    tags: tagsText.value.split(/[\s,，]+/).filter(Boolean),
    baziGender: divinationType.value === DivinationType.Bazi ? baziGender.value : null,
    location: location.value.trim() || null,
    isTrueSolarTime: isTrueSolarTime.value,
    isDaylightSavingTime: isDaylightSavingTime.value,
    longitude: longitude.value,
    standardUtcOffsetHours: standardUtcOffsetHours.value,
    liuYaoRawLines: divinationType.value === DivinationType.LiuYao ? liuYaoLines.value : null,
    meiHuaCastMethod: 'Numbers',
    meiHuaNumbers: [meiHuaNum1.value, meiHuaNum2.value]
  };
}

async function handlePreview() {
  errorMsg.value = '';
  previewLoading.value = true;
  try {
    previewData.value = await metaApi.previewPlot(buildPayload());
  } catch (err: any) {
    errorMsg.value = err.message || '排盘预览失败，请检查输入参数';
  } finally {
    previewLoading.value = false;
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
        @click="divinationType = DivinationType.Bazi; previewData = null"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.Bazi ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >八字</button>
      <button
        type="button"
        @click="divinationType = DivinationType.LiuYao; previewData = null"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.LiuYao ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >六爻</button>
      <button
        type="button"
        @click="divinationType = DivinationType.QiMen; previewData = null"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.QiMen ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >奇门</button>
      <button
        type="button"
        @click="divinationType = DivinationType.MeiHua; previewData = null"
        class="py-2 rounded-lg text-xs font-bold border"
        :class="divinationType === DivinationType.MeiHua ? 'bg-cinnabar text-white border-cinnabar' : 'bg-paper-bg text-ink border-paper-border'"
      >梅花</button>
    </div>

    <!-- 2. 公历时间与真太阳时/夏令时/国内外地点经度面板 -->
    <div class="p-3.5 rounded-xl bg-paper-sub/70 border border-paper-border space-y-3">
      <div>
        <label class="block text-xs font-bold text-ink mb-1">
          核心公历时间 (请直接输入出生地/起卦地当时的钟表时间)
        </label>
        <input
          v-model="castTimeLocal"
          type="datetime-local"
          class="w-full rounded-lg bg-white border border-paper-border p-2.5 text-sm"
        />
      </div>

      <!-- 八字性别选择 -->
      <div v-if="divinationType === DivinationType.Bazi" class="flex gap-5 text-sm pt-1">
        <label class="flex items-center gap-1.5 font-medium text-ink">
          <input type="radio" v-model="baziGender" :value="Gender.Male" class="accent-cinnabar" /> 乾造 (男命)
        </label>
        <label class="flex items-center gap-1.5 font-medium text-ink">
          <input type="radio" v-model="baziGender" :value="Gender.Female" class="accent-cinnabar" /> 坤造 (女命)
        </label>
      </div>

      <!-- 国内外城市选择与自由输入 -->
      <div class="space-y-2 border-t border-dashed border-paper-border pt-2.5">
        <div class="flex items-center justify-between">
          <label class="text-xs font-bold text-ink">出生地 / 起卦地 (支持国内外城市)</label>
          <select
            @change="onSelectPresetCity"
            class="text-xs bg-white border border-paper-border rounded-lg px-2 py-1 text-cinnabar font-medium"
          >
            <option value="" disabled>⚡ 快捷选择国内外城市...</option>
            <option v-for="(c, idx) in presetCities" :key="idx" :value="idx">
              {{ c.label }}
            </option>
          </select>
        </div>

        <input
          v-model="location"
          @input="onManualLocationInput"
          type="text"
          placeholder="可自由输入城市（如：四川省成都市 / New York / 伦敦），后台自动匹配经度"
          class="w-full rounded-lg bg-white border border-paper-border p-2 text-sm"
        />

        <!-- 经度与时区高级微调 (自动填充，也允许手动填偏远县城精确经度) -->
        <div class="grid grid-cols-2 gap-2 text-xs">
          <div>
            <span class="text-ink-muted block mb-0.5">地理经度 (东经+ / 西经-)</span>
            <input
              v-model.number="longitude"
              type="number"
              step="0.01"
              placeholder="留空则由后台按城市名自动截取"
              class="w-full rounded-lg bg-white border border-paper-border p-1.5 text-xs font-mono"
            />
          </div>
          <div>
            <span class="text-ink-muted block mb-0.5">标准时区 (国内+8 / 美东-5)</span>
            <input
              v-model.number="standardUtcOffsetHours"
              type="number"
              step="1"
              class="w-full rounded-lg bg-white border border-paper-border p-1.5 text-xs font-mono"
            />
          </div>
        </div>
      </div>

      <!-- 真太阳时与夏令时开关 -->
      <div class="flex flex-wrap items-center gap-4 pt-1 text-xs">
        <label class="flex items-center gap-1.5 font-bold text-ink cursor-pointer">
          <input type="checkbox" v-model="isTrueSolarTime" class="accent-cinnabar w-4 h-4" />
          <span>启用真太阳时排盘 (经度+均时差校准)</span>
        </label>
        <label class="flex items-center gap-1.5 font-bold text-cinnabar cursor-pointer">
          <input type="checkbox" v-model="isDaylightSavingTime" class="accent-cinnabar w-4 h-4" />
          <span>夏令时出生/起卦 (自动扣减1小时)</span>
        </label>
      </div>
    </div>

    <!-- 3. 六爻专属参数 (初爻到上爻) -->
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

    <!-- 4. 文本信息 -->
    <textarea v-model="backgroundInfo" rows="2" placeholder="求测背景提示 (发帖公开)..." class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm"></textarea>
    <input v-model="questionText" type="text" placeholder="盲断核心问题 (发帖公开)..." class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />
    <textarea v-model="realFeedback" rows="2" placeholder="真实反馈底牌 (揭晓前严密隐藏)..." class="w-full rounded-lg bg-jade-light/50 border border-jade/40 p-2.5 text-sm"></textarea>
    <input v-model="tagsText" type="text" placeholder="考点标签 (空格分隔)" class="w-full rounded-lg bg-paper-bg border border-paper-border p-2.5 text-sm" />

    <!-- 实时排盘预览展示区 -->
    <div v-if="previewData" class="pt-2 space-y-1.5">
      <div class="flex items-center justify-between text-xs font-bold text-jade">
        <span>✓ 后端 YiFramework 实时排盘预览：</span>
        <span v-if="previewData.trueSolarTimeUtc">
          排盘真太阳时：{{ previewData.trueSolarTimeUtc.replace('T', ' ').slice(0, 16) }}
        </span>
      </div>
      <DivinationChartHost :payload="previewData" />
    </div>

    <div class="grid grid-cols-2 gap-3 pt-2">
      <button
        type="button"
        @click="handlePreview"
        :disabled="previewLoading"
        class="py-3 rounded-xl bg-paper-sub hover:bg-paper-border text-ink font-serif font-bold text-sm border border-paper-border transition"
      >
        {{ previewLoading ? '正在排盘...' : '👁️ 预览排盘' }}
      </button>
      <button
        type="button"
        @click="handlePublish"
        class="py-3 rounded-xl bg-cinnabar hover:bg-cinnabar-dark text-white font-serif font-bold text-sm shadow-md transition"
      >
        🚀 正式发布
      </button>
    </div>
  </div>
</template>