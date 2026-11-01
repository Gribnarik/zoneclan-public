using System.Net.Http.Json;
using ZoneClan.Core.Features.Penalties.Models;
using ZoneClan.Web.Features.Shared;

namespace ZoneClan.Web.Features.Penalties.Data;

public class PenaltiesApi(HttpClient http)
{
    public async Task<PenaltyPageDto> GetAsync(bool includeResolved) =>
        await ApiClient.ReadAsync<PenaltyPageDto>(await http.GetAsync($"api/penalties?resolved={includeResolved}"));

    public async Task IssueAsync(IssuePenaltyRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/penalties", request);
        await ApiClient.EnsureAsync(response);
    }

    public async Task UpdateAsync(int id, UpdatePenaltyRequest request)
    {
        using var response = await http.PutAsJsonAsync($"api/penalties/{id}", request);
        await ApiClient.EnsureAsync(response);
    }

    public async Task ResolveAsync(int id, string method)
    {
        using var response = await http.PostAsJsonAsync($"api/penalties/{id}/resolve", new ResolvePenaltyRequest { Method = method });
        await ApiClient.EnsureAsync(response);
    }

    public async Task CancelAsync(int id)
    {
        using var response = await http.PostAsync($"api/penalties/{id}/cancel", null);
        await ApiClient.EnsureAsync(response);
    }

    public async Task DeleteAsync(int id)
    {
        using var response = await http.DeleteAsync($"api/penalties/{id}");
        await ApiClient.EnsureAsync(response);
    }
}