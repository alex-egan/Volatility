using Api.Models.Database;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BeverageController(BeverageService beverageService) : ControllerBase
{
    private readonly BeverageService _beverageService = beverageService;

    [HttpGet(Name = "Get")]
    public async Task<List<Beverage>> GetAsync()
    {
        return await _beverageService.GetAsync();
    }

    [HttpPost(Name = "Create")]
    public async Task CreateAsync(Beverage beverage)
    {
        await _beverageService.CreateAsync(beverage);
    }

    [HttpPut(Name = "Purchase")]
    public async Task<Beverage> PurchaseAsync(Guid id) 
    {
        return await _beverageService.PurchaseAsync(id);
    }
}