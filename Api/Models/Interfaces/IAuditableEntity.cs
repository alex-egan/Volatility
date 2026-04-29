namespace Api.Models.Interfaces;

public interface IAuditableEntity 
{
    string CreatedBy { get; set; }
    DateTime CreatedOn { get; set; }
    string UpdatedBy { get; set; }
    DateTime UpdatedOn { get; set; }
}