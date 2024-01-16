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
        if (CurrentMap.StartZone != new Vector(0, 0, 0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        return;
    }

    [ConsoleCommand("css_rs", "Reset back to the start of the stage or bonus you're in.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerResetStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        // To-do: players[userid].Timer.Reset() -> teleport player
        Player SurfPlayer = playerList[player.UserId ?? 0];
        if (SurfPlayer.Timer.Stage != 0 && CurrentMap.StageStartZone[SurfPlayer.Timer.Stage] != new Vector(0, 0, 0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StageStartZone[SurfPlayer.Timer.Stage], CurrentMap.StageStartZoneAngles[SurfPlayer.Timer.Stage], new Vector(0, 0, 0)));
        else // Reset back to map start
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        return;
    }

    [ConsoleCommand("css_s", "Teleport to a stage")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int stage = Int32.Parse(command.ArgByIndex(1)) - 1;
        if (stage > CurrentMap.Stages - 1)
            stage = CurrentMap.Stages - 1;

        // Must be 1 argument
        if (command.ArgCount < 2 || stage < 0)
        {
            #if DEBUG
            player.PrintToChat($"CS2 Surf DEBUG >> css_s >> Arg#: {command.ArgCount} >> Args: {Int32.Parse(command.ArgByIndex(1))}");
            #endif

            player.PrintToChat($"{PluginPrefix} {ChatColors.Red}Invalid arguments. Usage: {ChatColors.Green}!s <stage>");
            return;
        }
        else if (CurrentMap.Stages <= 0)
        {
            player.PrintToChat($"{PluginPrefix} {ChatColors.Red}This map has no stages.");
            return;
        }

        if (CurrentMap.StageStartZone[stage] != new Vector(0, 0, 0))
        {
            if (stage == 0)
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, CurrentMap.StartZoneAngles, new Vector(0, 0, 0)));
            else
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StageStartZone[stage], CurrentMap.StageStartZoneAngles[stage], new Vector(0, 0, 0)));

            playerList[player.UserId ?? 0].Timer.Reset();
            playerList[player.UserId ?? 0].Timer.IsStageMode = true;

            // To-do: If you run this while you're in the start zone, endtouch for the start zone runs after you've teleported
            //        causing the timer to start. This needs to be fixed.
        }

        else
            player.PrintToChat($"{PluginPrefix} {ChatColors.Red}Invalid stage provided. Usage: {ChatColors.Green}!s <stage>");
    }

    // Test command
    [ConsoleCommand("css_spec", "Moves a player automaticlly into spectator mode")]
    public void MovePlayerToSpectator(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator)
            return;

        player.ChangeTeam(CsTeam.Spectator);
    }

    [ConsoleCommand("css_replaybotpause", "Pause the replay bot playback")]
    [ConsoleCommand("css_rbpause", "Pause the replay bot playback")]
    public void PauseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null
            || player.Team != CsTeam.Spectator
            || CurrentMap.ReplayBot.Controller == null
            || !CurrentMap.ReplayBot.IsPlaying
            || CurrentMap.ReplayBot.Controller.Pawn.SerialNum != player.ObserverPawn.Value!.ObserverServices!.ObserverTarget.SerialNum)
            return;

        CurrentMap.ReplayBot.Pause();
    }

    [ConsoleCommand("css_replaybotflip", "Flips the replay bot between Forward/Backward playback")]
    [ConsoleCommand("css_rbflip", "Flips the replay bot between Forward/Backward playback")]
    public void ReverseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null
            || player.Team != CsTeam.Spectator
            || CurrentMap.ReplayBot.Controller == null
            || !CurrentMap.ReplayBot.IsPlaying
            || CurrentMap.ReplayBot.Controller.Pawn.SerialNum != player.ObserverPawn.Value!.ObserverServices!.ObserverTarget.SerialNum)
            return;

        CurrentMap.ReplayBot.FrameTickIncrement *= -1;
    }
}