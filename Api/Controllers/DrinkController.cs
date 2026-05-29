using Api.Models.Database;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BeverageController(DrinkService drinkService) : ControllerBase
{
    private readonly DrinkService _drinkService = drinkService;

    [HttpGet]
    public async Task<List<Drink>> GetAsync()
    {
        return await _drinkService.GetAsync();
    }

    [HttpGet("{id}")]
    public async Task<Drink?> GetByIdAsync(Guid id)
    {
        return await _drinkService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task CreateAsync(Drink drink)
    {
        await _drinkService.CreateAsync(drink);
    }

    [HttpPut("{id}/purchase")]
    public async Task PurchaseAsync(Guid id) 
    {
        await _drinkService.PurchaseAsync(id);
    }

    [HttpPut("{id}/deactivate")]
    public async Task DeactivateAsync(Guid id) 
    {
        await _drinkService.DeactivateAsync(id);
    }

    [HttpDelete]
    public async Task DeleteAsync(Guid id)
    {
        await _drinkService.DeleteAsync(id);
    }
}