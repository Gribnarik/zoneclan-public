using System.Globalization;
using System.Text.RegularExpressions;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Core.Features.Tickets.BusinessLogic;

public static class StatsParser
{
    public static PlayerStatsDto Parse(string text)
    {
        var stats = new PlayerStatsDto();
        text = text.Replace(',', '.');

        stats.EffectiveHealth = ExtractNumber(text, @"Приведённое здоровье");
        stats.HealPerSec = ExtractNumber(text, @"Лечение в секунду");
        stats.Bleeding = ExtractNumber(text, @"Кровотечение");
        stats.Vitality = ExtractNumber(text, @"Живучесть");
        stats.HealEfficiency = ExtractNumber(text, @"Эффективность лечения");
        stats.HealthRegen = ExtractNumber(text, @"Регенерация здоровья");

        stats.MoveSpeed = ExtractNumber(text, @"Скорость передвижения");
        stats.RunSpeed = ExtractNumber(text, @"Скорость бега");
        stats.Endurance = ExtractNumber(text, @"Выносливость");
        stats.EnduranceRegen = ExtractNumber(text, @"Восстановление выносливости");
        stats.CarryWeight = ExtractNumber(text, @"Переносимый вес");

        stats.BulletResist = ExtractNumber(text, @"Пулестойкость");
        stats.Resilience = ExtractNumber(text, @"Стойкость");

        return stats;
    }

    private static decimal ExtractNumber(string text, string label)
    {
        string pattern = $@"(?i){label}\s*(-?\d+(?:\.\d+)?)";
        var match = Regex.Match(text, pattern);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            return result;
        return 0;
    }
}
