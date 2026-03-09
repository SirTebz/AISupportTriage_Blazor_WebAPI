using AISupportTriage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISupportTriage.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketMessage> TicketMessages { get; }
    DbSet<TicketAuditLog> TicketAuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}