using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

using ZaoMengXiYou3.Server.Data;
using ZaoMengXiYou3.Server.Models;
using ZaoMengXiYou3.Server.Dtos;

namespace ZaoMengXiYou3.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SaveController : ControllerBase
{
    private readonly GameDbContext _dbContext;

    public SaveController(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    [HttpGet("list")]
    public async Task<IActionResult> GetSaveList()
    {
        // 从 JWT 获取当前玩家 ID
        var playerIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(playerIdValue, out int playerId))
            return Unauthorized();

        // 查询当前玩家的所有云端存档
        var saves = await _dbContext.CloudSaves
            .AsNoTracking()
            .Where(s => s.PlayerAccountId == playerId)
            .OrderBy(s => s.SlotNumber)
            .ToListAsync();

        // 提取存档基本信息
        var result = saves.Select(save =>
        {
            string name = "";
            int gameLevel = 1;
            int rebirth = 0;

            using var document = JsonDocument.Parse(save.DataJson);
            var root = document.RootElement;

            // 读取存档名称
            if (root.TryGetProperty("name", out var nameElement)
                && nameElement.ValueKind == JsonValueKind.String)
            {
                name = nameElement.GetString() ?? "";
            }

            // 读取最高关卡
            if (root.TryGetProperty("gameLevel", out var levelElement)
                && levelElement.TryGetInt32(out int level))
            {
                gameLevel = level;
            }

            // 读取轮回次数
            if (root.TryGetProperty("rebirth", out var rebirthElement)
                && rebirthElement.TryGetInt32(out int rebirthValue))
            {
                rebirth = rebirthValue;
            }

            return new
            {
                save.SlotNumber,
                Name = name,
                GameLevel = gameLevel,
                Rebirth = rebirth,
                save.UpdatedAtUtc
            };
        }).ToList();

        return Ok(new
        {
            saves = result
        });
    }


    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        UploadCloudSaveRequest request)
    {
        // 从 JWT 获取当前玩家 ID
        var playerIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(playerIdValue, out int playerId))
        {
            return Unauthorized();
        }

        // 验证存档 JSON 格式
        try
        {
            using var document = JsonDocument.Parse(request.DataJson);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                message = "存档 JSON 格式错误"
            });
        }

        // 查找玩家对应的存档槽位
        var save = await _dbContext.CloudSaves
            .SingleOrDefaultAsync(s =>
                s.PlayerAccountId == playerId &&
                s.SlotNumber == request.SlotNumber);

        // 不存在则创建，存在则覆盖
        if (save == null)
        {
            save = new CloudSave
            {
                PlayerAccountId = playerId,
                SlotNumber = request.SlotNumber
            };
            _dbContext.CloudSaves.Add(save);
        }

        save.DataJson = request.DataJson;
        save.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "存档上传成功",
            save.SlotNumber,
            save.UpdatedAtUtc
        });
    }
    [HttpGet("download/{slotNumber:int}")]
    public async Task<IActionResult> Download(int slotNumber)
    {
        // 检查槽位编号
        if (slotNumber < 1)
        {
            return BadRequest(new
            {
                message = "无效的存档槽位"
            });
        }

        // 从 JWT 获取当前玩家 ID
        var playerIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(playerIdValue, out int playerId))
        {
            return Unauthorized();
        }

        // 查询当前玩家对应的存档
        var save = await _dbContext.CloudSaves
            .AsNoTracking()
            .SingleOrDefaultAsync(s =>
                s.PlayerAccountId == playerId &&
                s.SlotNumber == slotNumber);

        if (save == null)
        {
            return NotFound(new
            {
                message = "存档不存在"
            });
        }

        return Ok(new
        {
            save.SlotNumber,
            save.DataJson,
            save.UpdatedAtUtc
        });
    }

    [HttpDelete("delete/{slotNumber:int}")]
    public async Task<IActionResult> Delete(int slotNumber)
    {
        // 检查槽位编号
        if (slotNumber < 1)
        {
            return BadRequest(new
            {
                message = "无效的存档槽位"
            });
        }

        // 从 JWT 获取当前玩家 ID
        var playerIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(playerIdValue, out int playerId))
        {
            return Unauthorized();
        }

        // 只能删除自己账号下的存档
        var save = await _dbContext.CloudSaves
            .SingleOrDefaultAsync(s =>
                s.PlayerAccountId == playerId &&
                s.SlotNumber == slotNumber);

        if (save == null)
        {
            return NotFound(new
            {
                message = "存档不存在"
            });
        }

        _dbContext.CloudSaves.Remove(save);
        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = "存档删除成功",
            save.SlotNumber
        });
    }
}