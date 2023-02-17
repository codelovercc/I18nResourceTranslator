# I18nResourceTranslator
translation for vue project using vue-i18n,.json files translation are also supported.PHP array resource files are supported. 为VUE项目使用vue-i18n国际化多语言文件支持翻译功能，JSON文件也支持翻译。
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
```shell
I18nResourceTranslator --help
I18nResourceTranslator.exe --help

Description:
  将国际化资源文件自动翻译到其它语言

Usage:
I18nResourceTranslator [options]

Options:
-f, --from <from> (REQUIRED)                 谷歌语言编码，指定输入文件的语言
-t, --to <to> (REQUIRED)                     谷歌语言编码，要翻译的目标语言
-sp, --source-path <source-path> (REQUIRED)  源文件路径
-e                                           指定该标志，结果将转义Unicode字符，否则不转义，此标志对PHP文件无效
--version                                    Show version information
-?, -h, --help                               Show help and usage information

```
<br>
理论上只要是JSON文件都可以翻译
<br>
after translation completed, translation file will be at the program's directory.if there's error while translating,you should try again.