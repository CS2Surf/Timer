using CounterStrikeSharp.API.Core;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SurfTimer;

public struct QAngleT : IAdditionOperators<QAngleT, QAngleT, QAngleT>,
        ISubtractionOperators<QAngleT, QAngleT, QAngleT>,
        IMultiplyOperators<QAngleT, float, QAngleT>,
        IDivisionOperators<QAngleT, float, QAngleT>
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
                Exception ex = new IndexOutOfRangeException($"Index {i} is out of range for QAngleT. Valid range is 0 to {SIZE}.");
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
                Exception ex = new IndexOutOfRangeException($"Index {i} is out of range for QAngleT. Valid range is 0 to {SIZE}.");
                throw ex;
            }

            fixed (void* ptr = &this)
            {
                Unsafe.Write(Unsafe.Add<float>(ptr, i), value);
            }
        }
    }

    public QAngleT()
    {
    }

    public unsafe QAngleT(nint ptr) : this(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<float>((void*)ptr), SIZE))
    {
    }

    public QAngleT(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public QAngleT(ReadOnlySpan<float> values)
    {
        if (values.Length < SIZE)
        {
            throw new ArgumentOutOfRangeException(nameof(values));
        }

        this = Unsafe.ReadUnaligned<QAngleT>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
    }

    public unsafe (VectorT fwd, VectorT right, VectorT up) AngleVectors()
    {
        VectorT fwd = default, right = default, up = default;

        nint pFwd = (nint)Unsafe.AsPointer(ref fwd);
        nint pRight = (nint)Unsafe.AsPointer(ref right);
        nint pUp = (nint)Unsafe.AsPointer(ref up);

        fixed (void* ptr = &this)
        {
            NativeAPI.AngleVectors((nint)ptr, pFwd, pRight, pUp);
        }

        return (fwd, right, up);
    }

    public unsafe void AngleVectors(out VectorT fwd, out VectorT right, out VectorT up)
    {
        fixed (void* ptr = &this, pFwd = &fwd, pRight = &right, pUp = &up)
        {
            NativeAPI.AngleVectors((nint)ptr, (nint)pFwd, (nint)pRight, (nint)pUp);
        }
    }

    public readonly override string ToString()
    {
        return $"{X:n2} {Y:n2} {Z:n2}";
    }

    public static QAngleT operator +(QAngleT a, QAngleT b)
    {
        return new QAngleT(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    public static QAngleT operator -(QAngleT a, QAngleT b)
    {
        return new QAngleT(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    public static QAngleT operator -(QAngleT a)
    {
        return new QAngleT(-a.X, -a.Y, -a.Z);
    }

    public static QAngleT operator *(QAngleT a, float b)
    {
        return new QAngleT(a.X * b, a.Y * b, a.Z * b);
    }

    public static QAngleT operator /(QAngleT a, float b)
    {
        return new QAngleT(a.X / b, a.Y / b, a.Z / b);
    }
}