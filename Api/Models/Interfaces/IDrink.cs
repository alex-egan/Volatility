using Api.Models.Enums;

namespace Api.Models.Interfaces;

public interface IDrink : IDbEntity, IAuditableEntity 
{
    BeverageType Type { get; set; }
    List<BeverageContainerType> AvailableContainers { get; set; }
    string Name { get; set; }
    string Code { get; set; }
    decimal Price { get; set; }
    decimal MinPrice { get; set; }
    decimal MaxPrice { get; set; }
}