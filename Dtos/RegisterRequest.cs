using System.ComponentModel.DataAnnotations;

namespace ZaoMengXiYou3.Server.Dtos;

public class RegisterRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}