using Microsoft.EntityFrameworkCore;
using ZaoMengXiYou3.Server.Models;

namespace ZaoMengXiYou3.Server.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options)
        : base(options)
    {
    }

    public DbSet<PlayerAccount> PlayerAccounts { get; set; } = null!;
    public DbSet<CloudSave> CloudSaves { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerAccount>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.HasIndex(p => p.Username).IsUnique();

            entity.Property(p => p.Username)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(p => p.PasswordHash)
                .IsRequired();
        });
        modelBuilder.Entity<CloudSave>(entity =>
        {
            entity.HasKey(s => s.Id);

            // 同一个玩家的同一个槽位只能有一份存档
            entity.HasIndex(s => new
            {
                s.PlayerAccountId,
                s.SlotNumber
            }).IsUnique();

            entity.Property(s => s.DataJson)
                .IsRequired();

            // 存档关联玩家，玩家删除时级联删除存档
            entity.HasOne<PlayerAccount>()
                .WithMany()
                .HasForeignKey(s => s.PlayerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}