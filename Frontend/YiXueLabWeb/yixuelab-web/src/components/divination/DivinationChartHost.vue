<script setup lang="ts">
import { computed } from 'vue';
import type {
  BaziCaseData,
  DivinationPayload,
  LiuYaoCaseData,
  MeiHuaCaseData,
  QiMenCaseData
} from '../../types/yixue';
import BaziChartView from './BaziChartView.vue';
import LiuYaoChartView from './LiuYaoChartView.vue';
import QiMenChartView from './QiMenChartView.vue';
import MeiHuaChartView from './MeiHuaChartView.vue';

const props = defineProps<{
  payload: DivinationPayload;
}>();

// 智能识别门类：优先读取 $type，若缺失则按特征属性自动识别
const chartKind = computed(() => {
  const p = props.payload as any;
  if (!p) return '';
  if (p.$type) return p.$type;
  if ('daYun' in p) return 'bazi';
  if ('original' in p && 'future' in p) return 'liuyao';
  if ('chart' in p) return 'qimen';
  if ('result' in p) return 'meihua';
  return '';
});
</script>

<template>
  <BaziChartView v-if="chartKind === 'bazi'" :data="payload as BaziCaseData" />
  <LiuYaoChartView v-else-if="chartKind === 'liuyao'" :data="payload as LiuYaoCaseData" />
  <QiMenChartView v-else-if="chartKind === 'qimen'" :data="payload as QiMenCaseData" />
  <MeiHuaChartView v-else-if="chartKind === 'meihua'" :data="payload as MeiHuaCaseData" />
</template>