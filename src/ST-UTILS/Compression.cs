using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;

namespace SurfTimer;

internal class VectorConverter : JsonConverter<Vector>
{
    public override Vector Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Ensure that the reader is positioned at the start of an object
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object.");

        float x = 0, y = 0, z = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "X":
                        x = (float)reader.GetDouble();
                        break;
                    case "Y":
                        y = (float)reader.GetDouble();
                        break;
                    case "Z":
                        z = (float)reader.GetDouble();
                        break;
                }
            }
        }

        return new Vector { X = x, Y = y, Z = z };
    }

    public override void Write(Utf8JsonWriter writer, Vector value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}

internal class QAngleConverter : JsonConverter<QAngle>
{
    public override QAngle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Ensure that the reader is positioned at the start of an object
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object.");

        float X = 0, Y = 0, Z = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "X":
                        X = (float)reader.GetDouble();
                        break;
                    case "Y":
                        Y = (float)reader.GetDouble();
                        break;
                    case "Z":
                        Z = (float)reader.GetDouble();
                        break;
                }
            }
        }

        return new QAngle { X = X, Y = Y, Z = Z };
    }

    public override void Write(Utf8JsonWriter writer, QAngle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }
}

internal class Compressor
{
    public static string Decompress(string input)
    {
        byte[] compressed = Convert.FromBase64String(input);
        byte[] decompressed = Decompress(compressed);
        return Encoding.UTF8.GetString(decompressed);
    }

    public static string Compress(string input)
    {
        byte[] encoded = Encoding.UTF8.GetBytes(input);
        byte[] compressed = Compress(encoded);
        return Convert.ToBase64String(compressed);
    }

    public static byte[] Decompress(byte[] input)
    {
        using (var source = new MemoryStream(input))
        {
            byte[] lengthBytes = new byte[4];
            source.Read(lengthBytes, 0, 4);

            var length = BitConverter.ToInt32(lengthBytes, 0);
            using (var decompressionStream = new GZipStream(source,
                CompressionMode.Decompress))
            {
                var result = new byte[length];
                int totalRead = 0, bytesRead;
                while ((bytesRead = decompressionStream.Read(result, totalRead, length - totalRead)) > 0)
                {
                totalRead += bytesRead;
                }

                return result;
            }
        }
    }

    public static byte[] Compress(byte[] input) 
    {
        using (var result = new MemoryStream())
        {
            var lengthBytes = BitConverter.GetBytes(input.Length);
            result.Write(lengthBytes, 0, 4);

            using (var compressionStream = new GZipStream(result,
                CompressionMode.Compress))
            {
                compressionStream.Write(input, 0, input.Length);
                compressionStream.Flush();

            }
            return result.ToArray();
        }
    }
}