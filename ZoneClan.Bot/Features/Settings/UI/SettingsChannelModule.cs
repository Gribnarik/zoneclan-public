using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using System.Net.Http.Json;

namespace ZoneClan.Bot.Features.Settings.UI;

public class SettingsChannelModule(IHttpClientFactory httpClientFactory)
    : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction("settings_gathering_channel")]
    public async Task HandleGatheringAsync() => await SaveAsync("gathering-channel");

    [ComponentInteraction("settings_penalty_channel")]
    public async Task HandlePenaltyAsync() => await SaveAsync("penalty-channel");

    [ComponentInteraction("settings_absent_channel")]
    public async Task HandleAbsentAsync() => await SaveAsync("absent-channel");

    private async Task SaveAsync(string endpoint)
    {
        var selected = Context.SelectedValues.FirstOrDefault();
        if (selected is null)
            return;

        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        using var client = httpClientFactory.CreateClient("api");
        var response = await client.PutAsJsonAsync(
            $"api/guilds/{Context.Guild!.Id}/settings/{endpoint}",
            selected.Id);
        if (response.IsSuccessStatusCode)
            await ModifyResponseAsync(options =>
            {
                options.Flags = MessageFlags.Ephemeral;
                options.Content = "✅ Канал сохранён.";
            });
        else
            await ModifyResponseAsync(options =>
            {
                options.Flags = MessageFlags.Ephemeral;
                options.Content = "❌ Не удалось сохранить канал.";
            });
        return;

    }
}
