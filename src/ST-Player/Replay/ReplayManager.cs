using CounterStrikeSharp.API.Core;

namespace SurfTimer;

public class ReplayManager
{
    public ReplayPlayer MapWR { get; set; }
    public ReplayPlayer? BonusWR { get; set; } = null;
    public ReplayPlayer? StageWR { get; set; } = null;
    /// <summary>
    /// Contains all Stage records for all styles - Refer to as AllStageWR[stage#][style]
    /// Need to figure out a way to NOT hardcode to `32` but to total amount of Stages
    /// </summary>
    public Dictionary<int, ReplayPlayer>[] AllStageWR { get; set; } = new Dictionary<int, ReplayPlayer>[32];
    /// <summary>
    /// Contains all Bonus records for all styles - Refer to as AllBonusWR[bonus#][style]
    /// Need to figure out a way to NOT hardcode to `32` but to total amount of Bonuses
    /// </summary>
    public Dictionary<int, ReplayPlayer>[] AllBonusWR { get; set; } = new Dictionary<int, ReplayPlayer>[32];
    public List<ReplayPlayer> CustomReplays { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="map_id">ID of the map</param>
    /// <param name="staged">Does the map have Stages</param>
    /// <param name="bonused">Does the map have Bonuses</param>
    /// <param name="frames">Frames for the replay</param>
    /// <param name="run_time">Run time (Ticks) for the run</param>
    /// <param name="playerName">Name of the player</param>
    /// <param name="map_time_id">ID of the run</param>
    /// <param name="style">Style of the run</param>
    /// <param name="stage">Stage/Bonus of the run</param>
    internal ReplayManager(int map_id, bool staged, bool bonused, List<ReplayFrame> frames, int run_time = 0, string playerName = "", int map_time_id = -1, int style = 0, int stage = 0)
    {
        MapWR = new ReplayPlayer
        {
            Type = 0,
            Stage = 0,
            RecordRank = 1,
            MapID = map_id,
            Frames = frames,
            RecordRunTime = run_time,
            RecordPlayerName = playerName,
            MapTimeID = map_time_id
        };

        if (staged)
        {
            // Initialize 32 Stages for each style
            // TODO: Make the amount of stages dynamic
            for (int i = 0; i < 32; i++)
            {
                AllStageWR[i] = new Dictionary<int, ReplayPlayer>();
                foreach (int x in Config.Styles)
                {
                    AllStageWR[i][x] = new ReplayPlayer();
                }
            }
            StageWR = new ReplayPlayer();
        }

        if (bonused)
        {
            // Initialize 32 Stages for each style
            // TODO: Make the amount of bonuses dynamic
            for (int i = 0; i < 32; i++)
            {
                AllBonusWR[i] = new Dictionary<int, ReplayPlayer>();
                foreach (int x in Config.Styles)
                {
                    AllBonusWR[i][x] = new ReplayPlayer();
                }
            }
            BonusWR = new ReplayPlayer();
        }

        CustomReplays = new List<ReplayPlayer>();
    }

    public bool IsControllerConnectedToReplayPlayer(CCSPlayerController controller)
    {
        if (this.MapWR.Controller?.Equals(controller) == true)
            return true;

        if (this.StageWR?.Controller?.Equals(controller) == true)
            return true;

        if (this.BonusWR?.Controller?.Equals(controller) == true)
            return true;

        foreach (var replay in this.CustomReplays)
        {
            if (replay.Controller?.Equals(controller) == true)
                return true;
        }

        return false;
    }
}