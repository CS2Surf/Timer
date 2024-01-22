namespace SurfTimer;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;

internal enum ReplayFrameSituation
{
        NONE,
        START_RUN,
        END_RUN,
        TOUCH_CHECKPOINT,
        START_STAGE,
        END_STAGE
}

[Serializable]
internal class ReplayFrame 
{
        public Vector Pos { get; set; } = new Vector(0, 0, 0);
        public QAngle Ang { get; set; } = new QAngle(0, 0, 0);
        public uint Situation { get; set; } = (uint)ReplayFrameSituation.NONE;
        public ulong Button { get; set; }
        public uint Flags { get; set; }
        public MoveType_t MoveType { get; set; }
}
