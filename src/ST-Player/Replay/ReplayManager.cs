using CounterStrikeSharp.API.Core;

namespace SurfTimer;

public class ReplayManager
{
    public ReplayPlayer MapWR { get; set; }
    public ReplayPlayer? BonusWR { get; set; } = null;
    public ReplayPlayer? StageWR { get; set; } = null;
    /// <summary>
    /// Contains all Stage records for all styles - Refer to as AllStageWR[stage#][style]
    /// </summary>
    public Dictionary<int, ReplayPlayer>[] AllStageWR { get; set; } = Array.Empty<Dictionary<int, ReplayPlayer>>();
    /// <summary>
    /// Contains all Bonus records for all styles - Refer to as AllBonusWR[bonus#][style]
    /// </summary>
    public Dictionary<int, ReplayPlayer>[] AllBonusWR { get; set; } = Array.Empty<Dictionary<int, ReplayPlayer>>();
    public List<ReplayPlayer> CustomReplays { get; set; }


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
            this.AllStageWR = new Dictionary<int, ReplayPlayer>[SurfTimer.CurrentMap.Stages + 1];

            for (int i = 1; i <= SurfTimer.CurrentMap.Stages; i++)
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
            this.AllBonusWR = new Dictionary<int, ReplayPlayer>[SurfTimer.CurrentMap.Bonuses + 1];

            for (int i = 1; i <= SurfTimer.CurrentMap.Bonuses; i++)
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