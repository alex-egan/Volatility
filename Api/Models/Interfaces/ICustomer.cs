namespace Api.Models.Interfaces;

public interface ICustomer : IDbEntity, IAuditableEntity 
{
    string Name { get; set; }
    string Email { get; set; }
    string PhoneNumber { get; set; }
    DateTime LastSeen { get; set; }
}