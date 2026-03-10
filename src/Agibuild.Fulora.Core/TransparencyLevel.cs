using System.Text.Json.Serialization;

namespace Agibuild.Fulora;

/// <summary>
/// Platform transparency composition levels.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransparencyLevel>))]
public enum TransparencyLevel
{
    None,
    Transparent,
    Blur,
    AcrylicBlur,
    Mica
}
