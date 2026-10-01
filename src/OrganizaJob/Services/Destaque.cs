using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace OrganizaJob.Services;

/// <summary>Realce de sintaxe simples, sem dependências. Comentários, textos, números, palavras-chave e {{variáveis}}.</summary>
public static partial class Destaque
{
    private static readonly HashSet<string> Sql = Palavras(
        "select from where and or not in is null join inner left right outer full cross on group by order having insert into values " +
        "update set delete create alter drop table view procedure proc function trigger begin end if else declare as distinct top case when " +
        "then exec execute kill union all exists with nolock like between truncate index primary key foreign references go use return " +
        "while commit rollback transaction tran asc desc count sum min max avg cast convert isnull coalesce getdate");

    private static readonly HashSet<string> Shell = Palavras(
        "if then else elif fi for foreach do done while until case esac function return param try catch finally switch in " +
        "export echo sudo cd ls git cat grep set elseif break continue exit");

    private static readonly HashSet<string> CSharp = Palavras(
        "using namespace class public private protected internal static void var new return if else for foreach while do switch case " +
        "break continue try catch finally throw async await string int bool true false null this base record interface const readonly " +
        "function let in of import export default");

    private static readonly HashSet<string> Cmd = Palavras(
        "echo set if else goto call exit for in do not exist pause cd copy del move start errorlevel");

    private static HashSet<string> Palavras(string s) =>
        new(s.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"(?<cm>--[^\n]*|/\*[\s\S]*?\*/)|(?<str>'(?:[^'\n]|'')*')|(?<var>\{\{\w+\}\})|(?<num>\b\d+(?:\.\d+)?\b)|(?<w>[A-Za-z_@#][\w]*)")]
    private static partial Regex SqlRx();

    [GeneratedRegex(@"(?<cm>#[^\n]*)|(?<str>""(?:[^""\\\n]|\\.)*""|'[^'\n]*')|(?<var>\{\{\w+\}\})|(?<num>\b\d+(?:\.\d+)?\b)|(?<w>\$?[A-Za-z_][\w]*(?:-[A-Za-z]\w*)*)")]
    private static partial Regex ShellRx();

    [GeneratedRegex(@"(?<cm>//[^\n]*|/\*[\s\S]*?\*/|#[^\n]*)|(?<str>""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)*')|(?<var>\{\{\w+\}\})|(?<num>\b\d+(?:\.\d+)?\b)|(?<w>[A-Za-z_][\w]*)")]
    private static partial Regex CLikeRx();

    [GeneratedRegex(@"(?<cm>(?im:^\s*(?:rem\b|::)[^\n]*))|(?<str>""[^""\n]*"")|(?<var>\{\{\w+\}\}|%\w+%)|(?<num>\b\d+\b)|(?<w>[A-Za-z_][\w]*)")]
    private static partial Regex CmdRx();

    [GeneratedRegex(@"(?<var>\{\{\w+\}\})")]
    private static partial Regex SoVariaveisRx();

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex VariavelRx();

    public static IReadOnlyList<string> Variaveis(string? codigo) =>
        VariavelRx().Matches(codigo ?? "").Select(m => m.Groups[1].Value).Distinct().ToList();

    public static string Preencher(string codigo, IReadOnlyDictionary<string, string> valores) =>
        VariavelRx().Replace(codigo, m =>
            valores.TryGetValue(m.Groups[1].Value, out var v) && !string.IsNullOrEmpty(v) ? v : m.Value);

    public static MarkupString Html(string? codigo, string? linguagem)
    {
        codigo ??= "";
        var (rx, palavras) = (linguagem ?? "").ToLowerInvariant() switch
        {
            "sql" => (SqlRx(), Sql),
            "powershell" or "bash" or "python" => (ShellRx(), Shell),
            "c#" or "javascript" => (CLikeRx(), CSharp),
            "cmd" => (CmdRx(), Cmd),
            _ => (SoVariaveisRx(), new HashSet<string>())
        };

        var sb = new StringBuilder(codigo.Length * 2);
        var pos = 0;
        foreach (Match m in rx.Matches(codigo))
        {
            sb.Append(WebUtility.HtmlEncode(codigo[pos..m.Index]));
            var texto = WebUtility.HtmlEncode(m.Value);
            string? classe =
                m.Groups["cm"].Success ? "cm" :
                m.Groups["str"].Success ? "str" :
                m.Groups["var"].Success ? "var" :
                m.Groups["num"].Success ? "num" :
                m.Groups["w"].Success && palavras.Contains(m.Value) ? "kw" :
                m.Groups["w"].Success && m.Value.Contains('-') && char.IsUpper(m.Value[0]) ? "fn" :
                m.Groups["w"].Success && m.Value.StartsWith('$') ? "var2" : null;
            sb.Append(classe is null ? texto : $"<span class=\"{classe}\">{texto}</span>");
            pos = m.Index + m.Length;
        }
        sb.Append(WebUtility.HtmlEncode(codigo[pos..]));
        return new MarkupString(sb.ToString());
    }
}
