using System.Text;

namespace I18nResourceTranslator;

/// <summary>
/// php 文件内容为PHP 数组，将寻找到 => 出现的第一个字符串进行翻译
/// </summary>
public class PhpTranslator : Translator
{
    private readonly IList<ToTranStrInfo> _toTranStrPosList;

    public PhpTranslator(string from, string to, string content) : base(from, to, content)
    {
        _toTranStrPosList = new List<ToTranStrInfo>();
    }

    private class ToTranStrInfo
    {
        public int Line;
        public int Start;
        public int End;
        public char Quote;
        public string LineStr = default!;

        /// <summary>
        /// 要进行翻译的值
        /// </summary>
        public string Key = default!;

        public string Transed = default!;
    }

    protected override async Task DoEditAndTranslation()
    {
        _toTranStrPosList.Clear();
        await FindKeys();
        TransKeys();
    }

    private void TransKeys()
    {
        foreach (var info in _toTranStrPosList)
        {
            EditTasks.Add(TransKey(info));
        }
    }

    private async Task TransKey(ToTranStrInfo info)
    {
        info.Transed = await Translate(info.Key);
    }

    private async Task FindKeys()
    {
        using var reader = new StringReader(Content);
        using var lineReader = new StringReader(Content);
        var c = -1;
        var equalFlag = false;
        var greaterFlag = false;
        var quoteFlag = false;
        var lineNumber = 0;
        var columnNumber = 0;
        var quote = '\'';
        var line = "";
        ToTranStrInfo? pos = null;
        while ((c = reader.Read()) > -1)
        {
            if (columnNumber == 0)
            {
                line = await lineReader.ReadLineAsync();
            }

            columnNumber++;
            if (c == '=' && !quoteFlag)
            {
                equalFlag = true;
                continue;
            }

            if (c == '>' && !quoteFlag && equalFlag)
            {
                greaterFlag = true;
                continue;
            }

            if (c == '\n' && !quoteFlag)
            {
                lineNumber++;
                columnNumber = 0;
                continue;
            }

            if (equalFlag && greaterFlag && c != ' ' && c != '\t' && c != '\'' && c != '"')
            {
                equalFlag = false;
                greaterFlag = false;
                continue;
            }

            if (equalFlag && greaterFlag && c is '\'' or '"')
            {
                //quote start
                quote = (char)c;
                pos = new ToTranStrInfo();
                pos.Quote = quote;
                quoteFlag = true;
                equalFlag = false;
                greaterFlag = false;
                pos.Line = lineNumber;
                pos.Start = columnNumber;
                pos.LineStr = line!;
                continue;
            }

            if (quoteFlag && c == quote)
            {
                //quote end
                quoteFlag = false;
                if (pos == null)
                {
                    throw new InvalidOperationException("未记录到翻译信息，翻译失败。");
                }

                pos.End = columnNumber;
                //取出不包括quote的字符串
                pos.Key = line!.Substring(pos.Start, pos.End - pos.Start - 1);
                _toTranStrPosList.Add(pos);
                pos = null;
            }
        }
    }

    protected override Task<string> GetStringResult(bool encode)
    {
        var sb = new StringBuilder(Content);
        foreach (var info in _toTranStrPosList)
        {
            sb.Replace(info.Quote + info.Key + info.Quote, info.Quote + info.Transed + info.Quote);
        }

        return Task.FromResult(sb.ToString());
    }
}