namespace Api.Models.Interfaces;

public interface IAlcoholicBeverage : IBeverage 
{
    decimal Abv { get; set; }
}