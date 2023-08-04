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
            IsRequired = false,
        };

        var deleteFlagOption = new Option<bool>(new[] { "-d" }, "指定该标志，将删除对应的源语言到目标语言的翻译缓存文件");

        var openCartFlagOption = new Option<bool>(new[] { "-oc", "--opencart" },
            "指定该标志，表示翻译OpenCart商城系统的语言包，源文件路径参数（--source-path）表示具体语言包的目录，" +
            "比如英文语言名目录，支持OpenCart 4.0，其它版本未测试兼容性");

        var encoderOption = new Option<bool>(new[] { "-e" }, "指定该标志，结果将转义Unicode字符，否则不转义，此标志对PHP文件无效");

        sourceOption.AddValidator(result =>
        {
            if (result.GetValueForOption(deleteFlagOption))
            {
                return;
            }

            // 验证OpenCart语言包是否存在
            if (result.GetValueForOption(openCartFlagOption))
            {
                if (!Directory.Exists(result.GetValueForOption(sourceOption)))
                {
                    result.ErrorMessage = "OpenCart语言包目录不存在";
                }

                return;
            }

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
            openCartFlagOption,
            encoderOption,
            deleteFlagOption
        };
        rootCommand.SetHandler(async context =>
        {
            var from = context.ParseResult.GetValueForOption(fromOption)!;
            var to = context.ParseResult.GetValueForOption(toOption)!;
            var source = context.ParseResult.GetValueForOption(sourceOption)!;
            var encoder = context.ParseResult.GetValueForOption(encoderOption);
            var openCartFlag = context.ParseResult.GetValueForOption(openCartFlagOption);
            bool result;

            if (context.ParseResult.GetValueForOption(deleteFlagOption))
            {
                if (openCartFlag)
                {
                    to = to.ToLower();
                }

                DeleteCacheFile(from, to);
                return;
            }

            if (openCartFlag)
            {
                result = await TranslationOpenCart(from, to, source, encoder);
            }
            else
            {
                result = await TranslationFile(from, to, source, encoder, openCartFlag, Path.GetDirectoryName(source)!);
            }

            context.ExitCode = result ? 0 : -1;
        });
        return rootCommand.InvokeAsync(args).Result;
    }

    private static void DeleteCacheFile(string from, string to)
    {
        var path = $"./translated_{from}_{to}.json";
        try
        {
            File.Delete(path);
            Console.WriteLine("{0} 缓存文件已删除", Path.GetFullPath(path));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    /// 翻译OpenCart语言包
    /// </summary>
    /// <param name="from">源语言</param>
    /// <param name="to">目标语言</param>
    /// <param name="source">open cart的语言包路径</param>
    /// <param name="encoder">是否对翻译值编码，目前没用</param>
    private static async Task<bool> TranslationOpenCart(string from, string to, string source, bool encoder)
    {
        // 获取所有文件的路径，包括子目录的子目录下的文件，就像递归获取一样
        var searchOptions = new EnumerationOptions
        {
            MaxRecursionDepth = 1024,
            RecurseSubdirectories = true,
        };
        var phpFiles = Directory.GetFiles(source, "*.php",
            searchOptions);
        if (phpFiles.Length == 0)
        {
            await Console.Error.WriteLineAsync("源目录并不包含任何.php文件");
            return false;
        }

        var destPath = Path.GetFullPath(Path.Combine(".", "OpenCart", to));
        try
        {
            // 删除原来翻译的所有文件
            Directory.Delete(destPath, true);
        }
        catch (Exception)
        {
            // ignored
        }

        var failedFiles = new List<string>();

        // opencart 中的语言代码都是小写的
        to = to.ToLower();
        var allSucceeded = true;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var file in phpFiles)
        {
            var fileSucceeded = await TranslationFile(from, to, file, encoder, true, source);
            if (!fileSucceeded)
            {
                failedFiles.Add(file);
            }

            allSucceeded = allSucceeded && fileSucceeded;
        }

        if (!allSucceeded)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            await Console.Error.WriteLineAsync("一个或多个文件翻译失败，原因可能是文件没有内容，请检查输出，手动修改或者重试翻译");
            Console.WriteLine("以下是失败文件列表：");
            Console.WriteLine(failedFiles.Aggregate((s, s1) => $"{s}\n{s1}"));
            Console.ResetColor();
        }

        var defaultPhpFilePath = Path.GetFullPath(Path.Combine(".", "OpenCart", to, "default.php"));
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine("翻译已完成，所有文件都保存在目录 {0}", destPath);
        Console.WriteLine("接下来请手动完成以下步骤：");
        Console.WriteLine("1. 修改文件 {0} 中的语言代码", defaultPhpFilePath);
        Console.WriteLine("2. 修改文件 {0} 中的区域格式化配置", defaultPhpFilePath);
        Console.WriteLine("3. 修改文件 {0} 中的`ckeditor`编辑器的语言代码为 `{1}`",
            defaultPhpFilePath, to);
        Console.WriteLine("4. 修改文件 {0} 中的`datepicker`日期选择器的语言代码为 `{1}`",
            defaultPhpFilePath, to);
        Console.WriteLine("5. 添加名为 {0}.png 的LOGO文件到目录 {1}", to, destPath);
        Console.WriteLine(
            "6. 将 {0} 目录打包为OpenCart语言包，并进行安装。打包教程参考：https://webocreation.com/how-to-make-the-custom-language-pack-in-opencart-3/ 中的第9步。",
            destPath);
        Console.ResetColor();

        return allSucceeded;
    }

    /// <summary>
    /// 翻译文件
    /// </summary>
    /// <param name="from">源语言</param>
    /// <param name="to">目标语言</param>
    /// <param name="filePath">要翻译的文件路径</param>
    /// <param name="encoder">Json是否需要Unicode编码</param>
    /// <param name="openCartFlag">是否为OpenCart语言文件</param>
    /// <param name="sourcePath">源路径，如果是OpenCart则为OpenCart的语言包路径，如果是php数组语言文件或json语言文件则为该文件的目录</param>
    /// <returns>返回成功或失败</returns>
    private static async Task<bool> TranslationFile(string from, string to, string filePath, bool encoder,
        bool openCartFlag, string sourcePath)
    {
        Console.WriteLine($"将源文件语言{from}翻译成{to}");
        Console.WriteLine("正在加载文件...");
        var content = await File.ReadAllTextAsync(filePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            await Console.Error.WriteLineAsync("文件没有内容");
            return false;
        }

        var ext = Path.GetExtension(filePath);
        Console.WriteLine("正在翻译...");
        var translator = CreateTranslator(from, to, content, ext, openCartFlag);
        string rePath;
        if (openCartFlag)
        {
            if (!sourcePath.EndsWith(Path.DirectorySeparatorChar))
            {
                sourcePath += Path.DirectorySeparatorChar;
            }

            // sourcePath : /opencart/upload/catalog/language/en-gb/
            // dirName : en-gb
            // filePath : /opencart/upload/catalog/language/en-gb/api/account/login.php
            // rePath : ./OpenCart/zh-CN/api/account/login.php
            rePath = Path.Combine(".", "OpenCart", to, filePath.Replace(sourcePath, ""));
            // 确保路径存在
            Directory.CreateDirectory(Path.GetDirectoryName(rePath)!);
        }
        else
        {
            rePath = $".{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(filePath)}.{to}{ext}";
        }

        await File.WriteAllTextAsync(rePath,
            await translator.GetResult(encoder));
        Console.WriteLine($"翻译完成，结果已经保存到：{Path.GetFullPath(rePath)}");
        return true;
    }

    private static Translator CreateTranslator(string from, string to, string content, string ext, bool openCartFlag)
    {
        if (openCartFlag)
        {
            return new OpenCartTranslator(from, to, content);
        }

        return ext switch
        {
            ".php" => new PhpTranslator(from, to, content),
            _ => new JsonTranslator(from, to, content) //默认为Json格式
        };
    }
}