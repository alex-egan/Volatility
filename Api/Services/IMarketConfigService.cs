namespace Api.Services;

public interface IMarketConfigService
{
    decimal Get(string category, string key);
    Task ReloadAsync();
}