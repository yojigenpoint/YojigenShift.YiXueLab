import { defineStore } from 'pinia';
import { ref } from 'vue';
import { metaApi } from '../api/yixueApi';
import { WuXingType, type YiGlobalDictionary } from '../types/yixue';

export const useDictStore = defineStore('yi-dict', () => {
  const dict = ref<YiGlobalDictionary | null>(null);
  const isLoaded = ref(false);

  // 天干 (0-9) 对应的五行映射：甲乙木、丙丁火、戊己土、庚辛金、壬癸水
  const stemWuXingMap: WuXingType[] = [
    WuXingType.Wood, WuXingType.Wood,
    WuXingType.Fire, WuXingType.Fire,
    WuXingType.Earth, WuXingType.Earth,
    WuXingType.Metal, WuXingType.Metal,
    WuXingType.Water, WuXingType.Water
  ];

  // 地支 (0-11) 对应的五行映射：子水、丑土、寅卯木、辰土、巳午火、未土、申酉金、戌土、亥水
  const branchWuXingMap: WuXingType[] = [
    WuXingType.Water, WuXingType.Earth, WuXingType.Wood, WuXingType.Wood,
    WuXingType.Earth, WuXingType.Fire, WuXingType.Fire, WuXingType.Earth,
    WuXingType.Metal, WuXingType.Metal, WuXingType.Earth, WuXingType.Water
  ];

  /** 初始化拉取全局字典（优先读取 sessionStorage 缓存） */
  async function ensureLoaded() {
    if (isLoaded.value && dict.value) return;

    const cached = sessionStorage.getItem('yixuelab_global_dict');
    if (cached) {
      dict.value = JSON.parse(cached);
      isLoaded.value = true;
      return;
    }

    dict.value = await metaApi.getDictionary();
    sessionStorage.setItem('yixuelab_global_dict', JSON.stringify(dict.value));
    isLoaded.value = true;
  }

  /** 将 0-59 的六十甲子索引拆解为天干、地支、中文名称与五行颜色类名 */
  function resolveGanZhi(pillarIdx: number) {
    const normalized = ((pillarIdx % 60) + 60) % 60;
    const stemIdx = normalized % 10;
    const branchIdx = normalized % 12;

    const stemName = dict.value?.stems[stemIdx] ?? '';
    const branchName = dict.value?.branches[branchIdx] ?? '';

    const stemWx = stemWuXingMap[stemIdx];
    const branchWx = branchWuXingMap[branchIdx];

    return {
      stemIdx,
      branchIdx,
      stemName,
      branchName,
      fullName: `${stemName}${branchName}`,
      stemColorClass: getWuXingTextColor(stemWx),
      branchColorClass: getWuXingTextColor(branchWx)
    };
  }

  /** 获取五行对应的 Tailwind 字体颜色类名 */
  function getWuXingTextColor(wx: WuXingType): string {
    switch (wx) {
      case WuXingType.Wood: return 'text-wuxing-wood';
      case WuXingType.Fire: return 'text-wuxing-fire';
      case WuXingType.Earth: return 'text-wuxing-earth';
      case WuXingType.Metal: return 'text-wuxing-metal';
      case WuXingType.Water: return 'text-wuxing-water';
      default: return 'text-ink';
    }
  }

  return {
    dict,
    isLoaded,
    ensureLoaded,
    resolveGanZhi,
    getWuXingTextColor
  };
});