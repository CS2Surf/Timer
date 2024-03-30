using CounterStrikeSharp.API.Modules.Cvars;

namespace SurfTimer;

internal class ConVarHelper
{
    public static void RemoveCheatFlagFromConVar(string cv_name)
    {
        ConVar? cv = ConVar.Find(cv_name);
        if (cv == null || (cv.Flags & CounterStrikeSharp.API.ConVarFlags.FCVAR_CHEAT) == 0)
            return;

        cv.Flags &= ~CounterStrikeSharp.API.ConVarFlags.FCVAR_CHEAT;
    }
}