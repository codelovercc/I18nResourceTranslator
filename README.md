# I18nResourceTranslator
translation for vue project using vue-i18n,.json files translation are also supported.为VUE项目使用vue-i18n国际化多语言文件支持翻译功能，JSON文件也支持翻译。
<br>
using google translator free API,thanks google.
<br>
usage
`I18nResourceTranslator -f <lang_code> -t <lang_code> -sp <source_filepath>`
<br>
show helps
`I18nResourceTranslator -h`
<br>
example
`I18nResourceTranslator -f zh-CN -t fr -sp ./cn.json`

cn.json file like this
`{
    "bind_account": {
        "bind_account": "绑定账号",
        "Account": "账号",
        "account_tip": "请输入"
    }
}`
<br>
理论上只要是JSON文件都可以翻译
<br>
after translation completed, translation file will be at the program's directory.if there's error while translating,you should try again.