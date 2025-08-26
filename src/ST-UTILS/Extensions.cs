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

    /// <summary>
    /// Color gradient for speed, based on a range of velocities.
    /// </summary>
    /// <param name="velocity">Velocity to determine color for</param>
    /// <param name="minSpeed">Minimum velocity</param>
    /// <param name="maxSpeed">Maximum velocity</param>
    /// <returns>HEX value as string</returns>
    public static string GetSpeedColorGradient(float velocity, float minSpeed = 240f, float maxSpeed = 4000f)
    {
        // Key colors (HEX -> RGB)
        (int R, int G, int B)[] gradient = new (int, int, int)[]
        {
            (79, 195, 247), // blue #4FC3F7
            (46, 159, 101), // green #2E9F65
            (255, 255, 0),  // yellow #FFFF00
            (255, 165, 0),  // orange #FFA500
            (255, 0, 0)     // red #FF0000
        };

        // Limit velocity
        velocity = Math.Clamp(velocity, minSpeed, maxSpeed);

        // Normalize velocity to 0..1
        float t = (velocity - minSpeed) / (maxSpeed - minSpeed);

        // Calculate which part of the gradient we are in
        float scaledT = t * (gradient.Length - 1);
        int index1 = (int)Math.Floor(scaledT);
        int index2 = Math.Min(index1 + 1, gradient.Length - 1);

        float localT = scaledT - index1;

        // Linear interpolation between the two color points
        int r = (int)(gradient[index1].R + (gradient[index2].R - gradient[index1].R) * localT);
        int g = (int)(gradient[index1].G + (gradient[index2].G - gradient[index1].G) * localT);
        int b = (int)(gradient[index1].B + (gradient[index2].B - gradient[index1].B) * localT);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Calculates the velocity of a given player controller
    /// </summary>
    /// <param name="controller">Controller to calculate velocity for</param>
    /// <returns>float velocity</returns>
    public static float GetVelocityFromController(CCSPlayerController controller)
    {
        var pawn = controller.PlayerPawn?.Value;
        if (pawn == null)
            return 0.0f;

        var vel = pawn.AbsVelocity;
        return (float)Math.Sqrt(vel.X * vel.X + vel.Y * vel.Y + vel.Z * vel.Z);
    }

}