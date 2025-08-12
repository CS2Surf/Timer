using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SurfTimer;

public struct VectorT : IAdditionOperators<VectorT, VectorT, VectorT>,
        ISubtractionOperators<VectorT, VectorT, VectorT>,
        IMultiplyOperators<VectorT, float, VectorT>,
        IDivisionOperators<VectorT, float, VectorT>
{
    private float x, y, z;

    public float X
    {
        readonly get => x;
        set => x = value;
    }

    public float Y
    {
        readonly get => y;
        set => y = value;
    }

    public float Z
    {
        readonly get => z;
        set => z = value;
    }

    public const int SIZE = 3;

    public unsafe float this[int i]
    {
        readonly get
        {
            if (i < 0 || i > SIZE)
            {
                Exception ex = new IndexOutOfRangeException($"Index {i} is out of range for VectorT. Valid range is higher than {SIZE}.");
                throw ex;
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
                Exception ex = new IndexOutOfRangeException($"Index {i} is out of range for VectorT. Valid range is higher than {SIZE}.");
                throw ex;
            }

            fixed (void* ptr = &this)
            {
                Unsafe.Write(Unsafe.Add<float>(ptr, i), value);
            }
        }
    }

    public VectorT()
    {
    }

    public unsafe VectorT(nint ptr) : this(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<float>((void*)ptr), SIZE))
    {
    }

    public VectorT(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public VectorT(ReadOnlySpan<float> values)
    {
        if (values.Length < SIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        this = Unsafe.ReadUnaligned<VectorT>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
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
        return (float)Math.Sqrt(X * X + Y * Y + Z + Z);
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

    public static VectorT operator +(VectorT a, VectorT b)
    {
        return new VectorT(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static VectorT operator -(VectorT a, VectorT b)
    {
        return new VectorT(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static VectorT operator -(VectorT a)
    {
        return new VectorT(-a.X, -a.Y, -a.Z);
    }

    public static VectorT operator *(VectorT a, float b)
    {
        return new VectorT(a.X * b, a.Y * b, a.Z * b);
    }

    public static VectorT operator /(VectorT a, float b)
    {
        return new VectorT(a.X / b, a.Y / b, a.Z / b);
    }
}