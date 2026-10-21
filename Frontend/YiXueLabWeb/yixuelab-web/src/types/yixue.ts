// ============================================================================
// 1. 基础常量枚举定义 (兼容 erasableSyntaxOnly: true，与后端 DAL/YiFramework 严格对齐)
// ============================================================================

export const UserRole = {
  User: 0,       // 普通群友
  Publisher: 1,  // 案例收集志愿者/发布者
  Admin: 2       // 群主管理员
} as const;
export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export const DivinationType = {
  Bazi: 0,       // 四柱八字
  LiuYao: 1,     // 六爻纳甲
  QiMen: 2,      // 奇门遁甲
  MeiHua: 3,     // 梅花易数
  Other: 99
} as const;
export type DivinationType = (typeof DivinationType)[keyof typeof DivinationType];

export const CaseStatus = {
  Blind: 0,      // 盲断进行中
  Revealed: 1    // 已揭晓答案
} as const;
export type CaseStatus = (typeof CaseStatus)[keyof typeof CaseStatus];

export const Gender = {
  Female: 0,
  Male: 1
} as const;
export type Gender = (typeof Gender)[keyof typeof Gender];

export const WuXingType = {
  Wood: 0,
  Fire: 1,
  Earth: 2,
  Metal: 3,
  Water: 4
} as const;
export type WuXingType = (typeof WuXingType)[keyof typeof WuXingType];

// ============================================================================
// 2. 全局易学多语言字典接口 (对应 GET /api/meta/dictionary)
// ============================================================================

export interface YiGlobalDictionary {
  wuXing: Record<number, string>;
  stems: Record<number, string>;
  branches: Record<number, string>;
  trigrams: Record<number, string>;
  tenGods: Record<number, string>;
  lifeStages: Record<number, string>;
  sixRelations: Record<number, string>;
  sixBeasts: Record<number, string>;
  yaoTypes: Record<number, string>;
  hexagramNames: Record<number, string>;
  qiMenStars: Record<number, string>;
  qiMenDoors: Record<number, string>;
  qiMenGods: Record<number, string>;
  solarTerms: Record<number, string>;
}

// ============================================================================
// 3. 四大门类 JSONB 多态排盘数据结构 (对应 DivinationPayloadBase 及子类)
// ============================================================================

export interface FourPillarsDto {
  yearIdx: number;  // 0-59 六十甲子索引
  monthIdx: number;
  dayIdx: number;
  hourIdx: number;
}

export interface DivinationPayloadBase {
  $type: 'bazi' | 'liuyao' | 'qimen' | 'meihua';
  castTimeUtc: string;
  pillars: FourPillarsDto;
}

// --- 3.1 八字排盘数据 (BaziCaseData) ---
export interface DaYunResult {
  isForward: boolean;
  startYearAge: number;
  startMonthAge: number;
  startDayAge: number;
  startDateUtc: string;
  daYunPillars: number[]; // 10步大运的 0-59 干支索引
}

export interface BaziCaseData extends DivinationPayloadBase {
  $type: 'bazi';
  gender: Gender;
  location?: string;
  isTrueSolarTime: boolean;
  daYun: DaYunResult;
}

// --- 3.2 六爻排盘数据 (LiuYaoCaseData) ---
export interface YaoInfo {
  index: number;       // 1-6 (初爻到上爻)
  type: number;        // 6=老阴, 7=少阳, 8=少阴, 9=老阳
  stem: number;        // HeavenlyStem
  branch: number;      // EarthlyBranch
  wuXing: WuXingType;
  isShi: boolean;
  isYing: boolean;
  relation: number;    // SixRelation (六亲)
  beast: number;       // SixBeast (六兽)
  isKongWang: boolean; // 是否旬空
}

export interface MountedHexagram {
  hexagram: {
    value: number;     // 0-63
    lower: number;     // TrigramName
    upper: number;     // TrigramName
  };
  palace: number;      // 所属八宫 (TrigramName)
  palaceWuXing: WuXingType;
  shiIndex: number;    // 世爻位置 (1-6)
  yingIndex: number;   // 应爻位置 (1-6)
  dayStem: number;
  dayBranch: number;
  lines: YaoInfo[];
}

export interface YaoStatus {
  isMoonBreak: boolean;
  isMonthCombine: boolean;
  isDayClash: boolean;
  isDayCombine: boolean;
  comments: string[];  // 例如 ["月破 (衰)", "日冲/暗动"]
}

export interface LiuYaoCaseData extends DivinationPayloadBase {
  $type: 'liuyao';
  rawLines: number[];  // [7, 8, 9, 7, 6, 8]
  movingLines: number[];
  original: MountedHexagram;
  future: MountedHexagram;
  evaluation: Record<number, YaoStatus>;
}

// --- 3.3 奇门遁甲排盘数据 (QiMenCaseData) ---
export interface QiMenPalace {
  index: number;       // 1-9 洛书宫位
  earthPlateStem: number;
  earthPlateParasiticStem?: number;
  heavenPlateStem: number;
  heavenPlateParasiticStem?: number;
  hiddenStem: number;
  star: number;
  parasiticStar?: number;
  door: number;
  god: number;
}

export interface QiMenChart {
  dun: number;         // 0=阴遁, 1=阳遁
  juNumber: number;
  yuan: number;
  term: number;
  hourStem: number;
  hourBranch: number;
  xunLeader: number;
  zhiFuStar: number;
  zhiShiDoor: number;
  palaces: Record<number, QiMenPalace>;
}

export interface QiMenCaseData extends DivinationPayloadBase {
  $type: 'qimen';
  chart: QiMenChart;
}

// --- 3.4 梅花易数排盘数据 (MeiHuaCaseData) ---
export interface MeiHuaGua {
  upper: number;       // TrigramName (由于后端开启了 IncludeFields = true，struct 字段直接序列化)
  lower: number;
}

export interface MeiHuaResult {
  original: MeiHuaGua;
  mutual: MeiHuaGua;
  changed: MeiHuaGua;
  movingLine: number;  // 1-6
}

export interface MeiHuaCaseData extends DivinationPayloadBase {
  $type: 'meihua';
  castMethod: 'Time' | 'Numbers';
  rawNumbers?: number[];
  result: MeiHuaResult;
}

// 联合类型：前端组件只需判断 payload.$type 即可获得完美代码提示
export type DivinationPayload =
  | BaziCaseData
  | LiuYaoCaseData
  | QiMenCaseData
  | MeiHuaCaseData;

// ============================================================================
// 4. API 请求与响应 DTOs
// ============================================================================

export interface UserProfileDto {
  id: string;
  username: string;
  wechatNickname: string;
  role: UserRole;
  createdAt: string;
}

export interface AuthTokenResponseDto {
  accessToken: string;
  expiresAtUtc: string;
  user: UserProfileDto;
}

export interface ReadingDto {
  id: string;
  userId: string;
  userNickname: string;
  content: string;
  likesCount: number;
  createdAt: string;
  isCurrentUser: boolean;
}

export interface CaseDetailDto {
  id: string;
  divinationType: DivinationType;
  publisherNickname: string;
  divinationData: DivinationPayload;
  backgroundInfo: string;
  questionText: string;
  status: CaseStatus;
  revealTime: string;
  createdAt: string;
  tags: string[];

  // 双层盲断防作弊锁控制字段
  hasCurrentUserSubmitted: boolean;
  isReadingsLocked: boolean;
  realFeedback?: string | null;
  readings: ReadingDto[];
}

export interface CaseSummaryDto {
  id: string;
  divinationType: DivinationType;
  publisherNickname: string;
  backgroundInfo: string;
  questionText: string;
  status: CaseStatus;
  revealTime: string;
  createdAt: string;
  tags: string[];
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
}

export interface PublishCaseRequestDto {
  divinationType: DivinationType;
  castTimeUtc: string;
  backgroundInfo: string;
  questionText: string;
  realFeedback?: string | null;
  revealTimeUtc: string;
  tags: string[];

  // 八字专属
  baziGender?: Gender | null;
  location?: string | null;
  isTrueSolarTime?: boolean;

  // 六爻专属 (长度为6的数组，值 6/7/8/9)
  liuYaoRawLines?: number[] | null;

  // 梅花专属
  meiHuaCastMethod?: 'Time' | 'Numbers' | null;
  meiHuaNumbers?: number[] | null;
  lunarMonth?: number | null;
  lunarDay?: number | null;
}