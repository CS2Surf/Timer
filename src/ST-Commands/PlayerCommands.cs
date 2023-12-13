using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public partial class SurfTimer
{
    [ConsoleCommand("css_r", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerReset(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        playerList[player.UserId ?? 0].Timer.Reset();
        if (CurrentMap.StartZone != new Vector(0,0,0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0,0,0), new Vector(0,0,0)));
        return;
    }

    [ConsoleCommand("css_rs", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerResetStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        Player SurfPlayer = playerList[player.UserId ?? 0];
        if (SurfPlayer.Timer.Stage != 0 && CurrentMap.StageStartZone[SurfPlayer.Timer.Stage] != new Vector(0,0,0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StageStartZone[SurfPlayer.Timer.Stage], CurrentMap.StageStartZoneAngles[SurfPlayer.Timer.Stage], new Vector(0,0,0)));
        else // Reset back to map start
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0,0,0), new Vector(0,0,0)));
        return;
    }

    [ConsoleCommand("css_triggers", "List all valid zone triggers in the map.")]
    [RequiresPermissions("@css/root")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void Triggers(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        IEnumerable<CBaseEntity> triggers = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("trigger_multiple");
        player.PrintToChat($"Count of triggers: {triggers.Count()}");
        foreach (CBaseEntity trigger in triggers)
        {
            if (trigger.Entity!.Name != null)
            {
                player.PrintToChat($"Trigger -> Origin: {trigger.AbsOrigin}, Name: {trigger.Entity!.Name}");
            }
        }

        player.PrintToChat($"Hooked Trigger -> Start -> {CurrentMap.StartZone}");
        player.PrintToChat($"Hooked Trigger -> End -> {CurrentMap.EndZone}");
        int stageCounter = 1;
        foreach (Vector stage in CurrentMap.StageStartZone)
        {
            if (stage.X == 0 && stage.Y == 0 && stage.Z == 0)
                continue;
            else
            {
                player.PrintToChat($"Hooked Trigger -> Stage {stageCounter} -> {stage}");
                stageCounter++;
            }
        }
        return;
    }
}