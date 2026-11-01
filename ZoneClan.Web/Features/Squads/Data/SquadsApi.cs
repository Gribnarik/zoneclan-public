using System.Net.Http.Json;
using ZoneClan.Core.Features.Squads.Models;
using ZoneClan.Web.Features.Shared;

namespace ZoneClan.Web.Features.Squads.Data;

public class SquadsApi(HttpClient http)
{
    public async Task<List<SquadDto>> ListAsync() =>
        await ApiClient.ReadAsync<List<SquadDto>>(await http.GetAsync("api/squads"));

    public async Task RenameAsync(int squadId, string name, string description = "")
    {
        using var response = await http.PatchAsJsonAsync($"api/squads/{squadId}", new UpdateSquadRequest { Name = name, Description = description });
        await ApiClient.EnsureAsync(response);
    }

    public async Task AddMemberAsync(int squadId, ulong playerId, string role)
    {
        using var response = await http.PostAsJsonAsync(
            $"api/squads/{squadId}/members",
            new AddSquadMemberRequest { PlayerId = playerId, Role = role });
        await ApiClient.EnsureAsync(response);
    }

    public async Task UpdateRoleAsync(int memberId, string role)
    {
        using var response = await http.PatchAsJsonAsync($"api/squads/members/{memberId}", new UpdateSquadMemberRequest { Role = role });
        await ApiClient.EnsureAsync(response);
    }

    public async Task UpdateExtraRoleAsync(int memberId, string extraRole)
    {
        using var response = await http.PatchAsJsonAsync($"api/squads/members/{memberId}/extra", extraRole);
        await ApiClient.EnsureAsync(response);
    }

    public async Task<List<SquadBuildDto>> GetBuildsAsync(int squadId) =>
        await ApiClient.ReadAsync<List<SquadBuildDto>>(await http.GetAsync($"api/squads/{squadId}/builds"));

    public async Task AddBuildAsync(int squadId, string description, Stream? file, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(description ?? ""), "description");
        if (file is not null)
            content.Add(new StreamContent(file), "file", fileName);
        using var response = await http.PostAsync($"api/squads/{squadId}/builds", content);
        await ApiClient.EnsureAsync(response);
    }

    public async Task DeleteBuildAsync(int buildId)
    {
        using var response = await http.DeleteAsync($"api/squads/builds/{buildId}");
        await ApiClient.EnsureAsync(response);
    }

    public async Task RemoveAsync(int memberId)
    {
        using var response = await http.DeleteAsync($"api/squads/members/{memberId}");
        await ApiClient.EnsureAsync(response);
    }
}
