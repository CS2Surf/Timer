using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using SurfTimer.Shared.DTO;
using System.Text.RegularExpressions;

namespace SurfTimer;

public partial class SurfTimer
{
    // All map-related commands here
    [ConsoleCommand("css_tier", "Display the current map tier.")]
    [ConsoleCommand("css_mapinfo", "Display the current map tier.")]
    [ConsoleCommand("css_mi", "Display the current map tier.")]
    [ConsoleCommand("css_difficulty", "Display the current map tier.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void MapTier(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        char rankedColor = CurrentMap.Ranked ? ChatColors.Green : ChatColors.Red;
        string rankedStatus = CurrentMap.Ranked ? "Yes" : "No";

        string msg = $"{Config.PluginPrefix} " + LocalizationService.LocalizerNonNull["map_info",
            CurrentMap.Name,
            $"{Extensions.GetTierColor(CurrentMap.Tier)}{CurrentMap.Tier}",
            CurrentMap.Author,
            $"{rankedColor}{rankedStatus}",
            DateTimeOffset.FromUnixTimeSeconds(CurrentMap.DateAdded).DateTime.ToString("dd.MM.yyyy HH:mm")
        ];

        if (CurrentMap.Stages > 1)
        {
            msg += LocalizationService.LocalizerNonNull["map_info_stages", CurrentMap.Stages];
        }
        else
        {
            msg += LocalizationService.LocalizerNonNull["map_info_linear", CurrentMap.TotalCheckpoints];
        }

        if (CurrentMap.Bonuses > 0)
        {
            msg += LocalizationService.LocalizerNonNull["map_info_bonuses", CurrentMap.Bonuses];
        }

        player.PrintToChat(msg);
    }

    [ConsoleCommand("css_amt", "Set the Tier of the map.")]
    [ConsoleCommand("css_addmaptier", "Set the Tier of the map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<Tier Number> [1-8]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void AddMapTier(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        short tier;
        try
        {
            tier = short.Parse(command.ArgByIndex(1));
        }
        catch (System.Exception)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!amt <tier> [1-8]"]}"
            );
            return;
        }

        if (tier > 8)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!amt <tier> [1-8]"]}"
            );
            return;
        }

        var mapInfo = new MapDto
        {
            Name = CurrentMap.Name!,
            Author = CurrentMap.Author!,
            Tier = tier,
            Stages = CurrentMap.Stages,
            Bonuses = CurrentMap.Bonuses,
            Ranked = CurrentMap.Ranked,
            LastPlayed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        CurrentMap.Tier = tier;

        Task.Run(async () =>
        {
            await _dataService!.UpdateMapInfoAsync(mapInfo, CurrentMap.ID);
        });

        string msg = $"{Config.PluginPrefix} {ChatColors.Yellow}{CurrentMap.Name}{ChatColors.Default} - Set Tier to {Extensions.GetTierColor(CurrentMap.Tier)}{CurrentMap.Tier}{ChatColors.Default}.";

        player.PrintToChat(msg);
    }

    [ConsoleCommand("css_amn", "Set the Name of the map author.")]
    [ConsoleCommand("css_addmappername", "Set the Name of the map author.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(minArgs: 1, usage: "<Author Name>", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void AddMapAuthor(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        string author = command.ArgString.Trim();

        // Validate: letters, numbers, intervals, dashes and up to 50 symbols
        if (string.IsNullOrWhiteSpace(author) || author.Length > 50 || !Regex.IsMatch(author, @"^[\w\s\-\.]+$"))
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!amn <author name>"]}"
            );
            return;
        }

        var mapInfo = new MapDto
        {
            Name = CurrentMap.Name!,
            Author = author,
            Tier = CurrentMap.Tier,
            Stages = CurrentMap.Stages,
            Bonuses = CurrentMap.Bonuses,
            Ranked = CurrentMap.Ranked,
            LastPlayed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        CurrentMap.Author = author;

        Task.Run(async () =>
        {
            await _dataService!.UpdateMapInfoAsync(mapInfo, CurrentMap.ID);
        });

        string msg = $"{Config.PluginPrefix} {ChatColors.Yellow}{CurrentMap.Name}{ChatColors.Default} - Set Author to {ChatColors.Green}{CurrentMap.Author}{ChatColors.Default}.";

        player.PrintToChat(msg);
    }

    [ConsoleCommand("css_amr", "Set the Ranked option of the map.")]
    [ConsoleCommand("css_addmapranked", "Set the Ranked option of the map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void AddMapRanked(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (CurrentMap.Ranked)
            CurrentMap.Ranked = false;
        else
            CurrentMap.Ranked = true;

        var mapInfo = new MapDto
        {
            Name = CurrentMap.Name!,
            Author = CurrentMap.Author!,
            Tier = CurrentMap.Tier,
            Stages = CurrentMap.Stages,
            Bonuses = CurrentMap.Bonuses,
            Ranked = CurrentMap.Ranked,
            LastPlayed = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        Task.Run(async () =>
        {
            await _dataService!.UpdateMapInfoAsync(mapInfo, CurrentMap.ID);
        });

        string msg = $"{Config.PluginPrefix} {ChatColors.Yellow}{CurrentMap.Name}{ChatColors.Default} - Set Ranked to {(CurrentMap.Ranked ? ChatColors.Green : ChatColors.Red)}{CurrentMap.Ranked}{ChatColors.Default}.";

        player.PrintToChat(msg);
    }

    [ConsoleCommand("css_triggers", "List all valid zone triggers in the map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Triggers(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        IEnumerable<CBaseTrigger> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");
        player.PrintToChat($"Count of triggers: {triggers.Count()}");
        foreach (CBaseTrigger trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                player.PrintToChat($"Trigger -> Origin: {trigger.AbsOrigin}, Radius: {trigger.Collision.BoundingRadius}, Name: {trigger.Entity!.Name}");
            }
        }

        player.PrintToChat($"Hooked Trigger -> Start -> {CurrentMap.StartZone} -> Angles {CurrentMap.StartZoneAngles}");
        player.PrintToChat($"Hooked Trigger -> End -> {CurrentMap.EndZone}");
        int i = 1;
        foreach (Vector_t stage in CurrentMap.StageStartZone)
        {
            if (stage.X == 0 && stage.Y == 0 && stage.Z == 0)
                continue;
            else
            {
                player.PrintToChat($"Hooked Trigger -> Stage {i} -> {stage} -> Angles {CurrentMap.StageStartZoneAngles[i]}");
                i++;
            }
        }

        i = 1;
        foreach (Vector_t bonus in CurrentMap.BonusStartZone)
        {
            if (bonus.X == 0 && bonus.Y == 0 && bonus.Z == 0)
                continue;
            else
            {
                player.PrintToChat($"Hooked Trigger -> Bonus {i} -> {bonus} -> Angles {CurrentMap.BonusStartZoneAngles[i]}");
                i++;
            }
        }

        return;
    }
}