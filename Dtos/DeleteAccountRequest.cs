using System.ComponentModel.DataAnnotations;

namespace ZaoMengXiYou3.Server.Dtos;

public class DeleteAccountRequest
{
    [Required]
    public string Password { get; set; } = string.Empty;
}
