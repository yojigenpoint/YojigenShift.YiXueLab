import http from './http';
import type {
  AuthTokenResponseDto,
  CaseDetailDto,
  CaseStatus,
  CaseSummaryDto,
  DivinationPayload,
  DivinationType,
  PagedResultDto,
  PolishReadingResponseDto,
  PublishCaseRequestDto,
  ReadingDto,
  SuggestTagsResponseDto,
  YiGlobalDictionary
} from '../types/yixue';

export const metaApi = {
  /** 获取 YiFramework 全局多语言术语字典 */
  async getDictionary(): Promise<YiGlobalDictionary> {
    const { data } = await http.get<YiGlobalDictionary>('/meta/dictionary');
    return data;
  },

  /** 发布前实时预览排盘结果 */
  async previewPlot(payload: PublishCaseRequestDto): Promise<DivinationPayload> {
    const { data } = await http.post<DivinationPayload>('/meta/preview-plot', payload);
    return data;
  }
};

export const authApi = {
  /** 账号密码登录 */
  async login(username: string, password: string): Promise<AuthTokenResponseDto> {
    const { data } = await http.post<AuthTokenResponseDto>('/auth/login', { username, password });
    return data;
  },

  /** 凭内部邀请码注册 */
  async register(
    username: string,
    password: string,
    wechatNickname: string,
    inviteCode: string
  ): Promise<AuthTokenResponseDto> {
    const { data } = await http.post<AuthTokenResponseDto>('/auth/register', {
      username,
      password,
      wechatNickname,
      inviteCode
    });
    return data;
  },

  /** 管理员专属：生成一枚新群友邀请码 */
  async generateInviteCode(): Promise<string> {
    const { data } = await http.post<{ inviteCode: string }>('/auth/invite-codes');
    return data.inviteCode;
  }
};

export const casesApi = {
  /** 获取演武场最新的一道进行中盲断题 */
  async getLatestBlindCase(type?: DivinationType): Promise<CaseDetailDto> {
    const { data } = await http.get<CaseDetailDto>('/cases/latest-blind', {
      params: type !== undefined ? { type } : {}
    });
    return data;
  },

  /** 根据 ID 获取案例详情与复盘数据 */
  async getCaseDetail(caseId: string): Promise<CaseDetailDto> {
    const { data } = await http.get<CaseDetailDto>(`/cases/${caseId}`);
    return data;
  },

  /** 藏经阁分页检索案例 */
  async searchCases(params: {
    status?: CaseStatus;
    divinationType?: DivinationType;
    keyword?: string;
    tagIds?: string[];
    pageIndex?: number;
    pageSize?: number;
  }): Promise<PagedResultDto<CaseSummaryDto>> {
    const { data } = await http.get<PagedResultDto<CaseSummaryDto>>('/cases', { params });
    return data;
  },

  /** 发布新盲断案例 */
  async publishCase(payload: PublishCaseRequestDto): Promise<string> {
    const { data } = await http.post<{ caseId: string }>('/cases', payload);
    return data.caseId;
  },

  /** 提交盲断分析（交卷并解锁他人断语） */
  async submitReading(caseId: string, content: string): Promise<ReadingDto> {
    const { data } = await http.post<ReadingDto>(`/cases/${caseId}/readings`, { content });
    return data;
  },

  /** 为群友断语点赞 (+1) */
  async likeReading(readingId: string): Promise<void> {
    await http.post(`/cases/readings/${readingId}/like`);
  }
};

export const aiApi = {
  /** 调用 AI 将断语草稿梳理为对应门类的三段式标准结构 */
  async polishReading(
    divinationType: DivinationType,
    rawContent: string,
    questionContext?: string
  ): Promise<PolishReadingResponseDto> {
    const { data } = await http.post<PolishReadingResponseDto>('/ai/polish-reading', {
      divinationType,
      rawContent,
      questionContext
    });
    return data;
  },

  /** 发布案例时，调用 AI 根据背景、问题与反馈自动提炼考点标签 */
  async suggestTags(
    divinationType: DivinationType,
    backgroundInfo: string,
    questionText: string,
    realFeedback?: string
  ): Promise<SuggestTagsResponseDto> {
    const { data } = await http.post<SuggestTagsResponseDto>('/ai/suggest-tags', {
      divinationType,
      backgroundInfo,
      questionText,
      realFeedback
    });
    return data;
  }
};