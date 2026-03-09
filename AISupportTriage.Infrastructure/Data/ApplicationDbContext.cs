using AISupportTriage.Application.Interfaces;
using AISupportTriage.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AISupportTriage.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly ICurrentTenantService? _currentTenant;

    public ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentTenantService? currentTenant = null)
    : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<TicketAuditLog> TicketAuditLogs => Set<TicketAuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Tenant ──────────────────────────────────────────────────────────
        builder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(200).IsRequired();
            e.Property(t => t.Plan).HasMaxLength(50).HasDefaultValue("Free");
        });

        // ── ApplicationUser ──────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
            e.Property(u => u.Department).HasMaxLength(100);
            e.HasOne(u => u.Tenant)
             .WithMany(t => t.Users)
             .HasForeignKey(u => u.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Ticket ───────────────────────────────────────────────────────────
        builder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasMaxLength(5000).IsRequired();
            e.Property(t => t.SuggestedTags).HasMaxLength(500);

            e.HasOne(t => t.Tenant)
             .WithMany(tn => tn.Tickets)
             .HasForeignKey(t => t.TenantId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.AssignedAgent)
             .WithMany(u => u.AssignedTickets)
             .HasForeignKey(t => t.AssignedAgentId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.CreatedBy)
             .WithMany(u => u.CreatedTickets)
             .HasForeignKey(t => t.CreatedById)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);

            // Multi-tenant global query filter
            e.HasQueryFilter(t => !t.IsDeleted);

            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.CreatedAt);
        });

        // ── TicketMessage ────────────────────────────────────────────────────
        builder.Entity<TicketMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).HasMaxLength(10000).IsRequired();

            e.HasOne(m => m.Ticket)
             .WithMany(t => t.Messages)
             .HasForeignKey(m => m.TicketId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
             .WithMany(u => u.Messages)
             .HasForeignKey(m => m.SenderId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TicketAuditLog ───────────────────────────────────────────────────
        builder.Entity<TicketAuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).HasMaxLength(100).IsRequired();
            e.Property(a => a.OldValue).HasMaxLength(500);
            e.Property(a => a.NewValue).HasMaxLength(500);

            e.HasOne(a => a.Ticket)
             .WithMany(t => t.AuditLogs)
             .HasForeignKey(a => a.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}