using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

namespace I18nResourceTranslator;

public abstract class Translator
{
    /// <summary>
    /// 源文件内容
    /// </summary>
    public string Content { get; }
    private readonly string _from;
    private readonly string _to;
    protected readonly List<Task> EditTasks = new();
    private IDictionary<string, string> _translatorCache = new ConcurrentDictionary<string, string>();
    private readonly string _cachePath;
    private static readonly Regex TokensRegex = new(@"\{[\w\-_\+\d]+\}");

    protected Translator(string from, string to, string content)
    {
        Content = content;
        _from = from;
        _to = to;
        _cachePath = $"./translated_{from}_{to}.json";
    }

    protected virtual async Task StartEditAndTranslation()
    {
        if (File.Exists(_cachePath))
        {
            Console.WriteLine("加载已翻译的缓存内容...");
            var cachedJson = await File.ReadAllTextAsync(_cachePath);
            _translatorCache = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(cachedJson) ??
                              _translatorCache;
            Console.WriteLine("已翻译缓存已加载");
        }

        try
        {
            await DoEditAndTranslation();
            Task.WaitAll(EditTasks.ToArray());
            var failedTasks = EditTasks.Where(task => task.IsFaulted).ToArray();
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
        await File.WriteAllTextAsync(_cachePath, JsonSerializer.Serialize(_translatorCache));
        Console.WriteLine("翻译缓存已保存");
    }

    protected abstract Task DoEditAndTranslation();

    /// <summary>
    /// 开始翻译并获取整个文件的翻译结果
    /// </summary>
    /// <param name="encode">是否转义Unicode字符</param>
    /// <returns></returns>
    public async Task<string> GetResult(bool encode)
    {
        await StartEditAndTranslation();
        return await GetStringResult(encode);
    }

    protected abstract Task<string> GetStringResult(bool encode);

    protected async Task<string> Translate(string toTrans)
    {
        if (_translatorCache.ContainsKey(toTrans))
        {
            return _translatorCache[toTrans];
        }

        var httpClient = new HttpClient();
        var url =
            $"https://translate.google.com/translate_a/single?client=gtx&dt=t&dj=1&ie=UTF-8&sl={_from}&tl={_to}&q={HttpUtility.UrlEncode(toTrans)}";
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
        var toTransTokens = TokensRegex.Matches(toTrans);
        var tranTokens = TokensRegex.Matches(tran);
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

        _translatorCache[toTrans] = tran;
        Console.WriteLine("已翻译：");
        Console.WriteLine($"\t{url}");
        Console.WriteLine($"\t{toTrans}");
        Console.WriteLine($"\t{tran}");
        return tran;
    }
}