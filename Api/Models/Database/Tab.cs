using Api.Models.Interfaces;

namespace Api.Models.Database;

public class Tab : ITab
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }

    List<Customer> Customers { get; set; }  = null!;
}