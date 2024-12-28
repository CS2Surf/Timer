using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public partial class SurfTimer
{
    [ConsoleCommand("css_r", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerReset(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];
        if (oPlayer.ReplayRecorder.IsSaving)
        {
            player.PrintToChat($"{Config.PluginPrefix} Please wait for your run to be saved before resetting.");
            return;
        }
        // To-do: players[userid].Timer.Reset() -> teleport player
        playerList[player.UserId ?? 0].Timer.Reset();
        if (CurrentMap.StartZone != new Vector(0, 0, 0))
            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
    }

    [ConsoleCommand("css_rs", "Reset back to the start of the stage or bonus you're in.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerResetStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];
        if (oPlayer.ReplayRecorder.IsSaving)
        {
            player.PrintToChat($"{Config.PluginPrefix} Please wait for your run to be saved before resetting.");
            return;
        }


        if (oPlayer.Timer.IsBonusMode)
        {
            if (oPlayer.Timer.Bonus != 0 && CurrentMap.BonusStartZone[oPlayer.Timer.Bonus] != new Vector(0, 0, 0))
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.BonusStartZone[oPlayer.Timer.Bonus], CurrentMap.BonusStartZoneAngles[oPlayer.Timer.Bonus], new Vector(0, 0, 0)));
            else // Reset back to map start
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        }

        else
        {
            if (oPlayer.Timer.Stage != 0 && CurrentMap.StageStartZone[oPlayer.Timer.Stage] != new Vector(0, 0, 0))
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StageStartZone[oPlayer.Timer.Stage], CurrentMap.StageStartZoneAngles[oPlayer.Timer.Stage], new Vector(0, 0, 0)));
            else // Reset back to map start
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, new QAngle(0, 0, 0), new Vector(0, 0, 0)));
        }
    }

    [ConsoleCommand("css_s", "Teleport to a stage")]
    [ConsoleCommand("css_stage", "Teleport to a stage")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        int stage;
        try
        {
            stage = Int32.Parse(command.ArgByIndex(1));
        }
        catch (System.Exception)
        {
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid arguments. Usage: {ChatColors.Green}!s <stage>");
            return;
        }

        // Must be 1 argument
        if (command.ArgCount < 2 || stage <= 0)
        {
#if DEBUG
            player.PrintToChat($"CS2 Surf DEBUG >> css_stage >> Arg#: {command.ArgCount} >> Args: {Int32.Parse(command.ArgByIndex(1))}");
#endif

            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid arguments. Usage: {ChatColors.Green}!s <stage>");
            return;
        }

        else if (CurrentMap.Stages <= 0)
        {
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}This map has no stages.");
            return;
        }

        else if (stage > CurrentMap.Stages)
        {
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid stage provided, this map has {ChatColors.Green}{CurrentMap.Stages} stages.");
            return;
        }

        if (CurrentMap.StageStartZone[stage] != new Vector(0, 0, 0))
        {
            playerList[player.UserId ?? 0].Timer.Reset();

            if (stage == 1)
            {
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StartZone, CurrentMap.StartZoneAngles, new Vector(0, 0, 0)));
            }
            else
            {
                playerList[player.UserId ?? 0].Timer.Stage = stage;
                Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.StageStartZone[stage], CurrentMap.StageStartZoneAngles[stage], new Vector(0, 0, 0)));
                playerList[player.UserId ?? 0].Timer.IsStageMode = true;
            }

            // To-do: If you run this while you're in the start zone, endtouch for the start zone runs after you've teleported
            //        causing the timer to start. This needs to be fixed.
        }

        else
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid stage provided. Usage: {ChatColors.Green}!s <stage>");
    }

    [ConsoleCommand("css_b", "Teleport to a bonus")]
    [ConsoleCommand("css_bonus", "Teleport to a bonus")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToBonus(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            return;

        int bonus;

        // Check for argument count
        if (command.ArgCount < 2)
        {
            if (CurrentMap.Bonuses > 0)
                bonus = 1;
            else
            {
                player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid arguments. Usage: {ChatColors.Green}!bonus <bonus>");
                return;
            }
        }

        else
            bonus = Int32.Parse(command.ArgByIndex(1));

        if (CurrentMap.Bonuses <= 0)
        {
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}This map has no bonuses.");
            return;
        }

        else if (bonus > CurrentMap.Bonuses)
        {
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid bonus provided, this map has {ChatColors.Green}{CurrentMap.Bonuses} bonuses.");
            return;
        }

        if (CurrentMap.BonusStartZone[bonus] != new Vector(0, 0, 0))
        {
            playerList[player.UserId ?? 0].Timer.Reset();
            playerList[player.UserId ?? 0].Timer.IsBonusMode = true;

            Server.NextFrame(() => player.PlayerPawn.Value!.Teleport(CurrentMap.BonusStartZone[bonus], CurrentMap.BonusStartZoneAngles[bonus], new Vector(0, 0, 0)));
        }

        else
            player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Invalid bonus provided. Usage: {ChatColors.Green}!bonus <bonus>");
    }

    [ConsoleCommand("css_spec", "Moves a player automaticlly into spectator mode")]
    public void MovePlayerToSpectator(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team == CsTeam.Spectator)
            return;

        player.ChangeTeam(CsTeam.Spectator);
    }

    [ConsoleCommand("css_rank", "Show the current rank of the player for the style they are in")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerRank(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        player.PrintToChat($"{Config.PluginPrefix} Your current rank for {ChatColors.Gold}{CurrentMap.Name}{ChatColors.Default} is {ChatColors.Green}{playerList[player.UserId ?? 0].Stats.PB[playerList[player.UserId ?? 0].Timer.Style].Rank}{ChatColors.Default} out of {ChatColors.Yellow}{playerList[player.UserId ?? 0].CurrMap.MapCompletions[playerList[player.UserId ?? 0].Timer.Style]}");
    }

    [ConsoleCommand("css_testx", "x")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TestCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];
        int style = oPlayer.Timer.Style;

        // player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Testing 'PB_LoadMapTimeData'");
        player.PrintToChat($"{Config.PluginPrefix}{ChatColors.Lime}====== PLAYER ======");
        player.PrintToChat($"{Config.PluginPrefix} Profile ID: {ChatColors.Green}{oPlayer.Profile.ID}");
        player.PrintToChat($"{Config.PluginPrefix} Steam ID: {ChatColors.Green}{oPlayer.Profile.SteamID}");
        player.PrintToChat($"{Config.PluginPrefix} MapTime ID: {ChatColors.Green}{oPlayer.Stats.PB[style].ID} - {PlayerHUD.FormatTime(oPlayer.Stats.PB[style].Ticks)}");
        player.PrintToChat($"{Config.PluginPrefix} Stage: {ChatColors.Green}{oPlayer.Timer.Stage}");
        player.PrintToChat($"{Config.PluginPrefix} IsStageMode: {ChatColors.Green}{oPlayer.Timer.IsStageMode}");
        player.PrintToChat($"{Config.PluginPrefix} IsRunning: {ChatColors.Green}{oPlayer.Timer.IsRunning}");
        player.PrintToChat($"{Config.PluginPrefix} Checkpoint: {ChatColors.Green}{oPlayer.Timer.Checkpoint}");
        player.PrintToChat($"{Config.PluginPrefix} Bonus: {ChatColors.Green}{oPlayer.Timer.Bonus}");
        player.PrintToChat($"{Config.PluginPrefix} Ticks: {ChatColors.Green}{oPlayer.Timer.Ticks}");
        player.PrintToChat($"{Config.PluginPrefix} StagePB ID: {ChatColors.Green}{oPlayer.Stats.StagePB[1][style].ID} - {PlayerHUD.FormatTime(oPlayer.Stats.StagePB[1][style].Ticks)}");
        // player.PrintToChat($"{Config.PluginPrefix} StagePB ID: {ChatColors.Green}{oPlayer.Stats.StagePB[style][1].ID} - {PlayerHUD.FormatTime(oPlayer.Stats.StagePB[style][1].Ticks)}");


        player.PrintToChat($"{Config.PluginPrefix}{ChatColors.Orange}====== MAP ======");
        player.PrintToChat($"{Config.PluginPrefix} Map ID: {ChatColors.Green}{CurrentMap.ID}");
        player.PrintToChat($"{Config.PluginPrefix} Map Name: {ChatColors.Green}{CurrentMap.Name}");
        player.PrintToChat($"{Config.PluginPrefix} Map Stages: {ChatColors.Green}{CurrentMap.Stages}");
        player.PrintToChat($"{Config.PluginPrefix} Map Bonuses: {ChatColors.Green}{CurrentMap.Bonuses}");
        player.PrintToChat($"{Config.PluginPrefix} Map Completions (Style: {ChatColors.Green}{style}{ChatColors.Default}): {ChatColors.Green}{CurrentMap.MapCompletions[style]}");
        player.PrintToChat($"{Config.PluginPrefix} .CurrentMap.WR[].Ticks: {ChatColors.Green}{CurrentMap.WR[style].Ticks}");
        player.PrintToChat($"{Config.PluginPrefix} .CurrentMap.WR[].Checkpoints.Count: {ChatColors.Green}{CurrentMap.WR[style].Checkpoints.Count}");


        player.PrintToChat($"{Config.PluginPrefix}{ChatColors.Purple}====== REPLAYS ======");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayRecorder.Frames.Count: {ChatColors.Green}{oPlayer.ReplayRecorder.Frames.Count}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayRecorder.IsRecording: {ChatColors.Green}{oPlayer.ReplayRecorder.IsRecording}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.MapWR.RecordRunTime: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.RecordRunTime}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.MapWR.Frames.Count: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.Frames.Count}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.MapWR.IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.IsPlayable}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.MapWR.MapSituations.Count: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.MapSituations.Count}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.StageWR.RecordRunTime: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR?.RecordRunTime}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.StageWR.Frames.Count: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR?.Frames.Count}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.StageWR.IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR?.IsPlayable}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.BonusWR.RecordRunTime: {ChatColors.Green}{CurrentMap.ReplayManager.BonusWR?.RecordRunTime}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.BonusWR.Frames.Count: {ChatColors.Green}{CurrentMap.ReplayManager.BonusWR?.Frames.Count}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.BonusWR.IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.BonusWR?.IsPlayable}");
        player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.BonusWR.IsPlaying: {ChatColors.Green}{CurrentMap.ReplayManager.BonusWR?.IsPlaying}");

        /*
                for (int i = 1; i < SurfTimer.CurrentMap.Stages; i++)
                {
                    player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.AllStageWR[{i}][0].RecordRunTime: {ChatColors.Green}{CurrentMap.ReplayManager.AllStageWR[i][0].RecordRunTime}");
                    player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.AllStageWR[{i}][0].Frames.Count: {ChatColors.Green}{CurrentMap.ReplayManager.AllStageWR[i][0].Frames.Count}");
                    player.PrintToChat($"{Config.PluginPrefix} .ReplayManager.AllStageWR[{i}][0].IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.AllStageWR[i][0].IsPlayable}");
                }
        */

        /*
                for (int i = 0; i < CurrentMap.ReplayManager.MapWR.Frames.Count; i++)
                {
                    ReplayFrame x = CurrentMap.ReplayManager.MapWR.Frames[i];

                    switch (x.Situation)
                    {
                        case ReplayFrameSituation.START_ZONE_ENTER:
                            player.PrintToChat($"Start Enter: {i} | Situation {x.Situation}");
                            break;
                        case ReplayFrameSituation.START_ZONE_EXIT:
                            player.PrintToChat($"Start Exit: {i} | Situation {x.Situation}");
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_ENTER:
                            player.PrintToChat($"Stage Enter: {i} | Situation {x.Situation}");
                            break;
                        case ReplayFrameSituation.STAGE_ZONE_EXIT:
                            player.PrintToChat($"Stage Exit: {i} | Situation {x.Situation}");
                            break;
                        case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
                            player.PrintToChat($"Checkpoint Enter: {i} | Situation {x.Situation}");
                            break;
                        case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
                            player.PrintToChat($"Checkpoint Exit: {i} | Situation {x.Situation}");
                            break;
                    }
                }
        */
        // for (int i = 0; i < CurrentMap.ReplayManager.MapWR.MapSituations.Count; i++)
        // {
        //     ReplayFrame x = CurrentMap.ReplayManager.MapWR.Frames[i];
        //     switch (x.Situation)
        //     {
        //         case ReplayFrameSituation.START_ZONE_ENTER:
        //             player.PrintToChat($"START_ZONE_ENTER: {i} | Situation {x.Situation}");
        //             break;
        //         case ReplayFrameSituation.START_ZONE_EXIT:
        //             player.PrintToChat($"START_ZONE_EXIT: {i} | Situation {x.Situation}");
        //             break;
        //         case ReplayFrameSituation.STAGE_ZONE_ENTER:
        //             player.PrintToChat($"STAGE_ZONE_ENTER: {i} | Situation {x.Situation}");
        //             break;
        //         case ReplayFrameSituation.STAGE_ZONE_EXIT:
        //             player.PrintToChat($"STAGE_ZONE_EXIT: {i} | Situation {x.Situation}");
        //             break;
        //         case ReplayFrameSituation.CHECKPOINT_ZONE_ENTER:
        //             player.PrintToChat($"CHECKPOINT_ZONE_ENTER: {i} | Situation {x.Situation}");
        //             break;
        //         case ReplayFrameSituation.CHECKPOINT_ZONE_EXIT:
        //             player.PrintToChat($"CHECKPOINT_ZONE_EXIT: {i} | Situation {x.Situation}");
        //             break;
        //     }
        // }

        // player.PrintToChat($"{Config.PluginPrefix} IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.IsPlayable}");
        // player.PrintToChat($"{Config.PluginPrefix} IsPlaying: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.IsPlaying}");
        // player.PrintToChat($"{Config.PluginPrefix} Player.IsSpectating: {ChatColors.Green}{oPlayer.IsSpectating(CurrentMap.ReplayManager.MapWR.Controller!)}");
        // player.PrintToChat($"{Config.PluginPrefix} Name & MapTimeID: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.RecordPlayerName} {CurrentMap.ReplayManager.MapWR.MapTimeID}");
        // player.PrintToChat($"{Config.PluginPrefix} ReplayCurrentRunTime: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.ReplayCurrentRunTime}");
        // player.PrintToChat($"{Config.PluginPrefix} RepeatCount: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.RepeatCount}");
        // player.PrintToChat($"{Config.PluginPrefix} IsReplayOutsideZone: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.IsReplayOutsideZone}");
        // player.PrintToChat($"{Config.PluginPrefix} CurrentFrameTick: {ChatColors.Green}{CurrentMap.ReplayManager.MapWR.CurrentFrameTick}");
        // player.PrintToChat($"{Config.PluginPrefix} ReplayRecorder.Frames.Length: {ChatColors.Green}{oPlayer.ReplayRecorder.Frames.Count}");

        // if (CurrentMap.ReplayManager.StageWR != null)
        // {
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.MapTimeID - Stage: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.MapTimeID} - {CurrentMap.ReplayManager.StageWR.Stage}");
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.IsPlayable: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.IsPlayable}");
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.IsEnabled: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.IsEnabled}");
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.IsPaused: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.IsPaused}");
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.IsPlaying: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.IsPlaying}");
        //     player.PrintToChat($"{Config.PluginPrefix} ReplayManager.StageWR.Controller Null?: {ChatColors.Green}{CurrentMap.ReplayManager.StageWR.Controller == null}");
        // }
    }

    /*
    #########################
        Replay Commands
    #########################
    */
    [ConsoleCommand("css_replaybotpause", "Pause the replay bot playback")]
    [ConsoleCommand("css_rbpause", "Pause the replay bot playback")]
    public void PauseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team != CsTeam.Spectator)
            return;

        foreach (ReplayPlayer rb in CurrentMap.ReplayManager.CustomReplays)
        {
            if (!rb.IsPlayable || !rb.IsPlaying || !playerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;

            rb.Pause();
        }
    }

    [ConsoleCommand("css_rbplay", "Start all replays from the start")]
    public void PlayReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team != CsTeam.Spectator)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];
        CurrentMap.ReplayManager.MapWR.ResetReplay();
        CurrentMap.ReplayManager.MapWR.Start();

        CurrentMap.ReplayManager.StageWR?.ResetReplay();
        CurrentMap.ReplayManager.StageWR?.Start();

        foreach (ReplayPlayer rb in CurrentMap.ReplayManager.CustomReplays)
        {
            if (!rb.IsPlayable || !rb.IsPlaying || !oPlayer.IsSpectating(rb.Controller!))
                continue;

            rb.Start();
        }
    }

    [ConsoleCommand("css_replaybotflip", "Flips the replay bot between Forward/Backward playback")]
    [ConsoleCommand("css_rbflip", "Flips the replay bot between Forward/Backward playback")]
    public void ReverseReplay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || player.Team != CsTeam.Spectator)
            return;

        foreach (ReplayPlayer rb in CurrentMap.ReplayManager.CustomReplays)
        {
            if (!rb.IsPlayable || !rb.IsPlaying || !playerList[player.UserId ?? 0].IsSpectating(rb.Controller!))
                continue;

            rb.FrameTickIncrement *= -1;
        }
    }

    // [ConsoleCommand("css_pbreplay", "Allows for replay of player's PB")]
    // public void PbReplay(CCSPlayerController? player, CommandInfo command)
    // {
    //     if(player == null)
    //         return;

    //     int maptime_id = playerList[player!.UserId ?? 0].Stats.PB[playerList[player.UserId ?? 0].Timer.Style].ID;
    //     if (command.ArgCount > 1)
    //     {
    //         try
    //         {
    //             maptime_id = int.Parse(command.ArgByIndex(1));
    //         }
    //         catch {}
    //     }

    //     if(maptime_id == -1 || !CurrentMap.ConnectedMapTimes.Contains(maptime_id))
    //     {
    //         player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}No time was found");
    //         return;
    //     }

    //     for(int i = 0; i < CurrentMap.ReplayBots.Count; i++)
    //     {
    //         if(CurrentMap.ReplayBots[i].MapTimeID == maptime_id)
    //         {
    //             player.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}A bot of this run already playing");
    //             return;
    //         }
    //     }

    //     CurrentMap.ReplayBots = CurrentMap.ReplayBots.Prepend(new ReplayPlayer() {
    //         Stat_MapTimeID = maptime_id,
    //         Stat_Prefix = "PB"
    //     }).ToList();

    //     Server.NextFrame(() => {
    //         Server.ExecuteCommand($"bot_quota {CurrentMap.ReplayBots.Count}");
    //     });
    // }

    /*
    ########################
        Saveloc Commands
    ########################
    */
    [ConsoleCommand("css_saveloc", "Save current player location to be practiced")]
    public void SavePlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];
        if (!p.Timer.IsRunning)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Cannot save location while not in run");
            return;
        }

        var player_pos = p.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = p.Controller.PlayerPawn.Value!.EyeAngles;
        var player_velocity = p.Controller.PlayerPawn.Value!.AbsVelocity;

        p.SavedLocations.Add(new SavelocFrame
        {
            Pos = new Vector(player_pos.X, player_pos.Y, player_pos.Z),
            Ang = new QAngle(player_angle.X, player_angle.Y, player_angle.Z),
            Vel = new Vector(player_velocity.X, player_velocity.Y, player_velocity.Z),
            Tick = p.Timer.Ticks
        });
        p.CurrentSavedLocation = p.SavedLocations.Count - 1;

        p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Green}Saved location! {ChatColors.Default} use !tele {p.SavedLocations.Count - 1} to teleport to this location");
    }

    [ConsoleCommand("css_tele", "Teleport player to current saved location")]
    public void TeleportPlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (!p.Timer.IsRunning)
            p.Timer.Start();

        if (!p.Timer.IsPracticeMode)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Timer now on practice");
            p.Timer.IsPracticeMode = true;
        }

        if (command.ArgCount > 1)
            try
            {
                int tele_n = int.Parse(command.ArgByIndex(1));
                if (tele_n < p.SavedLocations.Count)
                    p.CurrentSavedLocation = tele_n;
            }
            catch
            {
                Exception exception = new("sum ting wong");
                throw exception;
            }
        SavelocFrame location = p.SavedLocations[p.CurrentSavedLocation];
        Server.NextFrame(() =>
        {
            p.Controller.PlayerPawn.Value!.Teleport(location.Pos, location.Ang, location.Vel);
            p.Timer.Ticks = location.Tick;
        });

        p.Controller.PrintToChat($"{Config.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    [ConsoleCommand("css_teleprev", "Teleport player to previous saved location")]
    public void TeleportPlayerLocationPrev(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (p.CurrentSavedLocation == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Already at first location");
        }
        else
        {
            p.CurrentSavedLocation--;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{Config.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }

    [ConsoleCommand("css_telenext", "Teleport player to next saved location")]
    public void TeleportPlayerLocationNext(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.PawnIsAlive || !playerList.ContainsKey(player.UserId ?? 0))
            return;

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}No saved locations");
            return;
        }

        if (p.CurrentSavedLocation == p.SavedLocations.Count - 1)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {ChatColors.Red}Already at last location");
        }
        else
        {
            p.CurrentSavedLocation++;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{Config.PluginPrefix} Teleported #{p.CurrentSavedLocation}");
    }
}