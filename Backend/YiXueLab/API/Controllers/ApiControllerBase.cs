using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using YojigenShift.YiXueLab.API.Common;
using YojigenShift.YiXueLab.BLL.Common;

namespace YojigenShift.YiXueLab.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        /// 从当前 HTTP 请求的 JWT Claims 中安全提取当前登录用户的 Guid
        /// </summary>
        protected Guid CurrentUserId
        {
            get
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

                return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
            }
        }

        /// <summary>
        /// 将 BLL 的无返回值 ServiceResult 自动映射为标准 HTTP 响应
        /// </summary>
        protected IActionResult FromServiceResult(ServiceResult result)
        {
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return MapErrorToHttpResult(result.ErrorCode, result.ErrorMessage);
        }

        /// <summary>
        /// 将 BLL 的泛型 ServiceResult<T> 自动映射为标准 HTTP 响应
        /// </summary>
        protected IActionResult FromServiceResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess)
            {
                return new OkObjectResult(result.Data)
                {
                    DeclaredType = typeof(T)
                };
            }

            return MapErrorToHttpResult(result.ErrorCode, result.ErrorMessage);
        }

        private IActionResult MapErrorToHttpResult(ServiceErrorCode errorCode, string? message)
        {
            int statusCode = errorCode switch
            {
                ServiceErrorCode.ValidationError => StatusCodes.Status400BadRequest,
                ServiceErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
                ServiceErrorCode.Forbidden => StatusCodes.Status403Forbidden,
                ServiceErrorCode.NotFound => StatusCodes.Status404NotFound,
                ServiceErrorCode.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            var errorPayload = new ApiErrorResponse(
                Code: statusCode,
                Message: message ?? "请求处理失败。",
                TimestampUtc: DateTime.UtcNow
            );

            return StatusCode(statusCode, errorPayload);
        }
    }
}
