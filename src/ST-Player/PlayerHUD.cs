using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

internal class PlayerHUD
{
    private Player _player;

    public PlayerHUD(Player Player)
    {
        _player = Player;
    }

    private string FormatHUDElementHTML(string title, string body, string color, string size = "m")
    {
        if (title != "")
        {
            if (size == "m")
                return $"{title}: <font color='{color}'>{body}</font>";
            else
                return $"<font class='fontSize-{size.ToLower()}'>{title}: <font color='{color}'>{body}</font></font>";
        }

        else
        {
            if (size == "m")
                return $"<font color='{color}'>{body}</font>";
            else
                return $"<font class='fontSize-{size.ToLower()}' color='{color}'>{body}</font>";
        }
    }

    /// <summary>
    /// Formats the given time in ticks into a readable time string.
    /// Unless specified differently, the default formatting will be `Compact`.
    /// Check <see cref="PlayerTimer.TimeFormatStyle"/> for all formatting types.
    /// </summary>
    public static string FormatTime(int ticks, PlayerTimer.TimeFormatStyle style = PlayerTimer.TimeFormatStyle.Compact)
    {
        TimeSpan time = TimeSpan.FromSeconds(ticks / 64.0);
        int millis = (int)(ticks % 64 * (1000.0 / 64.0));

        switch (style)
        {
            case PlayerTimer.TimeFormatStyle.Compact:
                return time.TotalMinutes < 1
                    ? $"{time.Seconds:D2}.{millis:D3}"
                    : $"{time.Minutes:D1}:{time.Seconds:D2}.{millis:D3}";
            case PlayerTimer.TimeFormatStyle.Full:
                return time.TotalHours < 1
                    ? $"{time.Minutes:D2}:{time.Seconds:D2}.{millis:D3}"
                    : $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{millis:D3}";
            case PlayerTimer.TimeFormatStyle.Verbose:
                return $"{time.Hours}h {time.Minutes}m {time.Seconds}s {millis}ms";
            default:
                throw new ArgumentException("Invalid time format style");
        }
    }

    /// <summary>
    /// Displays the Center HUD for the client
    /// </summary>
    public void Display()
    {
        if (!_player.Controller.IsValid)
            return;

        if (_player.Controller.PawnIsAlive)
        {
            int style = _player.Timer.Style;
            // Timer Module
            string timerColor = "#79d1ed";

            if (_player.Timer.IsRunning)
            {
                if (_player.Timer.IsPracticeMode)
                    timerColor = "#F2C94C";
                else
                    timerColor = "#2E9F65";
            }

            string timerModule;
            if (_player.Timer.IsBonusMode)
                timerModule = FormatHUDElementHTML("", $"[B{_player.Timer.Bonus}] " + FormatTime(_player.Timer.Ticks), timerColor);
            else if (_player.Timer.IsStageMode)
                timerModule = FormatHUDElementHTML("", $"[S{_player.Timer.Stage}] " + FormatTime(_player.Timer.Ticks), timerColor);
            else
                timerModule = FormatHUDElementHTML("", FormatTime(_player.Timer.Ticks), timerColor);

            // Velocity Module - To-do: Make velocity module configurable (XY or XYZ velocity)
            float velocity = (float)Math.Sqrt(_player.Controller.PlayerPawn.Value!.AbsVelocity.X * _player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                                + _player.Controller.PlayerPawn.Value!.AbsVelocity.Y * _player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                                + _player.Controller.PlayerPawn.Value!.AbsVelocity.Z * _player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
            string velocityModule = FormatHUDElementHTML("Speed", velocity.ToString("0"), "#79d1ed") + " u/s";
            // Rank Module
            string rankModule = FormatHUDElementHTML("Rank", $"N/A", "#7882dd");
            if (_player.Timer.IsBonusMode)
            {
                if (_player.Stats.BonusPB[_player.Timer.Bonus][style].ID != -1 && SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"{_player.Stats.BonusPB[_player.Timer.Bonus][style].Rank}/{SurfTimer.CurrentMap.BonusCompletions[_player.Timer.Bonus][style]}", "#7882dd");
                else if (SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"-/{SurfTimer.CurrentMap.BonusCompletions[_player.Timer.Bonus][style]}", "#7882dd");
            }
            else if (_player.Timer.IsStageMode)
            {
                if (_player.Stats.StagePB[_player.Timer.Stage][style].ID != -1 && SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"{_player.Stats.StagePB[_player.Timer.Stage][style].Rank}/{SurfTimer.CurrentMap.StageCompletions[_player.Timer.Stage][style]}", "#7882dd");
                else if (SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"-/{SurfTimer.CurrentMap.StageCompletions[_player.Timer.Stage][style]}", "#7882dd");
            }
            else
            {
                if (_player.Stats.PB[style].ID != -1 && SurfTimer.CurrentMap.WR[style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"{_player.Stats.PB[style].Rank}/{SurfTimer.CurrentMap.MapCompletions[style]}", "#7882dd");
                else if (SurfTimer.CurrentMap.WR[style].ID != -1)
                    rankModule = FormatHUDElementHTML("Rank", $"-/{SurfTimer.CurrentMap.MapCompletions[style]}", "#7882dd");
            }

            // PB & WR Modules
            string pbModule = FormatHUDElementHTML("PB", _player.Stats.PB[style].Ticks > 0 ? FormatTime(_player.Stats.PB[style].Ticks) : "N/A", "#7882dd");
            string wrModule = FormatHUDElementHTML("WR", SurfTimer.CurrentMap.WR[style].Ticks > 0 ? FormatTime(SurfTimer.CurrentMap.WR[style].Ticks) : "N/A", "#ffc61a");

            if (_player.Timer.Bonus > 0 && _player.Timer.IsBonusMode) // Show corresponding bonus values
            {
                pbModule = FormatHUDElementHTML("PB", _player.Stats.BonusPB[_player.Timer.Bonus][style].Ticks > 0 ? FormatTime(_player.Stats.BonusPB[_player.Timer.Bonus][style].Ticks) : "N/A", "#7882dd");
                wrModule = FormatHUDElementHTML("WR", SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].Ticks > 0 ? FormatTime(SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].Ticks) : "N/A", "#ffc61a");
            }
            else if (_player.Timer.IsStageMode) // Show corresponding stage values
            {
                pbModule = FormatHUDElementHTML("PB", _player.Stats.StagePB[_player.Timer.Stage][style].Ticks > 0 ? FormatTime(_player.Stats.StagePB[_player.Timer.Stage][style].Ticks) : "N/A", "#7882dd");
                wrModule = FormatHUDElementHTML("WR", SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].Ticks > 0 ? FormatTime(SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].Ticks) : "N/A", "#ffc61a");
            }

            // Build HUD
            string hud = $"{timerModule}<br>{velocityModule}<br>{pbModule} | {rankModule}<br>{wrModule}";

            // Display HUD
            _player.Controller.PrintToCenterHtml(hud);
        }
        else if (_player.Controller.Team == CsTeam.Spectator)
        {
            ReplayPlayer? spec_replay;

            if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.MapWR.Controller!))
                spec_replay = SurfTimer.CurrentMap.ReplayManager.MapWR;
            else if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.StageWR?.Controller!))
                spec_replay = SurfTimer.CurrentMap.ReplayManager.StageWR!;
            else if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.BonusWR?.Controller!))
                spec_replay = SurfTimer.CurrentMap.ReplayManager.BonusWR!;
            else
                spec_replay = SurfTimer.CurrentMap.ReplayManager.CustomReplays.Find(x => _player.IsSpectating(x.Controller!));

            if (spec_replay != null)
            {
                string replayModule = $"{FormatHUDElementHTML("", "REPLAY", "red", "large")}";
                string nameModule = FormatHUDElementHTML($"{spec_replay.RecordPlayerName}", $"{FormatTime(spec_replay.RecordRunTime)}", "#ffd500");
                string hud = $"{replayModule}<br>{nameModule}";

                _player.Controller.PrintToCenterHtml(hud);
            }
        }
    }

    /// <summary>
    /// Only calculates if the player has a PB, otherwise it will display N/A
    /// </summary>
    public void DisplayCheckpointMessages()
    {
        int pbTime;
        int wrTime = -1;
        float pbSpeed;
        float wrSpeed = -1.0f;
        int style = _player.Timer.Style;
        int playerCheckpoint = _player.Timer.Checkpoint;

        // _player.Controller.PrintToChat($"{ChatColors.Blue}-> PlayerHUD{ChatColors.Default} => Style {ChatColors.Yellow}{style}{ChatColors.Default} | Checkpoint {playerCheckpoint} | WR Time Ticks {SurfTimer.CurrentMap.WR[style].Ticks} | Player Stage {_player.Timer.Stage} (CP {_player.Timer.Checkpoint}) | Player Ticks {_player.Timer.Ticks}");

        int currentTime = _player.Timer.Ticks;
        float currentSpeed = (float)Math.Sqrt(_player.Controller.PlayerPawn.Value!.AbsVelocity.X * _player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                        + _player.Controller.PlayerPawn.Value!.AbsVelocity.Y * _player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                        + _player.Controller.PlayerPawn.Value!.AbsVelocity.Z * _player.Controller.PlayerPawn.Value!.AbsVelocity.Z);

        // Default values for the PB and WR differences in case no calculations can be made
        string strPbDifference = $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";
        string strWrDifference = $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";

        // We need to try/catch this because the player might not have a PB for this checkpoint in this case but they will not have for the map as well
        // Can check checkpoints count instead of try/catch
        try
        {
            pbTime = _player.Stats.PB[style].Checkpoints[playerCheckpoint].Ticks;
            pbSpeed = (float)Math.Sqrt(_player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelX * _player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelX
                                        + _player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelY * _player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelY
                                        + _player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelZ * _player.Stats.PB[style].Checkpoints[playerCheckpoint].StartVelZ);

#if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  Got pbTime from _player.Stats.PB[{style}].Checkpoint[{playerCheckpoint} = {pbTime}]");
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] Got pbSpeed from _player.Stats.PB[{style}].Checkpoint[{playerCheckpoint}] = {pbSpeed}");
#endif
        }
#if DEBUG
        catch (System.Exception ex)
#else
        catch (System.Exception)
#endif
        {
            // Handle the exception gracefully without stopping
            // We assign default values to pbTime and pbSpeed
            pbTime = -1; // This determines if we will calculate differences or not!!!
            pbSpeed = 0.0f;

#if DEBUG
            Console.WriteLine($"CS2 Surf CAUGHT EXCEPTION >> DisplayCheckpointMessages -> An error occurred: {ex.Message}");
            Console.WriteLine($"CS2 Surf CAUGHT EXCEPTION >> DisplayCheckpointMessages -> An error occurred Player has no PB and therefore no Checkpoints | _player.Stats.PB[{style}].Checkpoint.Count = {_player.Stats.PB[style].Checkpoints.Count}");
#endif
        }

        // Calculate differences in PB (PB - Current)
        if (pbTime != -1)
        {
#if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> Starting PB difference calculation... (pbTime != -1)");
#endif
            // Reset the string
            strPbDifference = "";

            // Calculate the time difference
            if (pbTime - currentTime < 0.0)
            {
                strPbDifference += ChatColors.Red + "+" + FormatTime((pbTime - currentTime) * -1); // We multiply by -1 to get the positive value
            }
            else if (pbTime - currentTime >= 0.0)
            {
                strPbDifference += ChatColors.Green + "-" + FormatTime(pbTime - currentTime);
            }
            strPbDifference += ChatColors.Default + " ";

            // Calculate the speed difference
            if (pbSpeed - currentSpeed <= 0.0)
            {
                strPbDifference += "(" + ChatColors.Green + "+" + ((pbSpeed - currentSpeed) * -1).ToString("0"); // We multiply by -1 to get the positive value
            }
            else if (pbSpeed - currentSpeed > 0.0)
            {
                strPbDifference += "(" + ChatColors.Red + "-" + (pbSpeed - currentSpeed).ToString("0");
            }
            strPbDifference += ChatColors.Default + ")";
        }

        if (SurfTimer.CurrentMap.WR[style].Ticks > 0)
        {
            // Calculate differences in WR (WR - Current)
#if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> Starting WR difference calculation... (SurfTimer.CurrentMap.WR[{style}].Ticks > 0)");
#endif

            wrTime = SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].Ticks;
            wrSpeed = (float)Math.Sqrt(SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelX * SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelX
                                        + SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelY * SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelY
                                        + SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelZ * SurfTimer.CurrentMap.WR[style].Checkpoints[playerCheckpoint].StartVelZ);
            // Reset the string
            strWrDifference = "";

            // Calculate the WR time difference
            if (wrTime - currentTime < 0.0)
            {
                strWrDifference += ChatColors.Red + "+" + FormatTime((wrTime - currentTime) * -1); // We multiply by -1 to get the positive value
            }
            else if (wrTime - currentTime >= 0.0)
            {
                strWrDifference += ChatColors.Green + "-" + FormatTime(wrTime - currentTime);
            }
            strWrDifference += ChatColors.Default + " ";

            // Calculate the WR speed difference
            if (wrSpeed - currentSpeed <= 0.0)
            {
                strWrDifference += "(" + ChatColors.Green + "+" + ((wrSpeed - currentSpeed) * -1).ToString("0"); // We multiply by -1 to get the positive value
            }
            else if (wrSpeed - currentSpeed > 0.0)
            {
                strWrDifference += "(" + ChatColors.Red + "-" + (wrSpeed - currentSpeed).ToString("0");
            }
            strWrDifference += ChatColors.Default + ")";
        }

        // Print checkpoint message
        _player.Controller.PrintToChat($"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["checkpoint_message",
            playerCheckpoint, FormatTime(_player.Timer.Ticks), currentSpeed.ToString("0"), strPbDifference, strWrDifference]}"
        );

#if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  PB: {pbTime} - CURR: {currentTime} = pbTime: {pbTime - currentTime}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] PB: {pbSpeed} - CURR: {currentSpeed} = difference: {pbSpeed - currentSpeed}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  WR: {wrTime} - CURR: {currentTime} = difference: {wrTime - currentTime}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] WR: {wrSpeed} - CURR: {currentSpeed} = difference: {wrSpeed - currentSpeed}");
#endif
    }
}
