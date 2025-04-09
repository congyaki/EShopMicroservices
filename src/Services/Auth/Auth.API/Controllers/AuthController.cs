using Auth.API.DTOs;
using Auth.API.Interfaces.Services;
using Auth.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenRevocationService _tokenRevocationService;

    public AuthController(IUserService userService, ITokenService tokenService, ILogger<AuthController> logger, ITokenRevocationService tokenRevocationService)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
        _tokenRevocationService = tokenRevocationService;
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userService.ValidateUserAsync(request.Username, request.Password);

            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                return Unauthorized(new { message = "Username or password is incorrect" });
            }

            var tokenResponse = await _tokenService.GenerateTokensAsync(user);
            _logger.LogInformation("User {Username} successfully logged in", request.Username);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return StatusCode(500, new { message = "An error occurred during authentication" });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var tokenResponse = await _tokenService.RefreshTokenAsync(request.RefreshToken, request.AccessToken);
            return Ok(tokenResponse);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid token during refresh attempt");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred while refreshing the token" });
        }
    }

    [Authorize]
    [HttpGet("validate-token")]
    public ActionResult ValidateToken()
    {
        // If we reach here, the token is valid
        var username = User.Identity.Name;
        _logger.LogInformation("Token validated for user: {Username}", username);
        return Ok(new { isValid = true, username });
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _userService.RegisterUserAsync(request);

            if (!result)
            {
                return BadRequest(new { message = "Username or email already exists" });
            }

            _logger.LogInformation("User {Username} successfully registered", request.Username);
            return StatusCode(201, new { message = "User registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Username}", request.Username);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var jti = User.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
        {
            return BadRequest(new { message = "Invalid token" });
        }

        // Revoke the token
        await _tokenRevocationService.RevokeTokenAsync(jti);

        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("admin-data")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult GetAdminData()
    {
        return Ok(new { message = "This is admin only data" });
    }
}