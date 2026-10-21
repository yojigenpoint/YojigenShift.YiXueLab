<script setup lang="ts">
import type { DivinationPayload } from '../../types/yixue';
import BaziChartView from './BaziChartView.vue';
import LiuYaoChartView from './LiuYaoChartView.vue';
import QiMenChartView from './QiMenChartView.vue';
import MeiHuaChartView from './MeiHuaChartView.vue';

defineProps<{
  payload: DivinationPayload;
}>();
</script>

<template>
  <BaziChartView v-if="payload.$type === 'bazi'" :data="payload" />
  <LiuYaoChartView v-else-if="payload.$type === 'liuyao'" :data="payload" />
  <QiMenChartView v-else-if="payload.$type === 'qimen'" :data="payload" />
  <MeiHuaChartView v-else-if="payload.$type === 'meihua'" :data="payload" />
</template>