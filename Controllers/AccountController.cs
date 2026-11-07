using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


using ZaoMengXiYou3.Server.Services;
using ZaoMengXiYou3.Server.Data;
using ZaoMengXiYou3.Server.Models;
using ZaoMengXiYou3.Server.Dtos;

namespace ZaoMengXiYou3.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly GameDbContext _dbContext;
    private readonly IPasswordHasher<PlayerAccount> _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AccountController(
    GameDbContext dbContext,
    IPasswordHasher<PlayerAccount> passwordHasher,
    JwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        string username = request.Username.Trim().ToLowerInvariant();

        // 检查用户名是否已经存在
        bool exists = await _dbContext.PlayerAccounts
            .AnyAsync(p => p.Username == username);

        if (exists)
        {
            return Conflict(new
            {
                code = "account_exists",
                message = "账号已存在"
            });
        }

        // 创建账号
        var account = new PlayerAccount
        {
            Username = username,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // 对密码进行安全哈希
        account.PasswordHash = _passwordHasher.HashPassword(
            account,
            request.Password
        );

        _dbContext.PlayerAccounts.Add(account);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqliteException sqlite
                  && sqlite.SqliteErrorCode == 19)
        {
            return Conflict(new
            {
                code = "account_exists",
                message = "账号已存在"
            });
        }

        return StatusCode(201, new
        {
            account.Id,
            account.Username,
            account.CreatedAtUtc
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        string username = request.Username.Trim().ToLowerInvariant();

        // 查找账号
        var account = await _dbContext.PlayerAccounts
            .SingleOrDefaultAsync(p => p.Username == username);

        // 游戏客户端需要区分“账号不存在”和“密码错误”两种提示
        if (account == null)
        {
            return NotFound(new
            {
                code = "account_not_found",
                message = "账号不存在"
            });
        }

        // 验证密码
        var result = _passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                code = "wrong_password",
                message = "密码错误"
            });
        }

        // 如果密码哈希需要升级，则重新保存
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(
                account,
                request.Password
            );

            await _dbContext.SaveChangesAsync();
        }

        // 生成 JWT Token
        string token = _jwtTokenService.CreateToken(account);

        return Ok(new
        {
            account.Id,
            account.Username,
            accessToken = token,
            tokenType = "Bearer"
        });
    }
    // 注销账号：需要已登录，并再次输入密码确认；账号与其全部云端存档一起删除
    [Authorize]
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
    {
        var playerIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(playerIdValue, out int playerId))
        {
            return Unauthorized();
        }

        var account = await _dbContext.PlayerAccounts
            .SingleOrDefaultAsync(p => p.Id == playerId);

        if (account == null)
        {
            return NotFound(new
            {
                code = "account_not_found",
                message = "账号不存在"
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            request.Password
        );

        // 用 403 而不是 401：401 留给令牌失效，客户端据此区分“密码错误”和“登录已过期”
        if (result == PasswordVerificationResult.Failed)
        {
            return StatusCode(403, new
            {
                code = "wrong_password",
                message = "密码错误"
            });
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.CloudSaves
            .Where(s => s.PlayerAccountId == playerId)
            .ExecuteDeleteAsync();

        _dbContext.PlayerAccounts.Remove(account);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "账号已注销"
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentPlayer()
    {
        var playerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        if (!int.TryParse(playerId, out int id) ||
            string.IsNullOrEmpty(username))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id,
            username
        });
    }
}