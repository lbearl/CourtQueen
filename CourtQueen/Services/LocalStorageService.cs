using System.Text.Json;
using Microsoft.JSInterop;

namespace CourtQueen.Services;

public class LocalStorageService(IJSRuntime jsRuntime) : ILocalStorageService
{
    public async Task SetItemAsync<T>(string key, T item)
    {
        var json = JsonSerializer.Serialize(item);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveItemAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public async Task ClearAll()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}