using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace OrganizaJob.Services;

/// <summary>Um título do texto, para montar o sumário lateral.</summary>
public sealed record TituloMd(int Nivel, string Texto, string Id);

/// <summary>Resultado da renderização: o HTML e os títulos encontrados.</summary>
public sealed record DocumentoMd(MarkupString Html, IReadOnlyList<TituloMd> Titulos);

/// <summary>
/// Markdown no padrão do GitHub (tabelas, listas de tarefas, tachado, autolinks, notas de rodapé, alertas,
/// emojis, âncoras nos títulos). HTML digitado no texto não é executado, só exibido como texto.
/// </summary>
public static partial class MarkdownHtml
{
    /// <summary>Prefixo dos ids dos títulos, para não colidir com os ids da própria tela.</summary>
    public const string PrefixoId = "md-";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UseAlertBlocks(TituloAlerta)
        .UseEmojiAndSmiley(enableSmileys: false)
        .UseSoftlineBreakAsHardlineBreak() // Enter vira quebra de linha, como nos comentários do GitHub
        .DisableHtml()
        .Build();

    private static readonly Dictionary<string, string> NomesAlerta = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Note"] = "Nota", ["Tip"] = "Dica", ["Important"] = "Importante", ["Warning"] = "Atenção", ["Caution"] = "Cuidado"
    };

    /// <summary>Alertas do GitHub (&gt; [!NOTE], [!TIP], [!IMPORTANT], [!WARNING], [!CAUTION]) com o título em português.</summary>
    private static void TituloAlerta(HtmlRenderer renderer, Markdig.Helpers.StringSlice tipo)
    {
        using var sw = new StringWriter();
        AlertBlockRenderer.DefaultRenderKind(new HtmlRenderer(sw), tipo);
        var html = sw.ToString();
        foreach (var (en, pt) in NomesAlerta) html = html.Replace($">{en}</p>", $">{pt}</p>");
        renderer.Write(html);
    }

    [GeneratedRegex(@"^\s*([a-zA-Z][a-zA-Z0-9+.\-]*):")]
    private static partial Regex EsquemaRx();

    private static readonly HashSet<string> EsquemasSeguros = new(StringComparer.OrdinalIgnoreCase)
        { "http", "https", "mailto", "tel", "file", "ftp" };

    public static DocumentoMd Renderizar(string? markdown)
    {
        var doc = Markdown.Parse(markdown ?? "", Pipeline);
        var titulos = new List<TituloMd>();

        foreach (var no in doc.Descendants())
        {
            switch (no)
            {
                case HeadingBlock h:
                    var attrs = h.GetAttributes();
                    if (!string.IsNullOrEmpty(attrs.Id))
                    {
                        attrs.Id = PrefixoId + attrs.Id;
                        titulos.Add(new TituloMd(h.Level, TextoPuro(h.Inline), attrs.Id));
                    }
                    break;

                case LinkInline link:
                    link.Url = UrlSegura(link.Url, link.IsImage);
                    if (!link.IsImage) MarcarExterno(link, link.Url);
                    break;

                case AutolinkInline auto:
                    auto.Url = UrlSegura(auto.Url, false) ?? "#";
                    MarcarExterno(auto, auto.IsEmail ? null : auto.Url);
                    break;
            }
        }

        using var sw = new StringWriter();
        var renderer = new HtmlRenderer(sw);
        Pipeline.Setup(renderer);
        renderer.ObjectRenderers.ReplaceOrAdd<CodeBlockRenderer>(new BlocoCodigoRenderer());
        renderer.Render(doc);
        sw.Flush();

        return new DocumentoMd(new MarkupString(sw.ToString()), titulos);
    }

    /// <summary>Primeira linha "limpa" do texto, sem a marcação, para a prévia da lista.</summary>
    public static string Previa(string? markdown, int max = 140)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";
        var doc = Markdown.Parse(markdown, Pipeline);
        foreach (var bloco in doc.Descendants<LeafBlock>())
        {
            var texto = bloco.Inline is not null
                ? TextoPuro(bloco.Inline)
                : bloco.Lines.ToString().Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "";
            texto = texto.Trim();
            if (texto.Length > 0) return texto.Length > max ? texto[..max] : texto;
        }
        return "";
    }

    private static string? UrlSegura(string? url, bool imagem)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (url.StartsWith('#')) return "#" + PrefixoId + url[1..];
        var m = EsquemaRx().Match(url);
        if (!m.Success) return url; // relativo (ex.: imagens/abc.png)
        var esquema = m.Groups[1].Value;
        if (EsquemasSeguros.Contains(esquema)) return url;
        if (imagem && esquema.Equals("data", StringComparison.OrdinalIgnoreCase) && url.TrimStart()[5..].StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return url;
        return "#"; // javascript:, vbscript: etc.
    }

    private static void MarcarExterno(MarkdownObject link, string? url)
    {
        if (url is null || url.StartsWith('#')) return;
        var attrs = link.GetAttributes();
        attrs.AddPropertyIfNotExist("target", "_blank");
        attrs.AddPropertyIfNotExist("rel", "noopener noreferrer");
    }

    private static string TextoPuro(ContainerInline? inline)
    {
        if (inline is null) return "";
        var sb = new StringBuilder();
        foreach (var i in inline.Descendants())
        {
            switch (i)
            {
                case LiteralInline l: sb.Append(l.Content.ToString()); break;
                case CodeInline c: sb.Append(c.Content); break;
                case LineBreakInline: sb.Append(' '); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Blocos de código com o mesmo realce e botão Copiar dos scripts.</summary>
    private sealed class BlocoCodigoRenderer : HtmlObjectRenderer<CodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock bloco)
        {
            var info = (bloco as FencedCodeBlock)?.Info?.Trim() ?? "";
            var codigo = bloco.Lines.ToString();

            renderer.EnsureLine();
            renderer.Write("<div class=\"codewrap md-code\"><div class=\"codebar\"><span>");
            renderer.WriteEscape(info);
            renderer.Write("</span><button type=\"button\" class=\"md-copiar\">Copiar</button></div><pre>");
            renderer.Write(Destaque.Html(codigo, Linguagem(info)).Value);
            renderer.Write("</pre></div>");
            renderer.EnsureLine();
        }

        private static string? Linguagem(string info) => info.ToLowerInvariant() switch
        {
            "sql" or "tsql" or "t-sql" or "mysql" or "plsql" or "pgsql" => "SQL",
            "powershell" or "ps" or "ps1" or "pwsh" => "PowerShell",
            "bash" or "sh" or "shell" or "zsh" => "Bash",
            "cmd" or "bat" or "batch" => "CMD",
            "c#" or "cs" or "csharp" => "C#",
            "js" or "javascript" or "ts" or "typescript" or "json" or "java" or "c" or "cpp" or "c++" or "go" => "JavaScript",
            "py" or "python" => "Python",
            _ => null
        };
    }
}
