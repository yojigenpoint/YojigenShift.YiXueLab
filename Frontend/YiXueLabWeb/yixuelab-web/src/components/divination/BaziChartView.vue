<script setup lang="ts">
import { computed } from 'vue';
import { useDictStore } from '../../stores/useDictStore';
import { Gender, type BaziCaseData } from '../../types/yixue';

const props = defineProps<{
  data: BaziCaseData;
}>();

const dictStore = useDictStore();

const pillarsList = computed(() => {
  const { yearIdx, monthIdx, dayIdx, hourIdx } = props.data.pillars;
  return [
    { label: '年柱', ...dictStore.resolveGanZhi(yearIdx) },
    { label: '月柱', ...dictStore.resolveGanZhi(monthIdx) },
    { label: '日柱 (日主)', ...dictStore.resolveGanZhi(dayIdx), isDayMaster: true },
    { label: '时柱', ...dictStore.resolveGanZhi(hourIdx) }
  ];
});

const daYunList = computed(() => {
  const startAge = props.data.daYun.startYearAge;
  const startYear = new Date(props.data.daYun.startDateUtc).getUTCFullYear();
  return props.data.daYun.daYunPillars.map((idx, step) => ({
    ageText: `${startAge + step * 10}岁`,
    yearText: `${startYear + step * 10}`,
    ...dictStore.resolveGanZhi(idx)
  }));
});
</script>

<template>
  <div class="oriental-card rounded-xl overflow-hidden">
    <!-- 顶部造别与起运信息 -->
    <div class="bg-paper-sub/60 px-3.5 py-2 border-b border-paper-border flex items-center justify-between">
      <span class="font-serif font-bold text-sm text-ink">
        【四柱八字盘】{{ data.gender === Gender.Male ? '乾造 (男)' : '坤造 (女)' }}
      </span>
      <span class="text-xs text-ink-light">
        {{ data.daYun.startYearAge }}岁{{ data.daYun.startMonthAge }}个月起运 ({{ data.daYun.isForward ? '顺排' : '逆排' }})
      </span>
    </div>

    <!-- 四柱天干地支网格 -->
    <div class="grid grid-cols-4 bg-paper-card">
      <div
        v-for="(p, i) in pillarsList"
        :key="i"
        class="flex flex-col items-center border-r last:border-r-0 border-paper-border py-3 px-1"
      >
        <span class="text-xs text-ink-muted mb-2" :class="{ 'font-bold text-cinnabar': p.isDayMaster }">
          {{ p.label }}
        </span>
        <span class="font-serif font-bold text-2xl" :class="p.stemColorClass">
          {{ p.stemName }}
        </span>
        <span class="font-serif font-bold text-2xl mt-1.5" :class="p.branchColorClass">
          {{ p.branchName }}
        </span>
      </div>
    </div>

    <!-- 十年大运横向滚动条 -->
    <div class="p-3 bg-paper-sub/40 border-t border-paper-border">
      <div class="text-xs text-ink-muted mb-1.5 flex justify-between">
        <span>十年大运序列：</span>
        <span v-if="data.location" class="text-ink-light">📍 {{ data.location }}</span>
      </div>
      <div class="flex items-center gap-1.5 overflow-x-auto no-scrollbar pb-1">
        <div
          v-for="(dy, idx) in daYunList"
          :key="idx"
          class="shrink-0 w-16 rounded-lg p-1.5 text-center border bg-paper-bg border-paper-border"
        >
          <div class="text-xs text-ink-muted">{{ dy.ageText }}</div>
          <div class="text-xs text-ink-muted">{{ dy.yearText }}</div>
          <div class="font-serif font-bold text-base my-0.5">
            <span :class="dy.stemColorClass">{{ dy.stemName }}</span>
            <span :class="dy.branchColorClass">{{ dy.branchName }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>