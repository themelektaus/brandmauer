using System.Reflection;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brandmauer;

public class ExceptionConverter : JsonConverter<Exception>
{
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return default;
    }

    public override void Write(Utf8JsonWriter writer, Exception value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        var exceptionType = value.GetType();
        writer.WriteString("ClassName", exceptionType.FullName);

        var properties = exceptionType.GetProperties()
            .Where(e => e.PropertyType != typeof(Type))
            .Where(e => e.PropertyType.Namespace != typeof(MemberInfo).Namespace)
            .ToList();

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(value, null);

            if (options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull && propertyValue is null)
                continue;

            writer.WritePropertyName(property.Name);
            JsonSerializer.Serialize(writer, propertyValue, property.PropertyType, options);
        }

        writer.WriteEndObject();
    }
}
