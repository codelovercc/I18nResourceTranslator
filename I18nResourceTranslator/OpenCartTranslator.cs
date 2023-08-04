using System.Text;
using System.Text.RegularExpressions;

namespace I18nResourceTranslator;

/// <summary>
/// 支持open cart 4.0 的国际化语言文件进行翻译
/// </summary>
/// <remarks>适合翻译单个文件内容，对于整个OpenCart的语言包进行翻译，需要循环每个语言文件内容然后使用本类进行翻译</remarks>
public class OpenCartTranslator : Translator
{
    private class TranslationInfo
    {
        /// <summary>
        /// <see cref="Origin"/>所属行号
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// <see cref="Origin"/>所属行号开始的索引
        /// </summary>
        public int StartIndexInLine { get; set; }

        /// <summary>
        /// <see cref="Origin"/>所属行号结束的索引
        /// </summary>
        public int EndIndexInLine { get; set; }

        /// <summary>
        /// <see cref="Origin"/>被括起的字符，单引号或双引号
        /// </summary>
        public char Quote { get; set; }

        /// <summary>
        /// 原文
        /// </summary>
        public string Origin { get; set; } = default!;

        /// <summary>
        /// 译文
        /// </summary>
        public string Translated { get; set; } = default!;
    }

    private IList<TranslationInfo> TranslationInfos { get; }

    /// <summary>
    /// 用来匹配open cart 语言文件中<code>$_['error_exception']               = 'Error Code(%s): %s in %s on line %s';</code>
    /// 的<code>Error Code(%s): %s in %s on line %s</code>值的正则表达式，
    /// 这个匹配到的值在value分组中，可以拿去翻译
    /// </summary>
    /// <remarks>该正则表达式匹配到成对的单引号或双引号中的内容，并且成对引号中的有相同引号的话不会被匹配，不懂的问GPT来解释这个正则。\1 模式：表示与第一个分组匹配相同的字符，第一个分组匹配是组quote。</remarks>
    private static Regex OriginValueRegex { get; } = new(@"\s*=\s*(?<quote>['""])(?<value>[^\1]+)\1\s*;\s*");

    /// <summary>
    /// 要跳过翻译的值，列表项为要跳过的变量名
    /// </summary>
    /// <remarks>open cart 中的国际化的default.php文件中，包含格式化的配置、语言配置，这些属于配置信息，不需要进行翻译。
    /// 语言配置可以在外部进行完成，格式化的配置需要手动完成</remarks>
    private static IList<string> SkippedValues { get; } = new List<string>()
    {
        "code", "direction", "date_format_short",
        "date_format_long", "time_format", "datetime_format",
        "decimal_point", "thousand_point", "ckeditor", "datepicker", "DONTCHANGE"
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="from">原语言</param>
    /// <param name="to">目标语言</param>
    /// <param name="content">内容</param>
    public OpenCartTranslator(string from, string to, string content) : base(from, to, content)
    {
        TranslationInfos = new List<TranslationInfo>();
    }

    protected override Task DoEditAndTranslation()
    {
        TranslationInfos.Clear();
        CollectTranslationInfos();
        AddTranslationTask();
        return Task.CompletedTask;
    }

    protected override Task<string> GetStringResult(bool encode)
    {
        var sb = new StringBuilder(Content);
        foreach (var info in TranslationInfos)
        {
            sb.Replace($"{info.Quote}{info.Origin}{info.Quote}", $"{info.Quote}{info.Translated}{info.Quote}");
        }

        return Task.FromResult(sb.ToString());
    }

    /// <summary>
    /// 找出所有需要需要的内容，并保存到<see cref="TranslationInfos"/>
    /// </summary>
    private void CollectTranslationInfos()
    {
        using var reader = new StringReader(Content);
        // line 正常值是这样的
        // $_['text_yes']                      = 'Yes';
        // 行号是从1开始的
        var lineNumber = 0;
        // 是否进入了多行注释块中
        var multiLineCommentScope = false;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            #region 处理要跳过行，包括注释和SkippedValues定义的行

            if (multiLineCommentScope)
            {
                // 在多行注释块中的行，跳过
                continue;
            }

            // 去除空白符的行内容
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("//"))
            {
                // 跳过单行注释
                continue;
            }

            // 处理多行注释，
            // 只支持 /* 开头和 */结尾的注释块，如果 /* 或者 */ 出现的位置不是开头或结尾则不会跳过
            if (trimmedLine.StartsWith("/*") && trimmedLine.EndsWith("*/"))
            {
                // 单行中的多行注释块，跳过
                continue;
            }

            if (trimmedLine.StartsWith("/*"))
            {
                // 多行注释块开始
                // 接下来的多行注释全部跳过
                multiLineCommentScope = true;
                continue;
            }

            if (trimmedLine.EndsWith("*/"))
            {
                // 多行注释块结束
                multiLineCommentScope = false;
                continue;
            }

            // 去除了所有空白符的行内容
            var allTrimmed = Regex.Replace(line, @"\s", "");
            if (SkippedValues.Any(s => allTrimmed.StartsWith($"$_['{s}']") || allTrimmed.StartsWith($"$_[\"{s}\"]")))
            {
                // 行是 $_['value'] 或者 $_["value"] 格式开头的，value 为实际的索引Key，并且在需要跳过处理的列表中
                continue;
            }

            #endregion

            // 匹配要翻译的内容
            var matches = OriginValueRegex.Matches(line);
            foreach (Match match in matches)
            {
                var group = match.Groups["value"];
                var info = new TranslationInfo
                {
                    LineNumber = lineNumber,
                    StartIndexInLine = group.Index,
                    EndIndexInLine = group.Index + group.Length - 1,
                    Quote = match.Groups["quote"].Value[0],
                    Origin = group.Value
                };
                TranslationInfos.Add(info);
            }
        }
    }

    /// <summary>
    /// 对<see cref="TranslationInfos"/>内容创建异步任务并将异步任务添加到<see cref="Translator.EditTasks"/>中，方便父类进行处理
    /// </summary>
    private void AddTranslationTask()
    {
        foreach (var info in TranslationInfos)
        {
            EditTasks.Add(Translation(info));
        }
    }

    private async Task Translation(TranslationInfo info)
    {
        info.Translated = await Translate(info.Origin);
    }
}