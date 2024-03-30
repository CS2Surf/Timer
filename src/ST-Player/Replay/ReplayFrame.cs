namespace SurfTimer;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;
using System.Runtime.CompilerServices;

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
        public float[] pos { get; set; } = { 0, 0 ,0 };
        public float[] ang { get; set; } = { 0, 0 ,0 };
        public byte Situation { get; set; } = (byte)ReplayFrameSituation.NONE;
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
