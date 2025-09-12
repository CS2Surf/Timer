using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.CompilerServices;
using System.Text;

namespace SurfTimer;
public class SchemaString<SchemaClass> : NativeObject where SchemaClass : NativeObject
{
    public SchemaString(SchemaClass instance, string member)
        : base(Schema.GetSchemaValue<nint>(instance.Handle, typeof(SchemaClass).Name!, member))
    { }

    public unsafe void Set(string str)
    {
        byte[] bytes = this.GetStringBytes(str);

        for (int i = 0; i < bytes.Length; i++)
        {
            Unsafe.Write((void*)(this.Handle.ToInt64() + i), bytes[i]);
        }

        Unsafe.Write((void*)(this.Handle.ToInt64() + bytes.Length), 0);
    }

    private byte[] GetStringBytes(string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
}