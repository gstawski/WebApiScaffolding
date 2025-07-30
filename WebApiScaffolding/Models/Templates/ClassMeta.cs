using System.Text.Json.Serialization;

namespace WebApiScaffolding.Models.Templates;

public class ClassMeta
{
    [JsonPropertyName("n")]
    public string Name { get; set; }

    [JsonPropertyName("ns")]
    public string NameSpace { get; set; }

    [JsonPropertyName("c")]
    public int Order { get; set; } = 0;

    [JsonInclude]
    [JsonPropertyName("p")]
    public List<PropertyMeta> Properties { get; set; } = new ();

    [JsonInclude]
    [JsonPropertyName("nss")]
    public List<string> Namespaces { get; set; } = new ();

    public ClassMeta Clone()
    {
        return new ClassMeta
        {
            Name = Name,
            NameSpace = NameSpace,
            Order = Order,
            Properties = [..Properties.ConvertAll(p => p.Clone())],
            Namespaces = [..Namespaces]
        };
    }
}