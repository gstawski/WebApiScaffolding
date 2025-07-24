using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApiScaffolding.Models.Templates;

public class GeneratorContext
{
    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        IgnoreReadOnlyProperties = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    [JsonPropertyName("m")]
    public ClassMeta MetaData { get; init; }

    [JsonPropertyName("n")]
    public string Namespace {get; init;}

    [JsonIgnore]
    public string ClassName => MetaData.Name;

    public GeneratorContext(string objContext)
    {
        var gc = JsonSerializer.Deserialize<GeneratorContext>(objContext, JsonOptions);
        Namespace = gc.Namespace;
        MetaData = gc.MetaData;
    }

    public GeneratorContext(ClassMeta metaData, string nameSpace)
    {
        Namespace = nameSpace;
        MetaData = metaData;
    }

    [JsonConstructor]
    public GeneratorContext()
    {
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonOptions);
    }
}