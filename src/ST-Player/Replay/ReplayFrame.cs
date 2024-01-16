namespace SurfTimer;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;


[Serializable]
internal class ReplayFrame 
{
        public Vector Pos { get; set; } = new Vector(0, 0, 0);
        public QAngle Ang { get; set; } = new QAngle(0, 0, 0);
        public ulong Button { get; set; }
        public uint Flags { get; set; }
        public MoveType_t MoveType { get; set; }
}
