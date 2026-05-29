namespace Api.Models.Interfaces;

public interface IAlcoholicDrink : IDrink 
{
    decimal Abv { get; set; }
}