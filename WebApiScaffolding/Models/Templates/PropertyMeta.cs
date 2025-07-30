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

    [JsonPropertyName("o")]
    public int Order { get; set; }

    [JsonPropertyName("p")]
    public bool IsSetPublic { get; set; }

    [JsonPropertyName("c")]
    public bool IsCollection { get; set; }

    [JsonPropertyName("v")]
    public bool IsValueObject { get; set; }

    [JsonPropertyName("w")]
    public string WithOne { get; set; }

    [JsonPropertyName("f")]
    public string ForeignKey { get; set; }

    public PropertyMeta Clone()
    {
        return new PropertyMeta
        {
            Name = Name,
            Type = Type,
            IsSimpleType = IsSimpleType,
            Order = Order,
            IsSetPublic = IsSetPublic,
            IsCollection = IsCollection,
            IsValueObject = IsValueObject,
            WithOne = WithOne,
            ForeignKey = ForeignKey
        };
    }
}