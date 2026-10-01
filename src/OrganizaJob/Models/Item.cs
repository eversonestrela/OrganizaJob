using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrganizaJob.Models;

public enum ItemTipo { Script, Imagem, Lembrete, Link, Nota }

public enum Repeticao { Nenhuma, Semanal, Mensal }

public class Item
{
    public int Id { get; set; }
    public ItemTipo Tipo { get; set; } = ItemTipo.Script;

    [MaxLength(200)]
    public string Titulo { get; set; } = "";

    /// <summary>Script, texto da nota, descrição da imagem ou observação.</summary>
    public string Conteudo { get; set; } = "";

    [MaxLength(40)]
    public string? Linguagem { get; set; }

    /// <summary>Nome do arquivo dentro da pasta de imagens.</summary>
    [MaxLength(100)]
    public string? ImagemArquivo { get; set; }

    [MaxLength(2000)]
    public string? Url { get; set; }

    public DateTime? DataLembrete { get; set; }
    public Repeticao Repetir { get; set; } = Repeticao.Nenhuma;
    public bool Concluido { get; set; }
    public DateTime? NotificadoEm { get; set; }

    public bool Favorito { get; set; }
    public int Usos { get; set; }
    public DateTime? UltimoUso { get; set; }

    /// <summary>Tags separadas por vírgula, sempre em minúsculas.</summary>
    [MaxLength(500)]
    public string Tags { get; set; } = "";

    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;

    [NotMapped]
    public IReadOnlyList<string> ListaTags =>
        Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    [NotMapped]
    public bool LembreteVencido =>
        Tipo == ItemTipo.Lembrete && !Concluido && DataLembrete is { } d && d <= DateTime.Now;

    public Item Clonar() => (Item)MemberwiseClone();
}

public static class ItemTipoInfo
{
    public static string Nome(this ItemTipo t) => t switch
    {
        ItemTipo.Script => "Script",
        ItemTipo.Imagem => "Imagem",
        ItemTipo.Lembrete => "Lembrete",
        ItemTipo.Link => "Link",
        _ => "Nota"
    };

    public static string Plural(this ItemTipo t) => t switch
    {
        ItemTipo.Script => "Scripts",
        ItemTipo.Imagem => "Imagens",
        ItemTipo.Lembrete => "Lembretes",
        ItemTipo.Link => "Links",
        _ => "Notas"
    };

    /// <summary>Classe CSS que define a cor do tipo.</summary>
    public static string Css(this ItemTipo t) => "t-" + t.ToString().ToLowerInvariant();

    public static readonly string[] Linguagens = ["SQL", "PowerShell", "Bash", "CMD", "C#", "JavaScript", "Python", "Outro"];
}
