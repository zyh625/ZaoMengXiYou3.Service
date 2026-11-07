namespace ZaoMengXiYou3.Server.Models;

public class CloudSave
{
    public int Id { get; set; }

    public int PlayerAccountId { get; set; }

    public int SlotNumber { get; set; }

    public string DataJson { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
        = DateTimeOffset.UtcNow;
}