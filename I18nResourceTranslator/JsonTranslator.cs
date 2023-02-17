using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;

namespace I18nResourceTranslator;

public class JsonTranslator : Translator
{
    private readonly JsonNode _rootNode;

    public JsonTranslator(string from, string to, string content) : base(from, to, content)
    {
        _rootNode = JsonNode.Parse(content)!;
    }

    private async Task EditJson(JsonNode doc)
    {
        var json = doc.AsObject();
        var dic = new Dictionary<string, JsonNode>();
        foreach (var pro in json)
        {
            switch (pro.Value)
            {
                case null:
                    continue;
                case JsonObject or JsonArray:
                    EditTasks.Add(EditJson(pro.Value));
                    break;
                case JsonValue value:
                    dic.Add(pro.Key, JsonValue.Create(await Translate(value.GetValue<string>()))!);
                    break;
            }
        }

        foreach (var node in dic)
        {
            json[node.Key] = node.Value;
        }
    }

    protected override async Task DoEditAndTranslation()
    {
        await EditJson(_rootNode);
    }

    protected override Task<string> GetStringResult(bool encode)
    {
        return Task.FromResult(_rootNode.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = encode
                ? null
                : JavaScriptEncoder.Create(UnicodeRanges.All)
        }));
    }
}