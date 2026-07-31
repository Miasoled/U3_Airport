using Microsoft.EntityFrameworkCore;
using U3_Examen_Airport.Models.Application;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace U3_Examen_Airport.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<FlightChangeRequest> FlightChangeRequests =>
        Set<FlightChangeRequest>();

    public DbSet<FlightChangeHistory> FlightChangeHistories =>
        Set<FlightChangeHistory>();

    public DbSet<Order> Orders =>
        Set<Order>();

    public DbSet<OrderDetail> OrderDetails =>
        Set<OrderDetail>();

    public DbSet<Payment> Payments =>
        Set<Payment>();

    public DbSet<TransactionHistory> TransactionHistories =>
        Set<TransactionHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.ExternalTransactionId)
            .IsUnique();

        modelBuilder.Entity<TransactionHistory>()
            .HasIndex(t => t.ExternalTransactionId)
            .IsUnique();

        modelBuilder.Entity<FlightChangeRequest>()
            .HasMany(s => s.Histories)
            .WithOne(h => h.FlightChangeRequest)
            .HasForeignKey(h => h.FlightChangeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FlightChangeRequest>()
            .HasMany(s => s.Orders)
            .WithOne(o => o.FlightChangeRequest)
            .HasForeignKey(o => o.FlightChangeRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.OrderDetails)
            .WithOne(d => d.Order)
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Payment)
            .HasForeignKey(t => t.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}