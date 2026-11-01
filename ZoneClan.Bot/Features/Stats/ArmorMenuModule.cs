using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using ZoneClan.Core.Features.Roster.Models;

namespace ZoneClan.Bot.Features.Stats;

public class ArmorMenuModule : ComponentInteractionModule<StringMenuInteractionContext>
{
    [ComponentInteraction("armor_select")]
    public async Task HandleAsync()
    {
        if (!ArmorCardStorage.Cards.TryGetValue(Context.User.Id, out var card))
        {
            await RespondAsync(DiscordUi.PrivateText("Сессия истекла. Вызовите /статистика заново."));
            return;
        }

        var idx = int.TryParse(Context.SelectedValues.FirstOrDefault(), out var i) ? i : -1;
        var armor = (idx >= 0 && idx < card.Armors.Count) ? card.Armors[idx] : null;
        if (armor is null)
        {
            await RespondAsync(DiscordUi.PrivateText("Броня не найдена."));
            return;
        }

        var embed = new EmbedProperties
        {
            Title = $"🛡 {armor.Name} ({BuildName(armor.BuildType)})",
            Color = new Color(0x5865F2),
            Fields =
            [
                new EmbedFieldProperties { Name = "Здоровье", Value = armor.EffectiveHealth.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Лечение/с", Value = armor.HealPerSec.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Скорость", Value = (armor.MoveSpeed + armor.RunSpeed).ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Вес", Value = armor.CarryWeight.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Пулестойкость", Value = armor.BulletResist.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Разрыв", Value = armor.RuptureProtection.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Стойкость", Value = armor.Resilience.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Выносливость", Value = armor.Endurance.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Реген выносливости", Value = armor.EnduranceRegen.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Живучесть", Value = armor.Vitality.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Лечение (эфф.)", Value = armor.HealEfficiency.ToString("0.##"), Inline = true },
                new EmbedFieldProperties { Name = "Реген здоровья", Value = armor.HealthRegen.ToString("0.##"), Inline = true },
            ]
        };

        await RespondAsync(InteractionCallback.Message(new InteractionMessageProperties
        {
            Flags = MessageFlags.Ephemeral,
            Embeds = [embed]
        }));
    }

    private static string BuildName(string type) => type switch
    {
        "fat" => "боевая",
        "speed" => "скоростная",
        _ => "комбинированная"
    };
}
