using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

namespace I18nResourceTranslator;

public class Translator
{
    private readonly string from;
    private readonly string to;
    private List<Task> editTasks = new();
    private IDictionary<string, string> translatorCache = new ConcurrentDictionary<string, string>();
    private readonly string cachePath;
    private static readonly Regex tokensRegex = new Regex(@"\{[\w\-_\+\d]+\}");

    public Translator(string from, string to)
    {
        this.from = from;
        this.to = to;
        cachePath = $"./translated_{from}_{to}.json";
    }

    public async Task StartEditJsonAndTranslation(JsonNode doc)
    {
        if (File.Exists(cachePath))
        {
            Console.WriteLine("加载已翻译的缓存内容...");
            var cachedJson = await File.ReadAllTextAsync(cachePath);
            translatorCache = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(cachedJson) ??
                              translatorCache;
            Console.WriteLine("已翻译缓存已加载");
        }

        try
        {
            await EditJson(doc);
            Task.WaitAll(editTasks.ToArray());
            var failedTasks = editTasks.Where(task => task.IsFaulted).ToArray();
            foreach (var task in failedTasks)
            {
                Console.WriteLine("重试翻译任务: {0}", task.Id);
                task.Start();
            }

            Task.WaitAll(failedTasks.ToArray());
        }
        catch (Exception e)
        {
            Console.WriteLine("翻译失败");
            Console.WriteLine(e);
        }

        Console.WriteLine("翻译已完成");
        await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(translatorCache));
        Console.WriteLine("翻译缓存已保存");
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
                    editTasks.Add(EditJson(pro.Value));
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

    private async Task<string> Translate(string toTrans)
    {
        if (translatorCache.ContainsKey(toTrans))
        {
            return translatorCache[toTrans];
        }

        var httpClient = new HttpClient();
        var url =
            $"http://translate.google.cn/translate_a/single?client=gtx&dt=t&dj=1&ie=UTF-8&sl={from}&tl={to}&q={HttpUtility.UrlEncode(toTrans)}";
        var json = await httpClient.GetStringAsync(url);
        //{
        // "sentences":[
        // {
        // "trans":"Hello there","orig":"你好","backend":10
        // 
        // }],
        // 
        // "src":"zh-CN",
        // 
        // "confidence":1.0,
        // "spell":{},
        // "ld_result":{
        // "srclangs":["zh-CN"],
        // "srclangs_confidences":[1.0],
        // "extended_srclangs":["zh-CN"]
        // }
        // }
        var jsonObj = JsonNode.Parse(json);
        var tran = jsonObj!["sentences"]!.AsArray().Select(node => node!["trans"]!.GetValue<string>())
            .Aggregate((s, s1) => s + s1);
        var toTransTokens = tokensRegex.Matches(toTrans);
        var tranTokens = tokensRegex.Matches(tran);
        if (toTransTokens.Count != tranTokens.Count)
        {
            var bgC = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine("原Token与译文中的Token数目不一致，请比对");
            Console.WriteLine("\t{0} | {1}", toTransTokens.Count,
                toTransTokens.Any() ? toTransTokens.Select(m => m.Value).Aggregate((s, s1) => s + " " + s1) : "-");
            Console.WriteLine("\t{0} | {1}", tranTokens.Count,
                tranTokens.Any() ? tranTokens.Select(m => m.Value).Aggregate((s, s1) => s + " " + s1) : "-");
            Console.BackgroundColor = bgC;
        }
        else
        {
            for (var i = 0; i < toTransTokens.Count; i++)
            {
                var t = toTransTokens[i];
                var t1 = tranTokens[i];
                if (t.Value == t1.Value)
                {
                    continue;
                }

                tran = tran.Replace(t1.Value, t.Value);
            }
        }

        translatorCache[toTrans] = tran;
        Console.WriteLine("已翻译：");
        Console.WriteLine($"\t{url}");
        Console.WriteLine($"\t{toTrans}");
        Console.WriteLine($"\t{tran}");
        return tran;
    }
}