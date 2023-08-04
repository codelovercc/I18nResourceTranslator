# I18nResourceTranslator

translation for vue project using vue-i18n,.json files translation are also supported.PHP array resource files are
supported. 为VUE项目使用vue-i18n国际化多语言文件支持翻译功能，JSON文件也支持翻译。  
支持OpenCart 4 语言包的翻译，方便为OpenCart 4生成语言包扩展。 Support OpenCart 4 language pack translation.
<br>
using google translator free API,thanks google.
<br>
usage

```shell
I18nResourceTranslator -f <lang_code> -t <lang_code> -sp <source_filepath>
```

show helps:

```shell
I18nResourceTranslator -h
```

example:

```shell
I18nResourceTranslator -f zh-CN -t en -sp ./cn.json
```

cn.json file like this

```json
{
  "bind_account": {
    "bind_account": "绑定账号 {bind-1} {test_12} {test_+123}",
    "Account": "{account}账号",
    "account_tip": "请输入{name}"
  }
}
```

and result like this

```json
{
  "bind_account": {
    "bind_account": "Bind account {bind-1} {test_12} {test_\u002B123}",
    "Account": "{account} account",
    "account_tip": "Please enter {name}"
  }
}
```

php array resource file:

```PHP
return [
    'success' => 'Success',
    'fail' => 'Fail',
    "bind" =>[
        "no_img"=>"Please upload screenshot",
        "exist"=>"Bind info exist!",
        "interface_error"=>"Interface error!",
        "invalid_bind"=>"inlvaid bind"
    ],
]
```

```shell
Description:
  将国际化资源文件自动翻译到其它语言

Usage:
  I18nResourceTranslator [options]

Options:
  -f, --from <from> (REQUIRED)      谷歌语言编码，指定输入文件的语言
  -t, --to <to> (REQUIRED)          谷歌语言编码，要翻译的目标语言
  -sp, --source-path <source-path>  源文件路径
  -oc, --opencart                   指定该标志，表示翻译OpenCart商城系统的语言包，源文件路径参数（--source-path）表示具体语言包的目录，比如英文语言名目录，支持OpenCart 4.0，其它版本未测试兼容性
  -e                                指定该标志，结果将转义Unicode字符，否则不转义，此标志对PHP文件无效
  -d                                指定该标志，将删除对应的源语言到目标语言的翻译缓存文件
  --version                         Show version information
  -?, -h, --help                    Show help and usage information


```

## OpenCart 4 支持

OpenCart 4 需要翻译的语言文件目录

1. `upload/catalog/language` 对应语言代码的目录中
2. `upload/admin/language` 对应语言代码的目录中
3. 自带扩展目录 `upload/extension/opencart/admin/language` 对应语言代码的目录中
4. 自带扩展目录 `upload/extension/opencart/catalog/language` 对应语言代码的目录中
5. 其它扩展目录中的语言包与自带扩展目录的语言包位置相同，只是 opencart 扩展名字换成其它扩展的名字

现在举个例子，我从OpenCart官网下载了 Opencart4.0.2.2.zip 源码。将其解压后，找到解压文件中的 upload 目录。  
这个源码目前语言是英文，我们将其翻译到中文。  
使用下面几组命令：

1. 翻译`upload/catalog/language/en-gb`中的所有PHP文件到`zh-cn`

    ```shell
    I18nResourceTranslator -f en -t zh-CN -sp /your/path/to/upload/admin/language/en-gb -oc
    ```
   等待翻译完成，把翻译好的文件目录都拷贝出来备用，翻译好的文件目录在命令结束时输出的提示中可以找到。

2. 翻译`upload/catalog/language/en-gb`中的所有PHP文件到`zh-cn`
    ```shell
    I18nResourceTranslator -f en -t zh-CN -sp /your/path/to/upload/catalog/language/en-gb -oc
    ```
   等待翻译完成，把翻译好的文件目录都拷贝出来备用，翻译好的文件目录在命令结束时输出的提示中可以找到。
3. 翻译`upload/extension/opencart/admin/language/en-gb`中的所有PHP文件到`zh-cn`
    ```shell
    I18nResourceTranslator -f en -t zh-CN -sp /your/path/to/upload/extension/opencart/admin/language/en-gb -oc
    ```
   等待翻译完成，把翻译好的文件目录都拷贝出来备用，翻译好的文件目录在命令结束时输出的提示中可以找到。
4. 翻译`upload/extension/opencart/catalog/language/en-gb`中的所有PHP文件到`zh-cn`
    ```shell
    I18nResourceTranslator -f en -t zh-CN -sp /your/path/to/upload/extension/opencart/catalog/language/en-gb -oc
5. 翻译其它的扩展文件对应的资源文件，比如`upload/extension/elseExtension/catalog/language/en-gb`中的所有PHP文件到`zh-cn`
    ```shell
    I18nResourceTranslator -f en -t zh-CN -sp /your/path/to/upload/extension/elseExtension/catalog/language/en-gb -oc
    ```
   等待翻译完成，把翻译好的文件目录都拷贝出来备用，翻译好的文件目录在命令结束时输出的提示中可以找到。

<br>
理论上只要是JSON文件都可以翻译
<br>
after translation completed, translation file will be at the program's directory.if there's error while
translating,you should try again.