using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Admin;

namespace SurfTimer;

public partial class SurfTimer
{
    [ConsoleCommand("css_r", "Reset back to the start of the map.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerReset(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            Server.NextFrame(() =>  // Weird CS2 bug that requires doing this twice to show the Joined X team in chat and not stay in limbo
                {
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    player.Respawn();

                    player.ChangeTeam(CsTeam.Spectator);

                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    player.Respawn();
                }
            );
        }

        Player oPlayer = playerList[player.UserId ?? 0];
        if (oPlayer.ReplayRecorder.IsSaving)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["reset_delay"]}");
            return;
        }

        playerList[player.UserId ?? 0].Timer.Reset();
        if (!CurrentMap.StartZone.IsZero())
            Server.NextFrame(() =>
            {
                Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StartZone);
            }
        );
    }

    [ConsoleCommand("css_rs", "Reset back to the start of the stage or bonus you were in.")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerResetStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            Server.NextFrame(() =>  // Weird CS2 bug that requires doing this twice to show the Joined X team in chat and not stay in limbo
                {
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    player.Respawn();

                    player.ChangeTeam(CsTeam.Spectator);

                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    player.Respawn();
                }
            );
        }

        Player oPlayer = playerList[player.UserId ?? 0];
        if (oPlayer.ReplayRecorder.IsSaving)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["reset_delay"]}");
            return;
        }


        if (oPlayer.Timer.IsBonusMode)
        {
            if (oPlayer.Timer.Bonus != 0 && !CurrentMap.BonusStartZone[oPlayer.Timer.Bonus].IsZero())
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.BonusStartZone[oPlayer.Timer.Bonus]));
            else // Reset back to map start
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StartZone));
        }
        else
        {
            if (oPlayer.Timer.Stage != 0 && !CurrentMap.StageStartZone[oPlayer.Timer.Stage].IsZero())
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StageStartZone[oPlayer.Timer.Stage]));
            else // Reset back to map start
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StartZone));
        }
    }

    [ConsoleCommand("css_s", "Teleport to a stage")]
    [ConsoleCommand("css_stage", "Teleport to a stage")]
    [CommandHelper(minArgs: 1, usage: "<Stage Number> [1/2/3]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToStage(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        short stage;
        try
        {
            stage = short.Parse(command.ArgByIndex(1));
        }
        catch (System.Exception)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!s <stage>"]}"
            );
            return;
        }

        if (CurrentMap.Stages <= 0)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["not_staged"]}");
            return;
        }
        else if (stage > CurrentMap.Stages)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_stage_value",
                CurrentMap.Stages]}"
            );
            return;
        }

        if (!CurrentMap.StageStartZone[stage].IsZero())
        {
            playerList[player.UserId ?? 0].Timer.Reset();

            if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            {
                Server.NextFrame(() =>  // Weird CS2 bug that requires doing this twice to show the Joined X team in chat and not stay in limbo
                    {
                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        player.Respawn();

                        player.ChangeTeam(CsTeam.Spectator);

                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        player.Respawn();
                    }
                );
            }

            if (stage == 1)
            {
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StartZone));
            }
            else
            {
                playerList[player.UserId ?? 0].Timer.Stage = stage;
                Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.StageStartZone[stage]));
                playerList[player.UserId ?? 0].Timer.IsStageMode = true;
            }

            // To-do: If you run this while you're in the start zone, endtouch for the start zone runs after you've teleported
            //        causing the timer to start. This needs to be fixed.
        }
        else
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!s <stage>"]}"
            );
    }

    [ConsoleCommand("css_b", "Teleport to a bonus")]
    [ConsoleCommand("css_bonus", "Teleport to a bonus")]
    [CommandHelper(minArgs: 1, usage: "<Bonus Number> [1/2/3]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerGoToBonus(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int bonus;

        try
        {
            bonus = Int32.Parse(command.ArgByIndex(1));
        }
        catch (System.Exception)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!b <bonus>"]}"
            );
            return;
        }

        if (CurrentMap.Bonuses <= 0)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["not_bonused"]}");
            return;
        }
        else if (bonus > CurrentMap.Bonuses)
        {
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_bonus_value",
                CurrentMap.Bonuses]}"
            );
            return;
        }

        if (!CurrentMap.BonusStartZone[bonus].IsZero())
        {
            playerList[player.UserId ?? 0].Timer.Reset();
            playerList[player.UserId ?? 0].Timer.IsBonusMode = true;

            if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
            {
                Server.NextFrame(() =>  // Weird CS2 bug that requires doing this twice to show the Joined X team in chat and not stay in limbo
                    {
                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        player.Respawn();

                        player.ChangeTeam(CsTeam.Spectator);

                        player.ChangeTeam(CsTeam.CounterTerrorist);
                        player.Respawn();
                    }
                );
            }

            Server.NextFrame(() => Extensions.Teleport(player.PlayerPawn.Value!, CurrentMap.BonusStartZone[bonus]));
        }
        else
            player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["invalid_usage",
                "!b <bonus>"]}"
            );
    }

    [ConsoleCommand("css_spec", "Moves a player automaticlly into spectator mode")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void MovePlayerToSpectator(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        Server.NextFrame(() =>
            player.ChangeTeam(CsTeam.Spectator)
        );
    }

    [ConsoleCommand("css_rank", "Show the current rank of the player for the style they are in")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void PlayerRank(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        int pRank = playerList[player.UserId ?? 0].Stats.PB[playerList[player.UserId ?? 0].Timer.Style].Rank;
        int tRank = CurrentMap.MapCompletions[playerList[player.UserId ?? 0].Timer.Style];
        player.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["rank",
            CurrentMap.Name!, pRank, tRank]}"
        );
    }


    /*
    #########################
        Replay Commands
    #########################
    */
    [ConsoleCommand("css_replaybotpause", "Pause the replay bot playback")]
    [ConsoleCommand("css_rbpause", "Pause the replay bot playback")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
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
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void SavePlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;
        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.Respawn();
        }

        Player p = playerList[player.UserId ?? 0];
        if (!p.Timer.IsRunning)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_not_in_run"]}");
            return;
        }

        var player_pos = p.Controller.Pawn.Value!.AbsOrigin!;
        var player_angle = p.Controller.PlayerPawn.Value!.EyeAngles;
        var player_velocity = p.Controller.PlayerPawn.Value!.AbsVelocity;

        p.SavedLocations.Add(new SavelocFrame
        {
            Pos = new VectorT(player_pos.X, player_pos.Y, player_pos.Z),
            Ang = new QAngleT(player_angle.X, player_angle.Y, player_angle.Z),
            Vel = new VectorT(player_velocity.X, player_velocity.Y, player_velocity.Z),
            Tick = p.Timer.Ticks
        });
        p.CurrentSavedLocation = p.SavedLocations.Count - 1;

        p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_saved",
            p.SavedLocations.Count - 1]}"
        );
    }

    [ConsoleCommand("css_tele", "Teleport player to current saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportPlayerLocation(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;
        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.Respawn();
        }

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_no_locations"]}");
            return;
        }

        if (!p.Timer.IsRunning)
            p.Timer.Start();

        if (!p.Timer.IsPracticeMode)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_practice"]}");
            p.Timer.IsPracticeMode = true;
        }

        if (command.ArgCount > 1)
        {
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
        }
        SavelocFrame location = p.SavedLocations[p.CurrentSavedLocation];
        Server.NextFrame(() =>
            {
                Extensions.Teleport(p.Controller.PlayerPawn.Value!, location.Pos, location.Ang, location.Vel);
                p.Timer.Ticks = location.Tick;
            }
        );

        p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_teleported",
            p.CurrentSavedLocation]}"
        );
    }

    [ConsoleCommand("css_teleprev", "Teleport player to previous saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportPlayerLocationPrev(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;
        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.Respawn();
        }

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_no_locations"]}");
            return;
        }

        if (p.CurrentSavedLocation == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_first"]}");
        }
        else
        {
            p.CurrentSavedLocation--;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_teleported",
            p.CurrentSavedLocation]}"
        );
    }

    [ConsoleCommand("css_telenext", "Teleport player to next saved location")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void TeleportPlayerLocationNext(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;
        if (player.Team == CsTeam.Spectator || player.Team == CsTeam.None)
        {
            player.ChangeTeam(CsTeam.CounterTerrorist);
            player.Respawn();
        }

        Player p = playerList[player.UserId ?? 0];

        if (p.SavedLocations.Count == 0)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_no_locations"]}");
            return;
        }

        if (p.CurrentSavedLocation == p.SavedLocations.Count - 1)
        {
            p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_last"]}");
        }
        else
        {
            p.CurrentSavedLocation++;
        }

        TeleportPlayerLocation(player, command);

        p.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["saveloc_teleported",
            p.CurrentSavedLocation]}"
        );
    }




    /*
    ########################
           TEST CMDS
    ########################
    */
    [ConsoleCommand("css_rx", "x")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void TestSituationCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];

        CurrentRun.PrintSituations(oPlayer);
    }

    [ConsoleCommand("css_testx", "x")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    [RequiresPermissions("@css/root")]
    public void TestCmd(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
            return;

        Player oPlayer = playerList[player.UserId ?? 0];
        int style = oPlayer.Timer.Style;

        player.PrintToChat($"{Config.PluginPrefix}{ChatColors.Lime}====== PLAYER ======");
        player.PrintToChat($"{Config.PluginPrefix} Profile ID: {ChatColors.Green}{oPlayer.Profile.ID}");
        player.PrintToChat($"{Config.PluginPrefix} Steam ID: {ChatColors.Green}{oPlayer.Profile.SteamID}");
        player.PrintToChat($"{Config.PluginPrefix} MapTime ID: {ChatColors.Green}{oPlayer.Stats.PB[style].ID} - {PlayerHud.FormatTime(oPlayer.Stats.PB[style].RunTime)}");
        player.PrintToChat($"{Config.PluginPrefix} Stage: {ChatColors.Green}{oPlayer.Timer.Stage}");
        player.PrintToChat($"{Config.PluginPrefix} IsStageMode: {ChatColors.Green}{oPlayer.Timer.IsStageMode}");
        player.PrintToChat($"{Config.PluginPrefix} IsRunning: {ChatColors.Green}{oPlayer.Timer.IsRunning}");
        player.PrintToChat($"{Config.PluginPrefix} Checkpoint: {ChatColors.Green}{oPlayer.Timer.Checkpoint}");
        player.PrintToChat($"{Config.PluginPrefix} Bonus: {ChatColors.Green}{oPlayer.Timer.Bonus}");
        player.PrintToChat($"{Config.PluginPrefix} Ticks: {ChatColors.Green}{oPlayer.Timer.Ticks}");
        player.PrintToChat($"{Config.PluginPrefix} StagePB ID: {ChatColors.Green}{oPlayer.Stats.StagePB[1][style].ID} - {PlayerHud.FormatTime(oPlayer.Stats.StagePB[1][style].RunTime)}");


        player.PrintToChat($"{Config.PluginPrefix}{ChatColors.Orange}====== MAP ======");
        player.PrintToChat($"{Config.PluginPrefix} Map ID: {ChatColors.Green}{CurrentMap.ID}");
        player.PrintToChat($"{Config.PluginPrefix} Map Name: {ChatColors.Green}{CurrentMap.Name}");
        player.PrintToChat($"{Config.PluginPrefix} Map Stages: {ChatColors.Green}{CurrentMap.Stages}");
        player.PrintToChat($"{Config.PluginPrefix} Map Bonuses: {ChatColors.Green}{CurrentMap.Bonuses}");
        player.PrintToChat($"{Config.PluginPrefix} Map Completions (Style: {ChatColors.Green}{style}{ChatColors.Default}): {ChatColors.Green}{CurrentMap.MapCompletions[style]}");
        player.PrintToChat($"{Config.PluginPrefix} CurrentMap.WR[{style}].Ticks: {ChatColors.Green}{CurrentMap.WR[style].RunTime}");
        player.PrintToChat($"{Config.PluginPrefix} CurrentMap.WR[{style}].Checkpoints.Count: {ChatColors.Green}{CurrentMap.WR[style].Checkpoints!.Count}");


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
    }

    [ConsoleCommand("css_ctest", "x")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    [RequiresPermissions("@css/root")]
    public void ConsoleTestCmd(CCSPlayerController? player, CommandInfo command)
    {
        Console.WriteLine("====== MAP INFO ======");
        Console.WriteLine($"Map ID: {CurrentMap.ID}");
        Console.WriteLine($"Map Name: {CurrentMap.Name}");
        Console.WriteLine($"Map Author: {CurrentMap.Author}");
        Console.WriteLine($"Map Tier: {CurrentMap.Tier}");
        Console.WriteLine($"Map Stages: {CurrentMap.Stages}");
        Console.WriteLine($"Map Bonuses: {CurrentMap.Bonuses}");
        Console.WriteLine($"Map Completions: {CurrentMap.MapCompletions[0]}");

        Console.WriteLine("====== MAP WR INFO ======");
        Console.WriteLine($"Map WR ID: {CurrentMap.WR[0].ID}");
        Console.WriteLine($"Map WR Name: {CurrentMap.WR[0].Name}");
        Console.WriteLine($"Map WR Type: {CurrentMap.WR[0].Type}");
        Console.WriteLine($"Map WR Rank: {CurrentMap.WR[0].Rank}");
        Console.WriteLine($"Map WR Checkpoints.Count: {CurrentMap.WR[0].Checkpoints?.Count}");
        Console.WriteLine($"Map WR ReplayFramesBase64.Length: {CurrentMap.WR[0].ReplayFrames?.ToString().Length}");
        Console.WriteLine($"Map WR ReplayFrames.Length: {CurrentMap.WR[0].ReplayFrames?.ToString().Length}");

        Console.WriteLine("====== MAP StageWR INFO ======");
        Console.WriteLine($"Map Stage Completions: {CurrentMap.StageCompletions.Length}");
        Console.WriteLine($"Map StageWR ID: {CurrentMap.StageWR[1][0].ID}");
        Console.WriteLine($"Map StageWR Name: {CurrentMap.StageWR[1][0].Name}");
        Console.WriteLine($"Map StageWR Type: {CurrentMap.StageWR[1][0].Type}");
        Console.WriteLine($"Map StageWR Rank: {CurrentMap.StageWR[1][0].Rank}");
        Console.WriteLine($"Map StageWR ReplayFramesBase64.Length: {CurrentMap.StageWR[1][0].ReplayFrames?.ToString().Length}");
        Console.WriteLine($"Map StageWR ReplayFrames.Length: {CurrentMap.StageWR[1][0].ReplayFrames?.ToString().Length}");

        Console.WriteLine($"Map Bonus Completions: {CurrentMap.BonusCompletions.Length}");
    }
}