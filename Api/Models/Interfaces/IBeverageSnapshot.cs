namespace Api.Models.Interfaces;

public interface IBeverageSnapshot : IDbEntity, IAuditableEntity 
{
    string Name { get; set; }
    string Code { get; set; }
    decimal Price { get; set; }
}