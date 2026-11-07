using System.ComponentModel.DataAnnotations;

namespace ZaoMengXiYou3.Server.Dtos;

public class UploadCloudSaveRequest
{
    [Range(1, int.MaxValue)]
    public int SlotNumber { get; set; }

    [Required]
    [StringLength(1_000_000, MinimumLength = 2)]
    public string DataJson { get; set; } = string.Empty;
}