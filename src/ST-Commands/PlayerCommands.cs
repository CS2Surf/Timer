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
    [ConsoleCommand("css_stage", "Teleport to a stage")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int stage = Int32.Parse(command.ArgByIndex(1)) - 1;
        if (stage > CurrentMap.Stages - 1 && CurrentMap.Stages > 0)
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

    [ConsoleCommand("css_spec", "Moves a player automaticlly into spectator mode")]
    public void MovePlayerToSpectator(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator)
            return;

        player.ChangeTeam(CsTeam.Spectator);
    }

    /*
    #########################
        Reaplay Commands
    #########################
    */
    [ConsoleCommand("css_replaybotpause", "Pause the replay bot playback")]
    [ConsoleCommand("css_rbpause", "Pause the replay bot playback")]
    public void PauseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || player.Team != CsTeam.Spectator)
            return;

        foreach(ReplayPlayer rb in CurrentMap.ReplayBots)
        {
            if(!rb.IsPlayable || !rb.IsPlaying || !playerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;
            
            rb.Pause();
        }
    }

    [ConsoleCommand("css_replaybotflip", "Flips the replay bot between Forward/Backward playback")]
    [ConsoleCommand("css_rbflip", "Flips the replay bot between Forward/Backward playback")]
    public void ReverseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || player.Team != CsTeam.Spectator)
            return;

        foreach(ReplayPlayer rb in CurrentMap.ReplayBots)
        {
            if(!rb.IsPlayable || !rb.IsPlaying || !playerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;
            
            rb.FrameTickIncrement *= -1;
        }
    }

    [ConsoleCommand("css_pbreplay", "Allows for replay of player's PB")]
    public void PbReplay(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null)
            return;

        int maptime_id = playerList[player!.UserId ?? 0].Stats.PB[playerList[player.UserId ?? 0].Timer.Style].ID;
        if (command.ArgCount > 1)
        {
            try
            {
                maptime_id = int.Parse(command.ArgByIndex(1));
            }
            catch {}
        }

        if(maptime_id == -1 || !CurrentMap.ConnectedMapTimes.Contains(maptime_id))
        {
            player.PrintToChat($"{PluginPrefix} {ChatColors.Red}No time was found");
            return;
        }
        
        for(int i = 0; i < CurrentMap.ReplayBots.Count; i++)
        {
            if(CurrentMap.ReplayBots[i].Stat_MapTimeID == maptime_id)
            {
                player.PrintToChat($"{PluginPrefix} {ChatColors.Red}A bot of this run already playing");
                return;
            }
        }

        CurrentMap.ReplayBots = CurrentMap.ReplayBots.Prepend(new ReplayPlayer() {
            Stat_MapTimeID = maptime_id,
            Stat_Prefix = "PB"
        }).ToList();

        Server.NextFrame(() => {
            Server.ExecuteCommand($"bot_quota {CurrentMap.ReplayBots.Count}");
        });
    }

        /*
    ########################
        Saveloc Commands
    ########################
    */
    [ConsoleCommand("css_saveloc", "Save current player location to be practiced")]
    public void SavePlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];
        if (!p.Timer.IsRunning)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}Cannot save location while not in run");
            return;
        }

        var player_pos = p.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = p.Controller.PlayerPawn.Value!.EyeAngles;
        var player_velocity = p.Controller.PlayerPawn.Value!.AbsVelocity;

        p.SavedLocations.Add(new SavelocFrame {
            Pos = new Vector(player_pos.X, player_pos.Y, player_pos.Z),
            Ang = new QAngle(player_angle.X, player_angle.Y, player_angle.Z),
            Vel = new Vector(player_velocity.X, player_velocity.Y, player_velocity.Z),
            Tick = p.Timer.Ticks
        });
        p.CurrentSavedLocation = p.SavedLocations.Count-1;

        p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Green}Saved location! {ChatColors.Default} use !tele {p.SavedLocations.Count-1} to teleport to this location");
    }

    [ConsoleCommand("css_tele", "Teleport player to current saved location")]
    public void TeleportPlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if(p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if(!p.Timer.IsRunning)
            p.Timer.Start();

        if (!p.Timer.IsPracticeMode)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}Timer now on practice");
            p.Timer.IsPracticeMode = true;
        }

        if(command.ArgCount > 1)
            try
            {
                int tele_n = int.Parse(command.ArgByIndex(1));
                if (tele_n < p.SavedLocations.Count)
                    p.CurrentSavedLocation = tele_n;
            }
            catch { }
        SavelocFrame location = p.SavedLocations[p.CurrentSavedLocation];
        Server.NextFrame(() => {
            p.Controller.PlayerPawn.Value!.Teleport(location.Pos, location.Ang, location.Vel);
            p.Timer.Ticks = location.Tick;
        });

        p.Controller.PrintToChat($"{PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    [ConsoleCommand("css_teleprev", "Teleport player to previous saved location")]
    public void TeleportPlayerLocationPrev(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if(p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if(p.CurrentSavedLocation == 0)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}Already at first location");
        }
        else
        {
            p.CurrentSavedLocation--;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    [ConsoleCommand("css_telenext", "Teleport player to next saved location")]
    public void TeleportPlayerLocationNext(CCSPlayerController? player, CommandInfo command)
    {
        if(player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if(p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if(p.CurrentSavedLocation == p.SavedLocations.Count-1)
        {
            p.Controller.PrintToChat($"{PluginPrefix} {ChatColors.Red}Already at last location");
        }
        else
        {
            p.CurrentSavedLocation++;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }
}