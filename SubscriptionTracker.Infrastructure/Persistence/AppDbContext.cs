using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Entities;

namespace SubscriptionTracker.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("Categories");
            builder.HasKey(static category => category.Id);
            builder.Property(static category => category.Name).HasMaxLength(120).IsRequired();
            builder.Property(static category => category.ColorHex).HasMaxLength(16);
            builder.Property(static category => category.Icon).HasMaxLength(40);
        });

        modelBuilder.Entity<Subscription>(builder =>
        {
            builder.ToTable("Subscriptions");
            builder.HasKey(static subscription => subscription.Id);
            builder.Property(static subscription => subscription.Name).HasMaxLength(180).IsRequired();
            builder.Property(static subscription => subscription.Description).HasMaxLength(1000);
            builder.Property(static subscription => subscription.Currency).HasMaxLength(8).IsRequired();
            builder.Property(static subscription => subscription.Amount).HasPrecision(18, 2);
            builder.HasOne(static subscription => subscription.Category)
                .WithMany(static category => category.Subscriptions)
                .HasForeignKey(static subscription => subscription.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentHistory>(builder =>
        {
            builder.ToTable("PaymentHistories");
            builder.HasKey(static payment => payment.Id);
            builder.Property(static payment => payment.Currency).HasMaxLength(8).IsRequired();
            builder.Property(static payment => payment.Amount).HasPrecision(18, 2);
            builder.Property(static payment => payment.Note).HasMaxLength(500);
            builder.HasOne(static payment => payment.Subscription)
                .WithMany(static subscription => subscription.Payments)
                .HasForeignKey(static payment => payment.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
