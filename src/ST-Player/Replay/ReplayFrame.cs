namespace SurfTimer;
using CounterStrikeSharp.API.Modules.Utils;

internal enum ReplayFrameSituation
{
        NONE,

        STAGE_ZONE_ENTER,
        STAGE_ZONE_EXIT,

        START_ZONE_ENTER,
        START_ZONE_EXIT,

        END_ZONE_ENTER,
        END_ZONE_EXIT,

        CHECKPOINT_ZONE_ENTER,
        CHECKPOINT_ZONE_EXIT,

        // START_RUN,
        // END_RUN,
        // TOUCH_CHECKPOINT,
        // START_STAGE,
        // END_STAGE,
        // ENTER_STAGE,
}

[Serializable]
internal class ReplayFrame
{
        public float[] pos { get; set; } = { 0, 0, 0 };
        public float[] ang { get; set; } = { 0, 0, 0 };
        public ReplayFrameSituation Situation { get; set; } = ReplayFrameSituation.NONE;
        public uint Flags { get; set; }

        public Vector GetPos()
        {
                return new Vector(this.pos[0], this.pos[1], this.pos[2]);
        }

        public QAngle GetAng()
        {
                return new QAngle(this.ang[0], this.ang[1], this.ang[2]);
        }
}
