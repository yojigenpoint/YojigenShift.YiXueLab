using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using YojigenShift.YiXueLab.API.Auth;
using YojigenShift.YiXueLab.BLL.Models;
using YojigenShift.YiXueLab.BLL.Services.Interfaces;
using YojigenShift.YiXueLab.DAL.Enums;

namespace YojigenShift.YiXueLab.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthController(IAuthService authService, IJwtTokenService jwtTokenService)
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
        }

        /// <summary>
        /// 凭内部邀请码注册新账号，成功后直接签发 JWT Token
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequestDto request,
            CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(request, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                return FromServiceResult(result);
            }

            var tokenResponse = _jwtTokenService.GenerateToken(result.Data);
            return Ok(tokenResponse);
        }

        /// <summary>
        /// 账号密码登录，签发 JWT Token
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request,
            CancellationToken ct)
        {
            var result = await _authService.ValidateCredentialsAsync(request, ct);
            if (!result.IsSuccess || result.Data == null)
            {
                return FromServiceResult(result);
            }

            var tokenResponse = _jwtTokenService.GenerateToken(result.Data);
            return Ok(tokenResponse);
        }

        /// <summary>
        /// 管理员专属：生成一枚新的内部群友注册邀请码 (如 YX-8F3A9C)
        /// </summary>
        [HttpPost("invite-codes")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> GenerateInviteCode(CancellationToken ct)
        {
            var result = await _authService.GenerateInviteCodeAsync(CurrentUserId, ct);
            if (!result.IsSuccess)
            {
                return FromServiceResult(result);
            }

            return Ok(new { inviteCode = result.Data });
        }

        /// <summary>
        /// 获取当前登录用户的令牌解析信息（用于前端路由守卫校验登录态）
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            return Ok(new
            {
                id = CurrentUserId,
                username = User.Identity?.Name,
                nickname = User.FindFirstValue("nickname"),
                role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
