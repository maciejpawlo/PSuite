using System.Text;
using System.Text.Json;

namespace PSuite.Shared.Infrastructure.Modules;

internal sealed class JsonModuleSerializer : IModuleSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public byte[] Serialize<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), SerializerOptions);

    public T Deserialize<T>(byte[] value)
        => JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(value), SerializerOptions)!;

    public object Deserialize(byte[] value, Type type)
        => JsonSerializer.Deserialize(Encoding.UTF8.GetString(value), type, SerializerOptions)!;
}