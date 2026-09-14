using Microsoft.EntityFrameworkCore;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Infrastructure;

public class MiniExchangeDbContext : DbContext
{
    public MiniExchangeDbContext(
        DbContextOptions<MiniExchangeDbContext> options)
        : base(options)
    {
    }

    public DbSet<InstrumentEntity> Instruments => Set<InstrumentEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<TradeEntity> Trades => Set<TradeEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstrumentEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Ticker)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Ticker)
                .IsUnique();
        });

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Price)
                .HasPrecision(18, 4);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            entity.Property(x => x.RemainingQuantity)
                .HasPrecision(18, 4);

            entity.HasIndex(x => new
            {
                x.InstrumentId,
                x.Side,
                x.Price,
                x.CreatedAt,
                x.Id
            });
        });

        modelBuilder.Entity<TradeEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Price)
                .HasPrecision(18, 4);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            entity.HasIndex(x => new
            {
                x.InstrumentId,
                x.ExecutedAt
            });
        });

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Key)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Payload)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.PublishedAt,
                x.CreatedAt
            });
        });
    }
}