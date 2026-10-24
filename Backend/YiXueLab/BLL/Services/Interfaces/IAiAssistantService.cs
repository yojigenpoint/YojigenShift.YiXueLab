using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;

namespace YojigenShift.YiXueLab.BLL.Services.Interfaces
{
    public interface IAiAssistantService
    {
        Task<ServiceResult<PolishReadingResponseDto>> PolishReadingAsync(
            PolishReadingRequestDto req,
            CancellationToken ct = default);

        Task<ServiceResult<SuggestTagsResponseDto>> SuggestTagsAsync(
            SuggestTagsRequestDto req,
            CancellationToken ct = default);
    }
}
