<script setup lang="ts">
import { computed } from 'vue';
import { useDictStore } from '../../stores/useDictStore';
import type { MeiHuaCaseData, MeiHuaGua } from '../../types/yixue';

const props = defineProps<{
  data: MeiHuaCaseData;
}>();

const dictStore = useDictStore();

// 将八卦索引 (0-7) 转换为 3 根爻的阴阳数组 (初爻->三爻, 1=阳, 0=阴)
function getTrigramBits(trigramVal: number): number[] {
  return [
    (trigramVal >> 0) & 1,
    (trigramVal >> 1) & 1,
    (trigramVal >> 2) & 1
  ];
}

function buildGuaCard(title: string, gua: MeiHuaGua, isOriginal: boolean) {
  const lowerBits = getTrigramBits(gua.lower);
  const upperBits = getTrigramBits(gua.upper);
  const sixLines = [...lowerBits, ...upperBits]; // 索引 0(初爻) 到 5(上爻)

  return {
    title,
    upperName: dictStore.dict?.trigrams[gua.upper] ?? '',
    lowerName: dictStore.dict?.trigrams[gua.lower] ?? '',
    isOriginal,
    // 倒序为上爻(6)到初爻(1)渲染
    linesTopDown: sixLines
      .map((isYang, idx) => ({
        lineNumber: idx + 1,
        isYang: isYang === 1,
        isMoving: isOriginal && idx + 1 === props.data.result.movingLine
      }))
      .reverse()
  };
}

const guaCards = computed(() => [
  buildGuaCard('【本卦】', props.data.result.original, true),
  buildGuaCard('【互卦】', props.data.result.mutual, false),
  buildGuaCard('【变卦】', props.data.result.changed, false)
]);
</script>

<template>
  <div class="oriental-card rounded-xl overflow-hidden">
    <div class="bg-paper-sub/60 px-3.5 py-2 border-b border-paper-border flex items-center justify-between">
      <span class="font-serif font-bold text-sm text-ink">【梅花易数 · 本互变三联盘】</span>
      <span class="text-xs text-cinnabar">
        {{ data.castMethod === 'Numbers' ? `报数起卦 [${data.rawNumbers?.join(', ')}]` : '时间起卦' }} · 动爻：第{{ data.result.movingLine }}爻
      </span>
    </div>

    <div class="grid grid-cols-3 bg-paper-card divide-x divide-paper-border">
      <div
        v-for="(g, idx) in guaCards"
        :key="idx"
        class="p-2.5 text-center flex flex-col justify-between"
      >
        <div class="font-serif font-bold text-sm text-ink">{{ g.title }}</div>
        <div class="text-xs text-ink-muted mt-0.5">
          上:{{ g.upperName }} / 下:{{ g.lowerName }}
        </div>
        <div class="my-2 py-2 bg-paper-bg rounded-lg border border-paper-border space-y-1.5">
          <div
            v-for="line in g.linesTopDown"
            :key="line.lineNumber"
            class="flex items-center justify-center gap-1"
          >
            <div v-if="line.isYang" class="w-14 h-2 bg-cinnabar rounded-sm"></div>
            <div v-else class="w-14 flex justify-between">
              <div class="w-6 h-2 bg-ink rounded-sm"></div>
              <div class="w-6 h-2 bg-ink rounded-sm"></div>
            </div>
            <span class="w-3 text-xs font-bold text-cinnabar">
              {{ line.isMoving ? '○' : '' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>