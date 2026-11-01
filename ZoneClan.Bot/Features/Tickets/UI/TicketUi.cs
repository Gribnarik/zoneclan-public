using NetCord;
using NetCord.Rest;
using ZoneClan.Core.Features.Tickets.Models;

namespace ZoneClan.Bot.Features.Tickets.UI;

internal static class TicketUi
{
    public static IEnumerable<IMessageComponentProperties> PlayerStep()
    {
        yield return new TextDisplayProperties(
            "📝 **Заявка (Шаг 1/4)**\nВыберите игрока из списка ниже.\nЕсли он уже есть в составе, заявка обновит его данные.");
        yield return new UserMenuProperties("ticket_select_user")
        {
            Placeholder = "Выберите игрока...",
            MinValues = 1,
            MaxValues = 1
        };
    }

    public static IEnumerable<IMessageComponentProperties> BuildStep(TicketSession session)
    {
        var filled = session.FilledBuilds.Count == 0
            ? "заполненных сборок пока нет"
            : "уже есть: " + string.Join(", ", session.FilledBuilds.Select(BuildName));
        yield return new TextDisplayProperties(
            $"🧩 **Заявка (Шаг 3/4)**\nВыберите сборку и отметьте её брони. Потом можно вернуться и выбрать вторую.\nСтаты боевой и скоростной вставляются вместе в конце.\nПустое поле сборку не трогает.\n**Сейчас:** {BuildName(session.BuildType)}. {filled}.");
        yield return new ActionRowProperties(
        [
            BuildButton("fat", "Боевая", session),
            BuildButton("speed", "Скоростная", session),
            BuildButton("combined", "Комбинированная", session),
            new ButtonProperties("ticket_build_next", "Дальше", ButtonStyle.Primary)
        ]);
    }

    public static IEnumerable<IMessageComponentProperties> ArmorStep(
        IReadOnlyList<ArmorInfo> armors,
        TicketSession session)
    {
        var selectedIds = session.SelectedArmorIds;
        var selected = armors.Where(armor => selectedIds.Contains(armor.Id)).OrderBy(armor => armor.Name).ToList();
        var selectedText = selected.Count == 0
            ? "_Пока ничего не выбрано._"
            : string.Join(", ", selected.Select(armor => armor.Name));
        var favorite = selected.FirstOrDefault(armor => armor.Id == session.FavoriteArmorId)?.Name ?? "не выбрана";

        yield return new TextDisplayProperties(
            $"🛡️ **Заявка (Шаг 3/3)**\nБроня отдельно от сборок: отметьте её один раз и выберите избранную.\n**Выбрано:** {selectedText}\n**Избранная:** {favorite}");

        foreach (var group in armors.GroupBy(armor => armor.CategoryId).OrderBy(group => group.Key))
        {
            var options = group
                .OrderBy(armor => armor.Name)
                .Select(armor => new StringMenuSelectOptionProperties(Trim(armor.Name, 100), armor.Id.ToString())
                {
                    Default = selectedIds.Contains(armor.Id)
                })
                .ToList();
            if (options.Count == 0)
                continue;

            var categoryName = group.First().CategoryName;
            yield return new StringMenuProperties($"ticket_armor_cat:{group.Key}", options)
            {
                Placeholder = Trim(categoryName, 100),
                MinValues = 0,
                MaxValues = options.Count
            };
        }

        if (selected.Count > 0)
        {
            var options = selected
                .Take(25)
                .Select(armor => new StringMenuSelectOptionProperties(Trim(armor.Name, 100), armor.Id.ToString())
                {
                    Default = armor.Id == session.FavoriteArmorId
                })
                .ToList();
            yield return new StringMenuProperties("ticket_favorite", options)
            {
                Placeholder = "Избранная броня",
                MinValues = 1,
                MaxValues = 1
            };
        }

        yield return new ActionRowProperties(
        [
            new ButtonProperties("ticket_armor_done", "Записать", ButtonStyle.Success)
        ]);
    }

    public static List<string> ChosenInOrder(TicketSession session) =>
        new[] { "fat", "speed", "combined" }.Where(session.ChosenBuilds.Contains).ToList();

    public static IEnumerable<IMessageComponentProperties> ProfileStep(TicketSession session)
    {
        var notice = session.Exists
            ? "Этот игрок уже есть: данные будут обновлены."
            : "Новый игрок: заявка создаст карточку.";
        var chosenText = session.ChosenBuilds.Count == 0
            ? "пока не выбраны"
            : string.Join(", ", ChosenInOrder(session).Select(BuildName));
        yield return new TextDisplayProperties(
            $"⚙️ **Заявка (Шаг 2/3)**\n{notice}\n**Игрок:** {session.TargetPlayerName}\n**Статус:** {StatusName(session.Status)} · кувалда {(session.HasSledgehammer ? "да" : "нет")} · снайпер {(session.PlaysSniper ? "да" : "нет")}\n**Сборки:** {chosenText}. Можно отметить несколько.");

        if (session.PlaysSniper && session.Snipers.Count > 0)
        {
            var options = session.Snipers
                .Take(25)
                .Select(sniper => new StringMenuSelectOptionProperties(Trim(sniper.Name, 100), sniper.Id.ToString())
                {
                    Default = session.SelectedSniperIds.Contains(sniper.Id)
                })
                .ToList();
            yield return new StringMenuProperties("ticket_snipers", options)
            {
                Placeholder = "Винтовки",
                MinValues = 0,
                MaxValues = options.Count
            };
        }

        yield return new StringMenuProperties("ticket_builds",
        [
            new StringMenuSelectOptionProperties("Боевая", "fat") { Default = session.ChosenBuilds.Contains("fat") },
            new StringMenuSelectOptionProperties("Скоростная", "speed") { Default = session.ChosenBuilds.Contains("speed") },
            new StringMenuSelectOptionProperties("Комбинированная", "combined") { Default = session.ChosenBuilds.Contains("combined") }
        ])
        {
            Placeholder = "Какие сборки заполнить",
            MinValues = 1,
            MaxValues = 3
        };

        yield return new ActionRowProperties(
        [
            StatusButton("pending", "В обработке", session),
            StatusButton("in_clan", "В клане", session),
            StatusButton("on_leave", "В отпуске", session),
            StatusButton("excluded", "Исключен", session)
        ]);
        yield return new ActionRowProperties(
        [
            new ButtonProperties("ticket_toggle_sledge", session.HasSledgehammer ? "Кувалда: да" : "Кувалда: нет", ButtonStyle.Secondary),
            new ButtonProperties("ticket_toggle_sniper", session.PlaysSniper ? "Снайпер: да" : "Снайпер: нет", ButtonStyle.Secondary),
            new ButtonProperties("ticket_profile_next", "Дальше", ButtonStyle.Primary)
        ]);
    }

    private static ButtonProperties StatusButton(string status, string label, TicketSession session) =>
        new(StatusId(status), label, session.Status == status ? ButtonStyle.Success : ButtonStyle.Secondary);

    private static ButtonProperties BuildButton(string type, string label, TicketSession session) =>
        new(BuildId(type), label, session.BuildType == type ? ButtonStyle.Success : ButtonStyle.Secondary);

    private static string StatusId(string status) => status switch
    {
        "in_clan" => "ticket_status_clan",
        "on_leave" => "ticket_status_leave",
        "excluded" => "ticket_status_excluded",
        _ => "ticket_status_pending"
    };

    private static string BuildId(string type) => type switch
    {
        "fat" => "ticket_build_fat",
        "speed" => "ticket_build_speed",
        _ => "ticket_build_combined"
    };

    private static string StatusName(string status) => status switch
    {
        "in_clan" => "в клане",
        "on_leave" => "в отпуске",
        "excluded" => "исключен",
        _ => "в обработке"
    };

    public static string BuildName(string type) => type switch
    {
        "fat" => "боевая",
        "speed" => "скоростная",
        _ => "комбинированная"
    };

    public static ModalProperties TicketModal(TicketSession session)
    {
        List<IModalComponentProperties> components =
        [
            new LabelProperties("Игровой никнейм", new TextInputProperties("ingame_name", TextInputStyle.Short)
            {
                Value = session.IngameName,
                Required = true,
                MaxLength = 64
            }),
            new LabelProperties("Заметка дуэлянта", new TextInputProperties("duel_note", TextInputStyle.Paragraph)
            {
                Value = session.DuelNote,
                Required = false
            })
        ];
        foreach (var type in ChosenInOrder(session))
            components.Add(StatInput("stats_" + type, "Статы " + BuildName(type)));

        return new ModalProperties("ticket_modal_submit", "Данные игрока")
        {
            Components = components
        };
    }

    private static LabelProperties StatInput(string id, string label) =>
        new(label, new TextInputProperties(id, TextInputStyle.Paragraph)
        {
            Required = false,
            Placeholder = "Вставьте текст сборки или оставьте пустым"
        });

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
