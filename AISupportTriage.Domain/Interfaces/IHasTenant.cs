namespace AISupportTriage.Domain.Interfaces;

public interface IHasTenant
{
    Guid TenantId { get; set; }
}