using YojigenShift.YiXueLab.BLL.Common;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.DAL.JsonModels;

namespace YojigenShift.YiXueLab.BLL.Services.Interfaces
{
    public interface IDivinationPlotterService
    {
        ServiceResult<DivinationPayloadBase> Plot(PublishCaseRequestDto request);
        object GetGlobalLocalizedDictionary();
    }
}
