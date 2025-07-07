using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SurfTimer;

struct Vector_t : IAdditionOperators<Vector_t, Vector_t, Vector_t>,
        ISubtractionOperators<Vector_t, Vector_t, Vector_t>,
        IMultiplyOperators<Vector_t, float, Vector_t>,
        IDivisionOperators<Vector_t, float, Vector_t>
{
    public float X, Y, Z;

    public const int SIZE = 3;

    public unsafe float this[int i]
    {
        readonly get
        {
            if (i < 0 || i > SIZE)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* ptr = &this)
            {
                return Unsafe.Read<float>(Unsafe.Add<float>(ptr, i));
            }
        }
        set
        {
            if (i < 0 || i > SIZE)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (void* ptr = &this)
            {
                Unsafe.Write(Unsafe.Add<float>(ptr, i), value);
            }
        }
    }

    public Vector_t()
    {
    }

    public unsafe Vector_t(nint ptr) : this(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<float>((void*)ptr), SIZE))
    {
    }

    public Vector_t(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector_t(ReadOnlySpan<float> values)
    {
        if (values.Length < SIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        this = Unsafe.ReadUnaligned<Vector_t>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
    }

    public readonly float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public readonly float Length2D()
    {
        return (float)Math.Sqrt(X * X + Y * Y);
    }

    public readonly float velMag()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z + Z );
    }

    public readonly bool IsZero(float tolerance = 0.0001f)
    {
        return Math.Abs(X) <= tolerance && Math.Abs(Y) <= tolerance && Math.Abs(Z) <= tolerance;
    }
    public void Scale(float scale)
    {
        X *= scale;
        Y *= scale;
        Z *= scale;
    }

    public readonly override string ToString()
    {
        return $"{X:n2} {Y:n2} {Z:n2}";
    }

    public static Vector_t operator +(Vector_t a, Vector_t b)
    {
        return new Vector_t(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static Vector_t operator -(Vector_t a, Vector_t b)
    {
        return new Vector_t(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static Vector_t operator -(Vector_t a)
    {
        return new Vector_t(-a.X, -a.Y, -a.Z);
    }

    public static Vector_t operator *(Vector_t a, float b)
    {
        return new Vector_t(a.X * b, a.Y * b, a.Z * b);
    }

    public static Vector_t operator /(Vector_t a, float b)
    {
        return new Vector_t(a.X / b, a.Y / b, a.Z / b);
    }
}