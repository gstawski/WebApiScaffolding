using System.Text.Json.Serialization;

namespace WebApiScaffolding.Models.Templates;

public class PropertyMeta
{
    [JsonPropertyName("n")]
    public string Name { get; set; }

    [JsonPropertyName("t")]
    public string Type { get; set; }

    [JsonPropertyName("s")]
    public bool IsSimpleType { get; set; }
}