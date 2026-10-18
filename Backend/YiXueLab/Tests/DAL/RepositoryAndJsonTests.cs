using FluentAssertions;
using YojigenShift.YiFramework.Core;
using YojigenShift.YiFramework.Enums;
using YojigenShift.YiFramework.LiuYao.Logic;
using YojigenShift.YiFramework.LiuYao.Models;
using YojigenShift.YiXueLab.DAL.Entities;
using YojigenShift.YiXueLab.DAL.Enums;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.Tests.DAL
{
    public class RepositoryAndJsonTests : DalTestBase
    {
        [Fact(DisplayName = "多态 JSONB 测试：八字与六爻排盘结果（含 readonly struct）存取后应完整还原")]
        public async Task CaseRepository_Should_Persist_And_Restore_Polymorphic_YiFramework_Models()
        {
            // Arrange
            var publisher = await SeedTestUserAsync("admin_pub", UserRole.Admin);
            var castTime = new DateTime(2024, 10, 5, 9, 15, 0, DateTimeKind.Utc);
            var (y, m, d, h) = BaziPlotter.Plot(castTime);

            // 使用 YiFramework 真实排出一卦六爻
            var rawLines = new List<YaoType>
            {
                YaoType.YoungYang, YaoType.YoungYin, YaoType.OldYang,
                YaoType.YoungYang, YaoType.OldYin, YaoType.YoungYin
            };
            var castResult = new LiuYaoResult { Lines = rawLines };
            var (orig, fut) = NaJiaLogic.Mount(castResult);
            var dayStem = GanZhiMath.GetStem(d);
            var dayBranch = GanZhiMath.GetBranch(d);
            var monthBranch = GanZhiMath.GetBranch(m);
            SixBeastsLogic.AttachContext(orig, dayStem, dayBranch);
            SixBeastsLogic.AttachContext(fut, dayStem, dayBranch);
            var eval = LiuYaoEvaluator.Evaluate(orig, monthBranch, dayBranch);

            var liuYaoCase = new DivinationCaseEntity
            {
                PublisherId = publisher.Id,
                DivinationType = DivinationType.LiuYao,
                BackgroundInfo = "测试金项链失物寻物",
                QuestionText = "能否找回？",
                RevealTime = castTime.AddDays(1),
                DivinationData = new LiuYaoCaseData
                {
                    CastTimeUtc = castTime,
                    Pillars = new FourPillarsDto(y, m, d, h),
                    RawLines = rawLines,
                    MovingLines = castResult.MovingLines,
                    Original = orig,
                    Future = fut,
                    Evaluation = eval
                }
            };

            // Act
            await Uow.Cases.AddAsync(liuYaoCase);
            await Uow.SaveChangesAsync();

            // 清空 EF 追踪缓存，强制从数据库反序列化读取
            DbContext.ChangeTracker.Clear();
            var loadedCase = await Uow.Cases.GetCaseWithDetailsAsync(liuYaoCase.Id);

            // Assert
            loadedCase.Should().NotBeNull();
            loadedCase!.DivinationData.Should().BeOfType<LiuYaoCaseData>("多态反序列化应自动识别为 LiuYaoCaseData 子类");

            var loadedPayload = (LiuYaoCaseData)loadedCase.DivinationData;
            loadedPayload.Pillars.DayIdx.Should().Be(d);
            loadedPayload.MovingLines.Should().BeEquivalentTo(new[] { 3, 5 });
            loadedPayload.Original.Palace.Should().Be(orig.Palace);
            loadedPayload.Original.Lines.Should().HaveCount(6);
            loadedPayload.Evaluation.Should().ContainKey(1);
        }

        [Fact(DisplayName = "标签智能去重测试：GetOrCreateTagsAsync 应自动去除井号并复用已存在标签")]
        public async Task TagRepository_GetOrCreateTagsAsync_Should_Normalize_And_Reuse_Existing()
        {
            // Arrange: 先预先存入一个 "婚姻" 标签
            await Uow.Tags.AddAsync(new TagEntity { Name = "婚姻", Category = "事项" });
            await Uow.SaveChangesAsync();

            // Act: 传入带 "#"、带空格、重复名称以及新名称的集合
            var inputNames = new[] { " #婚姻 ", "婚姻", "#辰戌冲", "#事业变动", "" };
            var tags = await Uow.Tags.GetOrCreateTagsAsync(inputNames, "默认分类");
            await Uow.SaveChangesAsync();

            // Assert
            tags.Should().HaveCount(3);
            tags.Select(t => t.Name).Should().BeEquivalentTo(new[] { "婚姻", "辰戌冲", "事业变动" });

            // 数据库总标签数也应当只有 3 个（没有重复创建 "婚姻"）
            int totalTagsInDb = await Uow.Tags.CountAsync();
            totalTagsInDb.Should().Be(3);
        }

        [Fact(DisplayName = "到期揭晓测试：RevealDueCasesAsync 应只将已到期的盲断案例更新为 Revealed")]
        public async Task CaseRepository_RevealDueCasesAsync_Should_Only_Update_Due_Cases()
        {
            // Arrange
            var publisher = await SeedTestUserAsync("pub_user");
            var now = DateTime.UtcNow;

            var dueCase = new DivinationCaseEntity
            {
                PublisherId = publisher.Id,
                DivinationType = DivinationType.Bazi,
                BackgroundInfo = "已到期案例",
                QuestionText = "问题1",
                Status = CaseStatus.Blind,
                RevealTime = now.AddHours(-1), // 1小时前就该揭晓了
                DivinationData = new BaziCaseData { CastTimeUtc = now, Gender = Gender.Male }
            };

            var futureCase = new DivinationCaseEntity
            {
                PublisherId = publisher.Id,
                DivinationType = DivinationType.Bazi,
                BackgroundInfo = "未到期案例",
                QuestionText = "问题2",
                Status = CaseStatus.Blind,
                RevealTime = now.AddHours(5), // 5小时后才揭晓
                DivinationData = new BaziCaseData { CastTimeUtc = now, Gender = Gender.Female }
            };

            await Uow.Cases.AddRangeAsync(new[] { dueCase, futureCase });
            await Uow.SaveChangesAsync();

            // Act
            int revealedCount = await Uow.Cases.RevealDueCasesAsync(now);

            // Assert
            revealedCount.Should().Be(1);

            DbContext.ChangeTracker.Clear();
            var updatedDueCase = await Uow.Cases.GetByIdAsync(dueCase.Id);
            var updatedFutureCase = await Uow.Cases.GetByIdAsync(futureCase.Id);

            updatedDueCase!.Status.Should().Be(CaseStatus.Revealed);
            updatedDueCase.ModifiedAt.Should().NotBeNull();
            updatedFutureCase!.Status.Should().Be(CaseStatus.Blind);
        }

        [Fact(DisplayName = "断语点赞测试：IncrementLikesAsync 应通过原子 SQL 将点赞数加 1")]
        public async Task ReadingRepository_IncrementLikesAsync_Should_Increase_Likes_Atomically()
        {
            // Arrange
            var user = await SeedTestUserAsync("reader_1", UserRole.User);
            var divCase = new DivinationCaseEntity
            {
                PublisherId = user.Id,
                BackgroundInfo = "背景",
                QuestionText = "问题",
                RevealTime = DateTime.UtcNow.AddDays(1),
                DivinationData = new BaziCaseData { CastTimeUtc = DateTime.UtcNow }
            };
            await Uow.Cases.AddAsync(divCase);

            var reading = new ReadingEntity
            {
                CaseId = divCase.Id,
                UserId = user.Id,
                Content = "我认为2018戊戌年辰戌冲，必有工作变动。",
                LikesCount = 0
            };
            await Uow.Readings.AddAsync(reading);
            await Uow.SaveChangesAsync();

            // Act
            bool success1 = await Uow.Readings.IncrementLikesAsync(reading.Id);
            bool success2 = await Uow.Readings.IncrementLikesAsync(reading.Id);

            // Assert
            success1.Should().BeTrue();
            success2.Should().BeTrue();

            DbContext.ChangeTracker.Clear();
            var updatedReading = await Uow.Readings.GetByIdAsync(reading.Id);
            updatedReading!.LikesCount.Should().Be(2);
            updatedReading.ModifiedAt.Should().NotBeNull();
        }
    }
}
