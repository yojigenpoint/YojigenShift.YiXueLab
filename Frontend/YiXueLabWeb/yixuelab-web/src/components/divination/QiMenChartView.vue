<script setup lang="ts">
import { computed } from 'vue';
import { useDictStore } from '../../stores/useDictStore';
import type { QiMenCaseData } from '../../types/yixue';

const props = defineProps<{
  data: QiMenCaseData;
}>();

const dictStore = useDictStore();

// 洛书九宫格标准顺序：巽4、离9、坤2 / 震3、中5、兑7 / 艮8、坎1、乾6
const luoShuOrder = [4, 9, 2, 3, 5, 7, 8, 1, 6];
const palaceNames: Record<number, string> = {
  4: '巽四宫', 9: '离九宫', 2: '坤二宫',
  3: '震三宫', 5: '中五宫', 7: '兑七宫',
  8: '艮八宫', 1: '坎一宫', 6: '乾六宫'
};

const orderedPalaces = computed(() => {
  return luoShuOrder.map((idx) => {
    const p = props.data.chart.palaces?.[idx];
    return {
      idx,
      posName: palaceNames[idx],
      god: p ? dictStore.dict?.qiMenGods[p.god] : '—',
      star: p ? dictStore.dict?.qiMenStars[p.star] : '—',
      door: p ? dictStore.dict?.qiMenDoors[p.door] : '—',
      heaven: p ? dictStore.dict?.stems[p.heavenPlateStem] : '—',
      earth: p ? dictStore.dict?.stems[p.earthPlateStem] : '—',
      hidden: p ? dictStore.dict?.stems[p.hiddenStem] : '—',
      isZhiFu: p && p.star === props.data.chart.zhiFuStar,
      isZhiShi: p && p.door === props.data.chart.zhiShiDoor
    };
  });
});
</script>

<template>
  <div class="oriental-card rounded-xl overflow-hidden">
    <div class="bg-paper-sub/60 px-3.5 py-2 border-b border-paper-border flex items-center justify-between">
      <span class="font-serif font-bold text-sm text-ink">
        【奇门遁甲盘】{{ data.chart.dun === 1 ? '阳遁' : '阴遁' }}{{ data.chart.juNumber }}局
      </span>
      <span class="text-xs text-cinnabar">
        值符：{{ dictStore.dict?.qiMenStars[data.chart.zhiFuStar] }} · 值使：{{ dictStore.dict?.qiMenDoors[data.chart.zhiShiDoor] }}
      </span>
    </div>

    <div class="grid grid-cols-3 bg-paper-border gap-[1px]">
      <div
        v-for="p in orderedPalaces"
        :key="p.idx"
        class="p-2 bg-paper-card flex flex-col justify-between min-h-[100px]"
        :class="{ 'ring-2 ring-inset ring-cinnabar bg-cinnabar-light/20': p.isZhiFu }"
      >
        <div class="flex items-center justify-between text-xs">
          <span class="font-bold text-ink-light">{{ p.god }}</span>
          <span class="text-ink-muted">{{ p.posName }}</span>
        </div>
        <div class="my-1 flex items-center justify-between">
          <div>
            <div class="text-xs font-bold text-wuxing-water">{{ p.star }}</div>
            <div class="text-xs font-bold text-cinnabar mt-0.5">{{ p.door }}</div>
          </div>
          <div class="text-right font-serif">
            <div class="text-sm font-bold text-ink">天:{{ p.heaven }}</div>
            <div class="text-sm font-bold text-ink-light">地:{{ p.earth }}</div>
          </div>
        </div>
        <div class="flex items-center justify-between text-xs border-t border-dashed border-paper-border pt-1">
          <span class="text-ink-muted">隐:{{ p.hidden }}</span>
          <div class="space-x-0.5">
            <span v-if="p.isZhiFu" class="px-1 rounded bg-cinnabar text-white text-[10px]">符</span>
            <span v-if="p.isZhiShi" class="px-1 rounded bg-gold text-white text-[10px]">使</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>