using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;


namespace SurfTimer;

unsafe static class Extensions
{
    public static void Teleport(this CBaseEntity entity, VectorT? position = null, QAngleT? angles = null, VectorT? velocity = null)
    {
        Guard.IsValidEntity(entity);

        void* pPos = null, pAng = null, pVel = null;

        // Structs are stored on the stack, GC should not break pointers.

        if (position.HasValue)
        {
            var pos = position.Value; // Remove nullable wrapper
            pPos = &pos;
        }

        if (angles.HasValue)
        {
            var ang = angles.Value;
            pAng = &ang;
        }

        if (velocity.HasValue)
        {
            var vel = velocity.Value;
            pVel = &vel;
        }

        VirtualFunction.CreateVoid<IntPtr, IntPtr, IntPtr, IntPtr>(entity.Handle, GameData.GetOffset("CBaseEntity_Teleport"))(entity.Handle, (nint)pPos,
            (nint)pAng, (nint)pVel);
    }

    public static (VectorT fwd, VectorT right, VectorT up) AngleVectors(this QAngle vec) => vec.ToQAngle_t().AngleVectors();
    public static void AngleVectors(this QAngle vec, out VectorT fwd, out VectorT right, out VectorT up) => vec.ToQAngle_t().AngleVectors(out fwd, out right, out up);

    public static VectorT ToVector_t(this Vector vec) => new(vec.Handle);
    public static QAngleT ToQAngle_t(this QAngle vec) => new(vec.Handle);

    public static void SetCollisionGroup(this CCSPlayerController controller, CollisionGroup collisionGroup)
    {
        if (!controller.IsValid || controller.Collision == null) return;
        controller.Collision.CollisionAttribute.CollisionGroup = (byte)collisionGroup;
        controller.Collision.CollisionGroup = (byte)collisionGroup;

        Utilities.SetStateChanged(controller, "CColisionProperity", "m_collisionGroup");
        Utilities.SetStateChanged(controller, "CCollisionProperty", "m_collisionAttribute");
    }

    /// <summary>
    /// Asssigns a ChatColor to the given Tier value
    /// </summary>
    /// <param name="tier">Map Tier up to 8</param>
    /// <returns>Appropriate ChatColor value for the Tier</returns>
    public static char GetTierColor(short tier)
    {
        return tier switch
        {
            1 => ChatColors.Green,
            2 => ChatColors.Lime,
            3 => ChatColors.Yellow,
            4 => ChatColors.Orange,
            5 => ChatColors.LightRed,
            6 => ChatColors.DarkRed,
            7 => ChatColors.LightPurple,
            8 => ChatColors.Purple,
            _ => ChatColors.White
        };
    }
}