using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

[Cfg("/csgo/cfg/SurfTimer/config.json")]
public class PluginCfg
{

    public string Prefix {get; set;} = $"[{ChatColors.DarkBlue}CS2 Surf{ChatColors.Default}]";
    public bool IncludeZVelocity {get; set;} = true;

}