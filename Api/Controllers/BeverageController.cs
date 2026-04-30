using Api.Models.Database;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BeverageController(BeverageService beverageService) : ControllerBase
{
    private readonly BeverageService _beverageService = beverageService;

    [HttpGet]
    public async Task<List<Beverage>> GetAsync()
    {
        return await _beverageService.GetAsync();
    }

    [HttpGet("{id}")]
    public async Task<Beverage?> GetByIdAsync(Guid id)
    {
        return await _beverageService.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task CreateAsync(Beverage beverage)
    {
        await _beverageService.CreateAsync(beverage);
    }

    [HttpPut("{id}/purchase")]
    public async Task<Beverage> PurchaseAsync(Guid id) 
    {
        return await _beverageService.PurchaseAsync(id);
    }

    [HttpPut("{id}/deactivate")]
    public async Task DeactivateAsync(Guid id) 
    {
        await _beverageService.DeactivateAsync(id);
    }

    [HttpDelete]
    public async Task DeleteAsync(Guid id)
    {
        await _beverageService.DeleteAsync(id);
    }
}