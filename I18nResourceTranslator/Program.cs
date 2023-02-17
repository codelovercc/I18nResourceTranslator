// See https://aka.ms/new-console-template for more information

using System.CommandLine;

namespace I18nResourceTranslator;

public static class Program
{
    public static int Main(string[] args)
    {
        var fromOption = new Option<string>(new[] { "--from", "-f" }, "谷歌语言编码，指定输入文件的语言")
        {
            IsRequired = true
        };
        var toOption = new Option<string>(new[] { "--to", "-t" }, "谷歌语言编码，要翻译的目标语言")
        {
            IsRequired = true
        };
        var sourceOption = new Option<string>(new[] { "--source-path", "-sp" }, "源文件路径")
        {
            IsRequired = true,
        };

        var encoderOption = new Option<bool>(new[] { "-e" }, "指定该标志，结果将转义Unicode字符，否则不转义，此标志对PHP文件无效");

        sourceOption.AddValidator(result =>
        {
            if (!File.Exists(result.GetValueForOption(sourceOption)))
            {
                result.ErrorMessage = "源文件不存在";
            }
        });
        var rootCommand = new RootCommand("将国际化资源文件自动翻译到其它语言")
        {
            fromOption,
            toOption,
            sourceOption,
            encoderOption
        };
        rootCommand.SetHandler(async (context) =>
        {
            var from = context.ParseResult.GetValueForOption(fromOption)!;
            var to = context.ParseResult.GetValueForOption(toOption)!;
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            Console.WriteLine($"将源文件语言{from}翻译成{to}");
            Console.WriteLine("正在加载文件...");
            var content = await File.ReadAllTextAsync(source);
            if (string.IsNullOrWhiteSpace(content))
            {
                await Console.Error.WriteLineAsync("文件没有内容");
                context.ExitCode = -1;
                return;
            }

            var fileInfo = new FileInfo(source);
            var ext = fileInfo.Extension;
            Console.WriteLine("正在翻译...");
            var translator = CreateTranslator(from, to, content, ext);
            var rePath = $"./{fileInfo.Name}.{to}{ext}";
            await File.WriteAllTextAsync(rePath,
                await translator.GetResult(context.ParseResult.GetValueForOption(encoderOption)));
            Console.WriteLine($"翻译完成，结果已经保存到：{Path.GetFullPath(rePath)}");
            context.ExitCode = 0;
        });
        return rootCommand.InvokeAsync(args).Result;
    }

    private static Translator CreateTranslator(string from, string to, string content, string ext)
    {
        return ext switch
        {
            ".php" => new PhpTranslator(from, to, content),
            _ => new JsonTranslator(from, to, content) //默认为Json格式
        };
    }
}