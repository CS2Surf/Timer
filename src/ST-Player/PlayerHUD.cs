using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

public class PlayerHud
{
    private readonly Player _player;
    private readonly string TimerColor = "#4FC3F7";
    private readonly string TimerColorPractice = "#BA68C8";
    private readonly string TimerColorActive = "#43A047";
    private readonly string RankColorPb = "#7986CB";
    private readonly string RankColorWr = "#FFD700";
    private readonly string SpectatorColor = "#9E9E9E";

    internal PlayerHud(Player Player)
    {
        _player = Player;
    }

    private static string FormatHUDElementHTML(
        string title,
        string body,
        string color,
        string size = "m"
    )
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
    public static string FormatTime(
        int ticks,
        PlayerTimer.TimeFormatStyle style = PlayerTimer.TimeFormatStyle.Compact
    )
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
    /// Build the timer module with appropriate prefix based on mode
    /// </summary>
    /// <returns>string timerModule</returns>
    internal string BuildTimerWithPrefix()
    {
        // Timer Module
        string timerColor = TimerColor;

        if (_player.Timer.IsRunning)
        {
            if (_player.Timer.IsPracticeMode)
                timerColor = TimerColorPractice;
            else
                timerColor = TimerColorActive;
        }

        string prefix = "";

        if (_player.Timer.IsPracticeMode)
            prefix += "[P] ";

        if (_player.Timer.IsBonusMode)
            prefix += $"[B{_player.Timer.Bonus}] ";
        else if (_player.Timer.IsStageMode)
            prefix += $"[S{_player.Timer.Stage}] ";

        string timerModule = FormatHUDElementHTML(
            "",
            prefix + FormatTime(_player.Timer.Ticks),
            timerColor
        );

        return timerModule;
    }

    /// <summary>
    /// Build the velocity module
    /// </summary>
    /// <returns>string velocityModule</returns>
    internal string BuildVelocityModule()
    {
        float velocity = Extensions.GetVelocityFromController(_player.Controller);
        string velocityModule =
            FormatHUDElementHTML(
                "Speed",
                velocity.ToString("0"),
                Extensions.GetSpeedColorGradient(velocity)
            ) + " u/s";
        return velocityModule;
    }

    /// <summary>
    /// Build the rank module with appropriate values based on mode
    /// </summary>
    /// <returns>string rankModule</returns>
    internal string BuildRankModule()
    {
        int style = _player.Timer.Style;

        // Rank Module
        string rankModule = FormatHUDElementHTML("Rank", $"N/A", RankColorPb);
        if (_player.Timer.IsBonusMode)
        {
            if (
                _player.Stats.BonusPB[_player.Timer.Bonus][style].ID != -1
                && SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].ID != -1
            )
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"{_player.Stats.BonusPB[_player.Timer.Bonus][style].Rank}/{SurfTimer.CurrentMap.BonusCompletions[_player.Timer.Bonus][style]}",
                    RankColorPb
                );
            else if (SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].ID != -1)
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"-/{SurfTimer.CurrentMap.BonusCompletions[_player.Timer.Bonus][style]}",
                    RankColorPb
                );
        }
        else if (_player.Timer.IsStageMode)
        {
            if (
                _player.Stats.StagePB[_player.Timer.Stage][style].ID != -1
                && SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].ID != -1
            )
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"{_player.Stats.StagePB[_player.Timer.Stage][style].Rank}/{SurfTimer.CurrentMap.StageCompletions[_player.Timer.Stage][style]}",
                    RankColorPb
                );
            else if (SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].ID != -1)
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"-/{SurfTimer.CurrentMap.StageCompletions[_player.Timer.Stage][style]}",
                    RankColorPb
                );
        }
        else
        {
            if (_player.Stats.PB[style].ID != -1 && SurfTimer.CurrentMap.WR[style].ID != -1)
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"{_player.Stats.PB[style].Rank}/{SurfTimer.CurrentMap.MapCompletions[style]}",
                    RankColorPb
                );
            else if (SurfTimer.CurrentMap.WR[style].ID != -1)
                rankModule = FormatHUDElementHTML(
                    "Rank",
                    $"-/{SurfTimer.CurrentMap.MapCompletions[style]}",
                    RankColorPb
                );
        }

        return rankModule;
    }

    /// <summary>
    /// Build the PB module with appropriate values based on mode
    /// </summary>
    /// <returns>string pbModule</returns>
    internal string BuildPbModule()
    {
        int style = _player.Timer.Style;

        // PB & WR Modules
        string pbModule = FormatHUDElementHTML(
            "PB",
            _player.Stats.PB[style].RunTime > 0
                ? FormatTime(_player.Stats.PB[style].RunTime)
                : "N/A",
            RankColorPb
        );

        if (_player.Timer.Bonus > 0 && _player.Timer.IsBonusMode) // Show corresponding bonus values
        {
            pbModule = FormatHUDElementHTML(
                "PB",
                _player.Stats.BonusPB[_player.Timer.Bonus][style].RunTime > 0
                    ? FormatTime(_player.Stats.BonusPB[_player.Timer.Bonus][style].RunTime)
                    : "N/A",
                RankColorPb
            );
        }
        else if (_player.Timer.IsStageMode) // Show corresponding stage values
        {
            pbModule = FormatHUDElementHTML(
                "PB",
                _player.Stats.StagePB[_player.Timer.Stage][style].RunTime > 0
                    ? FormatTime(_player.Stats.StagePB[_player.Timer.Stage][style].RunTime)
                    : "N/A",
                RankColorPb
            );
        }

        return pbModule;
    }

    /// <summary>
    /// Build the WR module with appropriate values based on mode
    /// </summary>
    /// <returns>string wrModule</returns>
    internal string BuildWrModule()
    {
        int style = _player.Timer.Style;

        // WR Module
        string wrModule = FormatHUDElementHTML(
            "WR",
            SurfTimer.CurrentMap.WR[style].RunTime > 0
                ? FormatTime(SurfTimer.CurrentMap.WR[style].RunTime)
                : "N/A",
            RankColorWr
        );

        if (_player.Timer.Bonus > 0 && _player.Timer.IsBonusMode) // Show corresponding bonus values
        {
            wrModule = FormatHUDElementHTML(
                "WR",
                SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].RunTime > 0
                    ? FormatTime(SurfTimer.CurrentMap.BonusWR[_player.Timer.Bonus][style].RunTime)
                    : "N/A",
                RankColorWr
            );
        }
        else if (_player.Timer.IsStageMode) // Show corresponding stage values
        {
            wrModule = FormatHUDElementHTML(
                "WR",
                SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].RunTime > 0
                    ? FormatTime(SurfTimer.CurrentMap.StageWR[_player.Timer.Stage][style].RunTime)
                    : "N/A",
                RankColorWr
            );
        }

        return wrModule;
    }

    /// <summary>
    /// Displays the Center HUD for the client
    /// </summary>
    internal void Display()
    {
        if (!_player.Controller.IsValid)
            return;

        if (_player.Controller.PawnIsAlive)
        {
            string timerModule = BuildTimerWithPrefix();

            // Velocity Module
            string velocityModule = BuildVelocityModule();

            // Rank Module
            string rankModule = BuildRankModule();

            // PB & WR Modules
            string pbModule = BuildPbModule();
            string wrModule = BuildWrModule();

            // Build HUD
            string hud =
                $"{timerModule}<br>{velocityModule}<br>{pbModule} | {rankModule}<br>{wrModule}";

            // Display HUD
            _player.Controller.PrintToCenterHtml(hud);
        }
        else if (_player.Controller.Team == CsTeam.Spectator)
        {
            DisplaySpectatorHud();
        }
    }

    /// <summary>
    /// Displays the Spectator HUD for the client if they are spectating a replay
    /// </summary>
    internal void DisplaySpectatorHud()
    {
        ReplayPlayer? specReplay;
        string hud = string.Empty;

        if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.MapWR.Controller!))
        {
            specReplay = SurfTimer.CurrentMap.ReplayManager.MapWR;
            hud = BuildMapWrModule(specReplay);
        }
        else if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.StageWR?.Controller!))
        {
            specReplay = SurfTimer.CurrentMap.ReplayManager.StageWR!;
            hud = BuildStageWrModule(specReplay);
        }
        else if (_player.IsSpectating(SurfTimer.CurrentMap.ReplayManager.BonusWR?.Controller!))
        {
            specReplay = SurfTimer.CurrentMap.ReplayManager.BonusWR!;
            hud = BuildBonusWrModule(specReplay);
        }
        else
        {
            specReplay = SurfTimer.CurrentMap.ReplayManager.CustomReplays.Find(x =>
                _player.IsSpectating(x.Controller!)
            );
            if (specReplay != null)
                hud = BuildCustomReplayModule(specReplay);
        }

        if (!string.IsNullOrEmpty(hud))
        {
            _player.Controller.PrintToCenterHtml(hud);
        }
    }

    /// <summary>
    /// Build the Map WR module for the spectator HUD
    /// </summary>
    /// <param name="specReplay">Replay data to use</param>
    private string BuildMapWrModule(ReplayPlayer specReplay)
    {
        float velocity = Extensions.GetVelocityFromController(specReplay.Controller!);
        string timerColor = specReplay.ReplayCurrentRunTime > 0 ? TimerColorActive : RankColorWr;

        string replayModule = FormatHUDElementHTML("", "Map WR Replay", SpectatorColor, "m");
        string nameModule = FormatHUDElementHTML("", $"{specReplay.RecordPlayerName}", RankColorWr);
        string timeModule = FormatHUDElementHTML(
            "",
            $"{FormatTime(specReplay.ReplayCurrentRunTime)} / {FormatTime(specReplay.RecordRunTime)}",
            timerColor
        );
        string velocityModule =
            FormatHUDElementHTML(
                "Speed",
                velocity.ToString("0"),
                Extensions.GetSpeedColorGradient(velocity)
            ) + " u/s";
        string cycleModule = FormatHUDElementHTML(
            "Cycle",
            $"{specReplay.RepeatCount}",
            SpectatorColor,
            "s"
        );

        return $"{replayModule}<br>{nameModule}<br>{timeModule}<br>{velocityModule}<br>{cycleModule}";
    }

    /// <summary>
    /// Build the Stage WR module for the spectator HUD
    /// </summary>
    /// <param name="specReplay">Replay data to use</param>
    private string BuildStageWrModule(ReplayPlayer specReplay)
    {
        float velocity = Extensions.GetVelocityFromController(specReplay.Controller!);
        string timerColor = specReplay.ReplayCurrentRunTime > 0 ? TimerColorActive : RankColorWr;

        string replayModule = FormatHUDElementHTML(
            "",
            $"Stage {specReplay.Stage} WR Replay",
            SpectatorColor,
            "m"
        );
        string nameModule = FormatHUDElementHTML("", $"{specReplay.RecordPlayerName}", RankColorWr);
        string timeModule = FormatHUDElementHTML(
            "",
            $"{FormatTime(specReplay.ReplayCurrentRunTime)} / {FormatTime(specReplay.RecordRunTime)}",
            timerColor
        );
        string velocityModule =
            FormatHUDElementHTML(
                "Speed",
                velocity.ToString("0"),
                Extensions.GetSpeedColorGradient(velocity)
            ) + " u/s";
        string cycleModule = FormatHUDElementHTML(
            "Cycle",
            $"{specReplay.RepeatCount}",
            SpectatorColor,
            "s"
        );

        return $"{replayModule}<br>{nameModule}<br>{timeModule}<br>{velocityModule}<br>{cycleModule}";
    }

    /// <summary>
    /// Build the Bonus WR module for the spectator HUD
    /// </summary>
    /// <param name="specReplay">Replay data to use<</param>
    private string BuildBonusWrModule(ReplayPlayer specReplay)
    {
        float velocity = Extensions.GetVelocityFromController(specReplay.Controller!);
        string timerColor = specReplay.ReplayCurrentRunTime > 0 ? TimerColorActive : RankColorWr;

        string replayModule = FormatHUDElementHTML(
            "",
            $"Bonus {specReplay.Stage} WR Replay",
            SpectatorColor,
            "m"
        );
        string nameModule = FormatHUDElementHTML("", $"{specReplay.RecordPlayerName}", RankColorWr);
        string timeModule = FormatHUDElementHTML(
            "",
            $"{FormatTime(specReplay.ReplayCurrentRunTime)} / {FormatTime(specReplay.RecordRunTime)}",
            timerColor
        );
        string velocityModule =
            FormatHUDElementHTML(
                "Speed",
                velocity.ToString("0"),
                Extensions.GetSpeedColorGradient(velocity)
            ) + " u/s";
        string cycleModule = FormatHUDElementHTML(
            "Cycle",
            $"{specReplay.RepeatCount}",
            SpectatorColor,
            "s"
        );

        return $"{replayModule}<br>{nameModule}<br>{timeModule}<br>{velocityModule}<br>{cycleModule}";
    }

    /// <summary>
    /// Build the Custom Replay module for the spectator HUD
    /// </summary>
    /// <param name="specReplay">Replay data to use<</param>
    private string BuildCustomReplayModule(ReplayPlayer specReplay)
    {
        float velocity = Extensions.GetVelocityFromController(specReplay.Controller!);
        string timerColor = specReplay.ReplayCurrentRunTime > 0 ? TimerColorActive : RankColorWr;

        string replayType;
        switch (specReplay.Type)
        {
            case 0:
                replayType = "Map PB Replay";
                break;
            case 1:
                replayType = $"Bonus {specReplay.Stage} PB Replay";
                break;
            case 2:
                replayType = $"Stage {specReplay.Stage} PB Replay";
                break;
            default:
                return ""; // Invalid type
        }

        string replayModule = FormatHUDElementHTML("", replayType, SpectatorColor, "m");
        string nameModule = FormatHUDElementHTML("", $"{specReplay.RecordPlayerName}", RankColorWr);
        string timeModule = FormatHUDElementHTML(
            "",
            $"{FormatTime(specReplay.ReplayCurrentRunTime)} / {FormatTime(specReplay.RecordRunTime)}",
            timerColor
        );
        string velocityModule =
            FormatHUDElementHTML(
                "Speed",
                velocity.ToString("0"),
                Extensions.GetSpeedColorGradient(velocity)
            ) + " u/s";
        string cycleModule = FormatHUDElementHTML(
            "Cycle",
            $"{specReplay.RepeatCount}",
            SpectatorColor,
            "s"
        );

        return $"{replayModule}<br>{nameModule}<br>{timeModule}<br>{velocityModule}<br>{cycleModule}";
    }

    /// <summary>
    /// Displays checkpoints comparison messages in player chat.
    /// Only calculates if the player has a PB, otherwise it will display N/A
    /// </summary>
    internal void DisplayCheckpointMessages()
    {
        int pbTime;
        int wrTime = -1;
        float pbSpeed;
        float wrSpeed = -1.0f;
        int style = _player.Timer.Style;
        int playerCurrentCheckpoint = _player.Timer.Checkpoint;
        int currentTime = _player.Timer.Ticks;
        float currentSpeed = Extensions.GetVelocityFromController(_player.Controller!);

        // Default values for the PB and WR differences in case no calculations can be made
        string strPbDifference =
            $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";
        string strWrDifference =
            $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";

        // Get PB checkpoint data if available
        if (_player.Stats.PB[style].Checkpoints != null)
        {
            pbTime = _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].RunTime;
            pbSpeed = (float)
                Math.Sqrt(
                    _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelX
                        * _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelX
                        + _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelY
                            * _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelY
                        + _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelZ
                            * _player.Stats.PB[style].Checkpoints![playerCurrentCheckpoint].StartVelZ
                );
        }
        else
        {
            // We assign default values to pbTime and pbSpeed
            pbTime = -1; // This determines if we will calculate differences or not!!!
            pbSpeed = 0.0f;
        }

        // Calculate differences in PB (PB - Current)
        if (pbTime != -1)
        {
#if DEBUG
            Console.WriteLine(
                $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> Starting PB difference calculation... (pbTime != -1)"
            );
#endif
            // Reset the string
            strPbDifference = string.Empty;

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
                strPbDifference +=
                    "(" + ChatColors.Green + "+" + ((pbSpeed - currentSpeed) * -1).ToString("0"); // We multiply by -1 to get the positive value
            }
            else if (pbSpeed - currentSpeed > 0.0)
            {
                strPbDifference +=
                    "(" + ChatColors.Red + "-" + (pbSpeed - currentSpeed).ToString("0");
            }
            strPbDifference += ChatColors.Default + ")";
        }

        if (SurfTimer.CurrentMap.WR[style].RunTime > 0)
        {
            // Calculate differences in WR (WR - Current)
#if DEBUG
            Console.WriteLine(
                $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> Starting WR difference calculation... (SurfTimer.CurrentMap.WR[{style}].Ticks > 0)"
            );
#endif

            wrTime = SurfTimer.CurrentMap.WR[style].Checkpoints![playerCurrentCheckpoint].RunTime;
            wrSpeed = (float)
                Math.Sqrt(
                    SurfTimer.CurrentMap.WR[style].Checkpoints![playerCurrentCheckpoint].StartVelX
                        * SurfTimer.CurrentMap.WR[style].Checkpoints![playerCurrentCheckpoint].StartVelX
                        + SurfTimer.CurrentMap.WR[style].Checkpoints![playerCurrentCheckpoint].StartVelY
                            * SurfTimer
                                .CurrentMap
                                .WR[style]
                                .Checkpoints![playerCurrentCheckpoint]
                                .StartVelY
                        + SurfTimer.CurrentMap.WR[style].Checkpoints![playerCurrentCheckpoint].StartVelZ
                            * SurfTimer
                                .CurrentMap
                                .WR[style]
                                .Checkpoints![playerCurrentCheckpoint]
                                .StartVelZ
                );
            // Reset the string
            strWrDifference = string.Empty;

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
                strWrDifference +=
                    "(" + ChatColors.Green + "+" + ((wrSpeed - currentSpeed) * -1).ToString("0"); // We multiply by -1 to get the positive value
            }
            else if (wrSpeed - currentSpeed > 0.0)
            {
                strWrDifference +=
                    "(" + ChatColors.Red + "-" + (wrSpeed - currentSpeed).ToString("0");
            }
            strWrDifference += ChatColors.Default + ")";
        }

        // Print checkpoint message
        _player.Controller.PrintToChat(
            $"{Config.PluginPrefix} {LocalizationService.LocalizerNonNull["checkpoint_message",
            playerCurrentCheckpoint, FormatTime(_player.Timer.Ticks), currentSpeed.ToString("0"), strPbDifference, strWrDifference]}"
        );

#if DEBUG
        Console.WriteLine(
            $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  PB: {pbTime} - CURR: {currentTime} = pbTime: {pbTime - currentTime}"
        );
        Console.WriteLine(
            $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] PB: {pbSpeed} - CURR: {currentSpeed} = difference: {pbSpeed - currentSpeed}"
        );
        Console.WriteLine(
            $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  WR: {wrTime} - CURR: {currentTime} = difference: {wrTime - currentTime}"
        );
        Console.WriteLine(
            $"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] WR: {wrSpeed} - CURR: {currentSpeed} = difference: {wrSpeed - currentSpeed}"
        );
#endif
    }
}
