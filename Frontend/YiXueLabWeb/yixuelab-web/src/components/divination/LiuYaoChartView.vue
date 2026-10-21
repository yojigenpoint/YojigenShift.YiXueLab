<script setup lang="ts">
import { computed } from 'vue';
import { useDictStore } from '../../stores/useDictStore';
import type { LiuYaoCaseData, YaoInfo } from '../../types/yixue';

const props = defineProps<{
  data: LiuYaoCaseData;
}>();

const dictStore = useDictStore();

const pillarsText = computed(() => {
  const y = dictStore.resolveGanZhi(props.data.pillars.yearIdx).fullName;
  const m = dictStore.resolveGanZhi(props.data.pillars.monthIdx).fullName;
  const d = dictStore.resolveGanZhi(props.data.pillars.dayIdx).fullName;
  return `${y}年 · ${m}月 · ${d}日`;
});

const origHexName = computed(() => {
  const name = dictStore.dict?.hexagramNames[props.data.original.hexagram.value] ?? '';
  const palace = dictStore.dict?.trigrams[props.data.original.palace] ?? '';
  return `${name} (${palace}宫)`;
});

const futHexName = computed(() => {
  const name = dictStore.dict?.hexagramNames[props.data.future.hexagram.value] ?? '';
  const palace = dictStore.dict?.trigrams[props.data.future.palace] ?? '';
  return `${name} (${palace}宫)`;
});

// 传统六爻从上爻(index=6)往下排到初爻(index=1)
const reversedLines = computed(() => {
  const origLines = [...props.data.original.lines].reverse();
  const futLines = [...props.data.future.lines].reverse();
  return origLines.map((orig, idx) => ({
    orig,
    fut: futLines[idx]!,
    evalComments: props.data.evaluation[orig.index]?.comments ?? []
  }));
});

function isYangLine(yaoType: number): boolean {
  return yaoType === 7 || yaoType === 9; // 7=少阳, 9=老阳
}

function getMovingSymbol(yaoType: number): string {
  if (yaoType === 6) return '× 老阴动';
  if (yaoType === 9) return '○ 老阳动';
  return '';
}

function formatLineLabel(line: YaoInfo): string {
  const rel = dictStore.dict?.sixRelations[line.relation] ?? '';
  const stem = dictStore.dict?.stems[line.stem] ?? '';
  const branch = dictStore.dict?.branches[line.branch] ?? '';
  const wx = dictStore.dict?.wuXing[line.wuXing] ?? '';
  return `${rel}${stem}${branch}${wx}`;
}
</script>

<template>
  <div class="oriental-card rounded-xl overflow-hidden">
    <div class="bg-paper-sub/60 px-3.5 py-2 border-b border-paper-border flex flex-wrap items-center justify-between gap-1">
      <span class="font-serif font-bold text-sm text-ink">
        本卦：{{ origHexName }} ➔ 变卦：{{ futHexName }}
      </span>
      <span class="text-xs font-mono text-cinnabar">{{ pillarsText }}</span>
    </div>

    <div class="divide-y divide-paper-border bg-paper-card">
      <div
        v-for="row in reversedLines"
        :key="row.orig.index"
        class="grid grid-cols-12 items-center py-2.5 px-2.5 text-xs"
        :class="{ 'bg-amber-50/70': row.orig.type === 6 || row.orig.type === 9 }"
      >
        <!-- 六兽 -->
        <div class="col-span-2 text-ink-muted font-medium">
          {{ dictStore.dict?.sixBeasts[row.orig.beast] }}
        </div>

        <!-- 本卦纳甲与世应空亡 -->
        <div class="col-span-4 font-medium text-ink flex flex-wrap items-center gap-1">
          <span>{{ formatLineLabel(row.orig) }}</span>
          <span v-if="row.orig.isShi" class="px-1 rounded bg-cinnabar text-white font-bold">世</span>
          <span v-if="row.orig.isYing" class="px-1 rounded bg-ink-light text-white font-bold">应</span>
          <span v-if="row.orig.isKongWang" class="px-1 rounded bg-gray-200 text-gray-700">空</span>
        </div>

        <!-- 阴阳爻画符号 -->
        <div class="col-span-2 flex items-center justify-center">
          <div v-if="isYangLine(row.orig.type)" class="w-12 h-2.5 bg-cinnabar rounded-sm"></div>
          <div v-else class="w-12 flex justify-between">
            <div class="w-5 h-2.5 bg-ink rounded-sm"></div>
            <div class="w-5 h-2.5 bg-ink rounded-sm"></div>
          </div>
        </div>

        <!-- 动爻标记 -->
        <div class="col-span-2 text-center font-bold text-cinnabar">
          {{ getMovingSymbol(row.orig.type) }}
        </div>

        <!-- 变卦纳甲 -->
        <div class="col-span-2 text-right text-ink-light">
          {{ formatLineLabel(row.fut) }}
        </div>

        <!-- LiuYaoEvaluator 自动旺衰批注 (月破/暗动等) -->
        <div v-if="row.evalComments.length > 0" class="col-span-12 mt-1.5 text-right">
          <span class="px-1.5 py-0.5 rounded bg-gold-light border border-gold/30 text-gold text-xs">
            💡 {{ row.evalComments.join(' · ') }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>