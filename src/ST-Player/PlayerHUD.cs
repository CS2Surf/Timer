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
    public string FormatTime(int ticks, PlayerTimer.TimeFormatStyle style = PlayerTimer.TimeFormatStyle.Compact)
    {
        TimeSpan time = TimeSpan.FromSeconds(ticks / 64.0);
        int millis = (int)(ticks % 64 * (1000.0 / 64.0));

        switch (style)
        {
            case PlayerTimer.TimeFormatStyle.Compact:
                return time.TotalMinutes < 1
                    ? $"{time.Seconds:D1}.{millis:D3}"
                    : $"{time.Minutes:D1}:{time.Seconds:D1}.{millis:D3}";
            case PlayerTimer.TimeFormatStyle.Full:
                return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{millis:D3}";
            case PlayerTimer.TimeFormatStyle.Verbose:
                return $"{time.Hours}h {time.Minutes}m {time.Seconds}s {millis}ms";
            default:
                throw new ArgumentException("Invalid time format style");
        }
    }

    public void Display() // To-do: make Style (currently 0) be dynamic
    {
        if (_player.Controller.IsValid && _player.Controller.PawnIsAlive)
        {
            // Timer Module
            string timerColor = "#79d1ed";
            if (_player.Timer.IsRunning)
            {
                if (_player.Timer.IsPracticeMode)
                    timerColor = "#F2C94C";
                else
                    timerColor = "#2E9F65";
            }
            string timerModule = FormatHUDElementHTML("", FormatTime(_player.Timer.Ticks), timerColor);

            // Velocity Module - To-do: Make velocity module configurable (XY or XYZ velocity)
            float velocity = (float)Math.Sqrt(_player.Controller.PlayerPawn.Value!.AbsVelocity.X * _player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                                + _player.Controller.PlayerPawn.Value!.AbsVelocity.Y * _player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                                + _player.Controller.PlayerPawn.Value!.AbsVelocity.Z * _player.Controller.PlayerPawn.Value!.AbsVelocity.Z);
            string velocityModule = FormatHUDElementHTML("Speed", velocity.ToString("0"), "#79d1ed") + " u/s";
            // Rank Module
            string rankModule = FormatHUDElementHTML("Rank", $"N/A", "#7882dd");
            if (_player.Stats.PB[0].Ticks > 0 && _player.CurrMap.WR[0].Ticks > 0)
            {
                rankModule = FormatHUDElementHTML("Rank", $"{_player.Stats.PB[0].Rank}/{_player.CurrMap.TotalCompletions}", "#7882dd");
            }
            else if (_player.CurrMap.WR[0].Ticks >= 0)
            {
                rankModule = FormatHUDElementHTML("Rank", $"N/A/{_player.CurrMap.TotalCompletions}", "#7882dd");
            }
            // PB & WR Modules
            string pbModule = FormatHUDElementHTML("PB", _player.Stats.PB[0].Ticks > 0 ? FormatTime(_player.Stats.PB[0].Ticks) : "N/A", "#7882dd"); // IMPLEMENT IN PlayerStats // To-do: make Style (currently 0) be dynamic
            string wrModule = FormatHUDElementHTML("WR", _player.CurrMap.WR[0].Ticks > 0 ? FormatTime(_player.CurrMap.WR[0].Ticks) : "N/A", "#ffc61a"); // IMPLEMENT IN PlayerStats - This should be part of CurrentMap, not PlayerStats?

            // Build HUD
            string hud = $"{timerModule}<br>{velocityModule}<br>{pbModule} | {rankModule}<br>{wrModule}";

            // Display HUD
            _player.Controller.PrintToCenterHtml(hud);
        }
    }

    /// <summary>
    /// Only calculates if the player has a PB, otherwise it will display N/A
    /// </summary>
    /// <param name="PluginPrefix"></param>
    public void DisplayCheckpointMessages(string PluginPrefix) // To-do: PluginPrefix should be accessible in here without passing it as a parameter
    {
        int pbTime;
        int wrTime = -1;
        float pbSpeed;
        float wrSpeed = -1.0f;

        int currentTime = _player.Timer.Ticks;
        float currentSpeed = (float)Math.Sqrt(_player.Controller.PlayerPawn.Value!.AbsVelocity.X * _player.Controller.PlayerPawn.Value!.AbsVelocity.X
                                        + _player.Controller.PlayerPawn.Value!.AbsVelocity.Y * _player.Controller.PlayerPawn.Value!.AbsVelocity.Y
                                        + _player.Controller.PlayerPawn.Value!.AbsVelocity.Z * _player.Controller.PlayerPawn.Value!.AbsVelocity.Z);

        // Default values for the PB and WR differences in case no calculations can be made
        string strPbDifference = $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";
        string strWrDifference = $"{ChatColors.Grey}N/A{ChatColors.Default} ({ChatColors.Grey}N/A{ChatColors.Default})";

        // We need to try/catch this because the player might not have a PB for this stage in this case but they will not have for the map as well
        // Can check checkpoints count instead of try/catch
        try
        {
            pbTime = _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].Ticks;
            pbSpeed = (float)Math.Sqrt(_player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelX * _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelX
                                        + _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelY * _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelY
                                        + _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelZ * _player.Stats.PB[0].Checkpoint[_player.Timer.Checkpoint].StartVelZ);
            
            #if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  Got pbTime from _player.Stats.PB[0].Checkpoint[{_player.Timer.Checkpoint} = {pbTime}]");
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] Got pbSpeed from _player.Stats.PB[0].Checkpoint[{_player.Timer.Checkpoint}] = {pbSpeed}");
            #endif
        }
        #if DEBUG
        catch (System.Exception ex)
        #else
        catch (System.Exception)
        #endif
        {
            // Handle the exception gracefully without stopping the application
            // We assign default values to pbTime and pbSpeed
            pbTime = -1; // This determines if we will calculate differences or not!!!
            pbSpeed = 0.0f;
            
            #if DEBUG
            Console.WriteLine($"CS2 Surf CAUGHT EXCEPTION >> DisplayCheckpointMessages -> An error occurred: {ex.Message}");
            Console.WriteLine($"CS2 Surf CAUGHT EXCEPTION >> DisplayCheckpointMessages -> An error occurred Player has no PB and therefore no Checkpoints | _player.Stats.PB[0].Checkpoint.Count = {_player.Stats.PB[0].Checkpoint.Count}");
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
                strPbDifference += ChatColors.Red + "+" + _player.HUD.FormatTime((pbTime - currentTime) * -1); // We multiply by -1 to get the positive value
            }
            else if (pbTime - currentTime >= 0.0)
            {
                strPbDifference += ChatColors.Green + "-" + _player.HUD.FormatTime(pbTime - currentTime);
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

        if (_player.CurrMap.WR[0].Ticks > 0) // To-do: make Style (currently 0) be dynamic
        {
            // Calculate differences in WR (WR - Current)
            #if DEBUG
            Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> Starting WR difference calculation... (_player.CurrMap.WR[0].Ticks > 0)");
            #endif

            wrTime = _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].Ticks;
            wrSpeed = (float)Math.Sqrt(_player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelX * _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelX
                                        + _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelY * _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelY
                                        + _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelZ * _player.CurrMap.WR[0].Checkpoint[_player.Timer.Checkpoint].StartVelZ);
            // Reset the string
            strWrDifference = "";

            // Calculate the WR time difference
            if (wrTime - currentTime < 0.0)
            {
                strWrDifference += ChatColors.Red + "+" + _player.HUD.FormatTime((wrTime - currentTime) * -1); // We multiply by -1 to get the positive value
            }
            else if (wrTime - currentTime >= 0.0)
            {
                strWrDifference += ChatColors.Green + "-" + _player.HUD.FormatTime(wrTime - currentTime);
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
        _player.Controller.PrintToChat(
            $"{PluginPrefix} CP [{ChatColors.Yellow}{_player.Timer.Checkpoint}{ChatColors.Default}]: " +
            $"{ChatColors.Yellow}{_player.HUD.FormatTime(_player.Timer.Ticks)}{ChatColors.Default} " +
            $"{ChatColors.Yellow}({currentSpeed.ToString("0")}){ChatColors.Default} " +
            $"[PB: {strPbDifference} | " +
            $"WR: {strWrDifference}]");

        #if DEBUG
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  PB: {pbTime} - CURR: {currentTime} = pbTime: {pbTime - currentTime}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] PB: {pbSpeed} - CURR: {currentSpeed} = difference: {pbSpeed - currentSpeed}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [TIME]  WR: {wrTime} - CURR: {currentTime} = difference: {wrTime - currentTime}");
        Console.WriteLine($"CS2 Surf DEBUG >> DisplayCheckpointMessages -> [SPEED] WR: {wrSpeed} - CURR: {currentSpeed} = difference: {wrSpeed - currentSpeed}");
        #endif 
    }
}
