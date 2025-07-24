using System.Text.Json.Serialization;

namespace WebApiScaffolding.Models.Templates;

public class ClassMeta
{
    [JsonPropertyName("n")]
    public string Name { get; set; }

    [JsonPropertyName("ns")]
    public string NameSpace { get; set; }

    [JsonInclude]
    [JsonPropertyName("p")]
    public List<PropertyMeta> Properties { get; set; } = new ();

    [JsonPropertyName("st")]
    public List<ClassMeta> SubTypes { get; set; }
}