using Microsoft.EntityFrameworkCore;

namespace MiniExchange.KafkaConsumer.Data;

public sealed class ConsumerDbContext
    : DbContext
{
    public ConsumerDbContext(
        DbContextOptions<ConsumerDbContext> options)
        : base(options)
    {
    }

    public DbSet<InboxMessageEntity> InboxMessages =>
        Set<InboxMessageEntity>();

    public DbSet<OrderViewEntity> OrderViews =>
        Set<OrderViewEntity>();

    public DbSet<TradeViewEntity> TradeViews =>
        Set<TradeViewEntity>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessageEntity>(entity =>
        {
            entity.HasKey(x => x.EventId);

            entity.Property(x => x.EventType)
                .HasMaxLength(200)
                .IsRequired();
        });

        modelBuilder.Entity<OrderViewEntity>(entity =>
        {
            entity.HasKey(x => x.OrderId);

            entity.Property(x => x.Side)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Price)
                .HasPrecision(18, 4);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 4);

            entity.Property(x => x.RemainingQuantity)
                .HasPrecision(18, 4);
        });

        modelBuilder.Entity<TradeViewEntity>(entity =>
        {
            entity.HasKey(x => x.TradeId);

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
    }
}