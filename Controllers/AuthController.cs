using Microsoft.AspNetCore.Mvc;
using ExpenseTracker.API.Repositories;
using ExpenseTracker.API.Services;
using ExpenseTracker.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.API.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepo;
        private readonly AuthRepository _authRepo;
        private readonly TokenService _tokenService;

        public AuthController(UserRepository userRepo, AuthRepository authRepo, TokenService tokenService)
        {
            _userRepo = userRepo;
            _authRepo = authRepo;
            _tokenService = tokenService;
        }

        // Login endpoint for Android app
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepo.GetUserByEmail(request.Email);
            if (user == null) return Unauthorized("User not found");

            var validPassword = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);
            if (!validPassword) return Unauthorized("Invalid password");

            var accessToken = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            await _authRepo.SaveRefreshToken(user.Id, refreshToken);

            return Ok(new
            {
                accessToken,
                refreshToken,
                expiresIn = 900
            });
        }


        // Refresh token endpoint for Android app
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            // Extract userId from claims in the expired access token (optional improvement)
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized("Missing user claim");

            var userId = int.Parse(userIdClaim);

            var valid = await _authRepo.ValidateRefreshToken(userId, request.RefreshToken);
            if (!valid) return Unauthorized("Invalid refresh token");

            var user = new User { Id = userId, Email = User.FindFirst("email")?.Value, Role = "User" };
            var newAccessToken = _tokenService.GenerateJwtToken(user);

            return Ok(new { accessToken = newAccessToken, expiresIn = 900 });
        }

        // Request password reset (Android app triggers this)
        [HttpPost("request-reset")]
        public async Task<IActionResult> RequestReset([FromBody] ResetRequest request)
        {
            var resetToken = Guid.NewGuid().ToString();
            await _authRepo.SaveResetToken(request.Email, resetToken);

            // TODO: send resetToken via email/Firebase
            return Ok(new { resetToken });
        }

        // Reset password (Android app posts token + new password)
        [HttpPost("reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var valid = await _authRepo.ValidateResetToken(request.Email, request.Token);
            if (!valid) return BadRequest("Invalid or expired reset token");

            var hashedPassword = PasswordHasher.HashPassword(request.NewPassword);
            var success = await _authRepo.UpdatePassword(request.Email, hashedPassword);

            if (!success) return StatusCode(500, "Failed to update password");

            return Ok("Password reset successful.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var hashedPassword = PasswordHasher.HashPassword(request.Password);

            // Move DB insert into UserRepository instead of using _dbFactory directly
            var userId = await _userRepo.CreateUser(request.Email, hashedPassword, request.FirebaseUid);

            return Ok(new { userId });
        }



    }

    // DTOs
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; }
    }

    public class ResetRequest
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirebaseUid { get; set; }
    }
}
