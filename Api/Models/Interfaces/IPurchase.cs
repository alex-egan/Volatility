namespace Api.Models.Interfaces;

public interface IPurchase : IDbEntity, IAuditableEntity 
{
    Guid CustomerId { get; set; }
}