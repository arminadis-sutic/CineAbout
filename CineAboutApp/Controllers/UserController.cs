using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests;
using CineAboutApp.Models.Requests.User;
using CineAboutApp.Models.Responses.User;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class UserController(IUserService service, IIdentityService identityService) : BaseCRUDController<UserResponse, UserSearchObject, UserInsertOrUpdateRequest, UserInsertOrUpdateRequest>(service)
    {
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Get(UserSearchObject search, CancellationToken cancellationToken = default)
        {
            return base.Get(search, cancellationToken);
        }

        [Authorize]
        public override async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
        {
            var currentUserId = identityService.GetCurrentUserId();
            var isAdmin = identityService.IsAdmin();
            if (!isAdmin && currentUserId != id)
                throw new UnauthorizedAccessException("Možete pristupiti samo vlastitom profilu.");
            return await base.GetById(id, cancellationToken);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            var response = await service.LoginAsync(request, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("logout")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken = default)
        {
            var result = await service.LogoutAsync(request, cancellationToken);
            return Ok(result);
        }
        [HttpPost("refresh")]
        [EnableRateLimiting("auth")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken = default)
        {
            var result = await service.GetRefreshedJwt(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUser([FromBody] UserRegisterRequest request, CancellationToken cancellationToken = default)
        {
            var response = await service.RegisterUser(request, cancellationToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var currentUserId = identityService.GetCurrentUserId();
            var response = await service.UpdateUser(currentUserId, request, cancellationToken);
            return Ok(response);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var response = await service.UpdateUser(id, request, cancellationToken);
            return Ok(response);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> SendResetRequest([FromBody] PasswordResetInitiateRequest request, CancellationToken cancellationToken = default)
        {
            var response = await service.RequestPasswordReset(request.Email, cancellationToken);
            return Ok(response);
        }

        [HttpPost("reset-password/confirm")]
        [AllowAnonymous]
        [EnableRateLimiting("password-reset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetRequest request, CancellationToken cancellationToken = default)
        {
            var response = await service.ResetPassword(request, cancellationToken);
            return Ok(response);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost("activate-deactivate/{id}")]
        public async Task<IActionResult> ActivateOrDeactivateProfile(int id, CancellationToken cancellationToken = default)
        {
            var response = await service.ActivateDeactivateUserAsync(id, cancellationToken);
            return Ok(response);
        }

        [Authorize(Roles = AppRoles.Admin)]
        public override async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            return await base.Delete(id, cancellationToken);
        }

        [NonAction]
        public override Task<IActionResult> Create([FromBody] UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            return base.Create(request, cancellationToken);
        }

        [NonAction]
        public override Task<IActionResult> Update(int id, [FromBody] UserInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
        {
            return base.Update(id, request, cancellationToken);
        }
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserResponse>> GetMyProfile(CancellationToken cancellationToken = default)
        {
            var result = await service.GetMyProfileAsync(cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }


        [Authorize]
        [HttpPost("register-device")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request, CancellationToken cancellationToken = default)
        {
            await service.RegisterDeviceToken(request, cancellationToken);
            return Ok();
        }


    }
}

