using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.UOW;

namespace YojigenShift.YiXueLab.BLL.Services
{
    public class CaseService : ICaseService
    {
        private readonly IUnitOfWork _uow;
        private readonly IDivinationPlotterService _plotter;

        public CaseService(IUnitOfWork uow, IDivinationPlotterService plotter)
        {
            _uow = uow;
            _plotter = plotter;
        }

        /// <summary>
        /// 发布新盲断案例：校验权限 -> 调用 YiFramework 自动排盘 -> 自动去重创建标签 -> 事务落盘
        /// </summary>
        public async Task<ServiceResult<Guid>> PublishCaseAsync(
            Guid publisherId,
            PublishCaseRequestDto req,
            CancellationToken ct = default)
        {
            var user = await _uow.Users.GetByIdAsync(publisherId, ct);
            if (user == null || (user.Role != UserRole.Publisher && user.Role != UserRole.Admin))
                return ServiceResult<Guid>.Failure("只有案例发布者或管理员可以发布新案例。", ServiceErrorCode.Forbidden);

            if (string.IsNullOrWhiteSpace(req.BackgroundInfo) || string.IsNullOrWhiteSpace(req.QuestionText))
                return ServiceResult<Guid>.Failure("案例背景与核心盲断问题不能为空。");

            // 1. 调用排盘引擎生成多态排盘数据
            var plotResult = _plotter.Plot(req);
            if (!plotResult.IsSuccess || plotResult.Data == null)
                return ServiceResult<Guid>.Failure(plotResult.ErrorMessage ?? "排盘失败", plotResult.ErrorCode);

            await _uow.BeginTransactionAsync(ct);

            // 2. 智能解析并复用标签
            var tagEntities = await _uow.Tags.GetOrCreateTagsAsync(req.Tags ?? new List<string>(), ct: ct);

            // 3. 构建案例实体
            var newCase = new DivinationCaseEntity
            {
                PublisherId = publisherId,
                DivinationType = req.DivinationType,
                DivinationData = plotResult.Data,
                BackgroundInfo = req.BackgroundInfo.Trim(),
                QuestionText = req.QuestionText.Trim(),
                RealFeedback = req.RealFeedback?.Trim(),
                Status = CaseStatus.Blind,
                RevealTime = req.RevealTimeUtc.Kind == DateTimeKind.Utc
                    ? req.RevealTimeUtc
                    : DateTime.SpecifyKind(req.RevealTimeUtc, DateTimeKind.Utc)
            };

            foreach (var tag in tagEntities)
            {
                newCase.CaseTags.Add(new CaseTagEntity
                {
                    CaseId = newCase.Id,
                    TagId = tag.Id,
                    Tag = tag
                });
            }

            await _uow.Cases.AddAsync(newCase, ct);
            await _uow.CommitTransactionAsync(ct);

            return ServiceResult<Guid>.Success(newCase.Id);
        }

        /// <summary>
        /// 获取案例详情（内置自动到期揭晓 + 盲断逻辑锁脱敏）
        /// </summary>
        public async Task<ServiceResult<CaseDetailDto>> GetCaseDetailAsync(
            Guid caseId,
            Guid currentUserId,
            CancellationToken ct = default)
        {
            var entity = await _uow.Cases.GetCaseWithDetailsAsync(caseId, ct);
            if (entity == null)
                return ServiceResult<CaseDetailDto>.Failure("未找到指定的易学案例。", ServiceErrorCode.NotFound);

            return await BuildCaseDetailWithBlindLockAsync(entity, currentUserId, ct);
        }

        /// <summary>
        /// 获取演武场最新的一道盲断题
        /// </summary>
        public async Task<ServiceResult<CaseDetailDto>> GetLatestBlindCaseAsync(
            Guid currentUserId,
            DivinationType? type = null,
            CancellationToken ct = default)
        {
            // 先顺便触发一次到期案例批量自动揭晓
            await _uow.Cases.RevealDueCasesAsync(DateTime.UtcNow, ct);

            var latestBlind = await _uow.Cases.GetLatestBlindCaseAsync(type, ct);
            if (latestBlind == null)
                return ServiceResult<CaseDetailDto>.Failure("当前暂无进行中的盲断案例。", ServiceErrorCode.NotFound);

            // 重新拉取带完整断语的详情并应用盲断锁
            return await GetCaseDetailAsync(latestBlind.Id, currentUserId, ct);
        }

        /// <summary>
        /// 藏经阁/演武场多条件分页搜索
        /// </summary>
        public async Task<ServiceResult<PagedResultDto<CaseSummaryDto>>> SearchCasesAsync(
            CaseSearchFilterDto filter,
            CancellationToken ct = default)
        {
            int pageIndex = Math.Max(1, filter.PageIndex);
            int pageSize = Math.Clamp(filter.PageSize, 1, 50);

            var (items, totalCount) = await _uow.Cases.SearchCasesAsync(
                filter.Status,
                filter.DivinationType,
                filter.Keyword,
                filter.TagIds,
                pageIndex,
                pageSize,
                ct);

            var summaryList = items.Select(c => new CaseSummaryDto(
                c.Id,
                c.DivinationType,
                c.Publisher?.WechatNickname ?? "匿名发布者",
                c.BackgroundInfo,
                c.QuestionText,
                c.Status,
                c.RevealTime,
                c.CreatedAt,
                c.CaseTags.Select(ct => ct.Tag.Name).ToList()
            )).ToList();

            return ServiceResult<PagedResultDto<CaseSummaryDto>>.Success(
                new PagedResultDto<CaseSummaryDto>(summaryList, totalCount, pageIndex, pageSize));
        }

        /// <summary>
        /// 群友提交断语（交卷解锁他人断语）
        /// </summary>
        public async Task<ServiceResult<ReadingDto>> SubmitReadingAsync(
            Guid userId,
            SubmitReadingRequestDto req,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(req.Content) || req.Content.Trim().Length < 5)
                return ServiceResult<ReadingDto>.Failure("断语内容过于简短，请至少输入 5 个字的分析思路。");

            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null || !user.IsActive)
                return ServiceResult<ReadingDto>.Failure("无效的用户状态。", ServiceErrorCode.Forbidden);

            var caseExists = await _uow.Cases.AnyAsync(c => c.Id == req.CaseId, ct);
            if (!caseExists)
                return ServiceResult<ReadingDto>.Failure("所属案例不存在。", ServiceErrorCode.NotFound);

            // 检查是否已提交过（一人一案一贴）
            bool alreadySubmitted = await _uow.Readings.HasUserSubmittedAsync(req.CaseId, userId, ct);
            if (alreadySubmitted)
                return ServiceResult<ReadingDto>.Failure("您已对该案例提交过断语，请勿重复提交。", ServiceErrorCode.Conflict);

            var reading = new ReadingEntity
            {
                CaseId = req.CaseId,
                UserId = userId,
                Content = req.Content.Trim(),
                LikesCount = 0
            };

            await _uow.Readings.AddAsync(reading, ct);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<ReadingDto>.Success(new ReadingDto(
                reading.Id,
                user.Id,
                user.WechatNickname,
                reading.Content,
                reading.LikesCount,
                reading.CreatedAt,
                IsCurrentUser: true
            ));
        }

        public async Task<ServiceResult> LikeReadingAsync(Guid readingId, CancellationToken ct = default)
        {
            bool updated = await _uow.Readings.IncrementLikesAsync(readingId, ct);
            return updated
                ? ServiceResult.Success()
                : ServiceResult.Failure("未找到该条断语。", ServiceErrorCode.NotFound);
        }

        // =====================================================================
        // 核心私有方法：盲断防作弊逻辑锁 (The Blind-Reading Guard)
        // =====================================================================
        private async Task<ServiceResult<CaseDetailDto>> BuildCaseDetailWithBlindLockAsync(
            DivinationCaseEntity entity,
            Guid currentUserId,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // 1. 懒加载自动揭晓：如果当前状态还是 Blind，但已经过了揭晓时间，立即自动开奖！
            if (entity.Status == CaseStatus.Blind && entity.RevealTime <= now)
            {
                await _uow.Cases.RevealDueCasesAsync(now, ct);
                entity.Status = CaseStatus.Revealed;
            }

            // 2. 判断当前用户是否已经提交过断语
            bool hasUserSubmitted = entity.Readings.Any(r => r.UserId == currentUserId);

            // 3. 判断当前用户是否是该案例的发布者本人（发布者可随时查看）
            bool isPublisher = entity.PublisherId == currentUserId;

            // 4. 【核心铁律1】：只要案例尚未揭晓 (Status == Blind)，任何人都看不到真实反馈答案 (RealFeedback)
            // （防止有人先注册小号随便水一条断语就看到真实答案）
            string? visibleFeedback = entity.Status == CaseStatus.Revealed
                ? entity.RealFeedback
                : null;

            // 5. 【核心铁律2】：如果案例处于盲断期，且当前用户既没交卷、也不是发布者，则锁定他人断语列表！
            bool isReadingsLocked = (entity.Status == CaseStatus.Blind) && !hasUserSubmitted && !isPublisher;

            var visibleReadings = isReadingsLocked
                ? new List<ReadingDto>() // 彻底清空，防止前端通过 F12 网络面板偷看
                : entity.Readings.Select(r => new ReadingDto(
                    r.Id,
                    r.UserId,
                    r.User?.WechatNickname ?? "群友",
                    r.Content,
                    r.LikesCount,
                    r.CreatedAt,
                    IsCurrentUser: r.UserId == currentUserId
                )).ToList();

            var detailDto = new CaseDetailDto(
                Id: entity.Id,
                DivinationType: entity.DivinationType,
                PublisherNickname: entity.Publisher?.WechatNickname ?? "管理员",
                DivinationData: entity.DivinationData,
                BackgroundInfo: entity.BackgroundInfo,
                QuestionText: entity.QuestionText,
                Status: entity.Status,
                RevealTime: entity.RevealTime,
                CreatedAt: entity.CreatedAt,
                Tags: entity.CaseTags.Select(ct => ct.Tag.Name).ToList(),
                HasCurrentUserSubmitted: hasUserSubmitted,
                IsReadingsLocked: isReadingsLocked,
                RealFeedback: visibleFeedback,
                Readings: visibleReadings
            );

            return ServiceResult<CaseDetailDto>.Success(detailDto);
        }
    }
}
