using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrganizaJob.Data;
using OrganizaJob.Models;

namespace OrganizaJob.Services;

public enum Ordem { Recentes, MaisUsados, Alfabetica }

public class ItemService(IDbContextFactory<AppDbContext> fabrica, ImagemService imagens)
{
    public async Task<List<Item>> ListarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        return await db.Itens.AsNoTracking().ToListAsync();
    }

    public async Task<Item> SalvarAsync(Item item)
    {
        item.Titulo = item.Titulo.Trim();
        item.Tags = NormalizarTags(item.Tags);
        item.AtualizadoEm = DateTime.Now;
        if (item.Tipo != ItemTipo.Script) item.Linguagem = null;
        if (item.Tipo == ItemTipo.Lembrete && item.DataLembrete > DateTime.Now) item.NotificadoEm = null;

        await using var db = await fabrica.CreateDbContextAsync();
        if (item.Id == 0)
        {
            item.CriadoEm = DateTime.Now;
            db.Itens.Add(item);
        }
        else
        {
            var anterior = await db.Itens.AsNoTracking().FirstOrDefaultAsync(i => i.Id == item.Id);
            if (anterior?.ImagemArquivo is { } velha && velha != item.ImagemArquivo) imagens.Excluir(velha);
            db.Itens.Update(item);
        }
        await db.SaveChangesAsync();
        return item;
    }

    public async Task ExcluirAsync(int id)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Itens.FindAsync(id);
        if (item is null) return;
        db.Itens.Remove(item);
        await db.SaveChangesAsync();
        imagens.Excluir(item.ImagemArquivo);
    }

    private async Task AlterarAsync(int id, Action<Item> alteracao)
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var item = await db.Itens.FindAsync(id);
        if (item is null) return;
        alteracao(item);
        await db.SaveChangesAsync();
    }

    public Task RegistrarUsoAsync(int id) => AlterarAsync(id, i => { i.Usos++; i.UltimoUso = DateTime.Now; });

    public Task AlternarFavoritoAsync(int id) => AlterarAsync(id, i => i.Favorito = !i.Favorito);

    /// <summary>Conclui o lembrete. Se ele se repete, passa para a próxima data em vez de concluir.</summary>
    public Task ConcluirLembreteAsync(int id) => AlterarAsync(id, i =>
    {
        if (i.Concluido) { i.Concluido = false; return; }
        if (i.Repetir == Repeticao.Nenhuma || i.DataLembrete is null) { i.Concluido = true; return; }

        var data = i.DataLembrete.Value;
        do data = i.Repetir == Repeticao.Semanal ? data.AddDays(7) : data.AddMonths(1);
        while (data <= DateTime.Now);
        i.DataLembrete = data;
        i.NotificadoEm = null;
    });

    /// <summary>Lembretes que venceram e ainda não foram avisados. Já marca como avisados.</summary>
    public async Task<List<Item>> PegarLembretesParaAvisarAsync()
    {
        await using var db = await fabrica.CreateDbContextAsync();
        var pendentes = await db.Itens
            .Where(i => i.Tipo == ItemTipo.Lembrete && !i.Concluido && i.NotificadoEm == null && i.DataLembrete != null)
            .ToListAsync();
        var agora = DateTime.Now;
        var vencidos = pendentes.Where(i => i.DataLembrete <= agora).ToList();
        if (vencidos.Count == 0) return vencidos;
        foreach (var i in vencidos) i.NotificadoEm = agora;
        await db.SaveChangesAsync();
        return vencidos;
    }

    public static string NormalizarTags(string? tags) =>
        string.Join(",", (tags ?? "")
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.TrimStart('#').ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct());

    /// <summary>
    /// Filtra em memória. Cada palavra da busca precisa aparecer no item (sem diferenciar acento ou maiúscula).
    /// Palavras que começam com # procuram só nas tags.
    /// </summary>
    public static IEnumerable<Item> Filtrar(IEnumerable<Item> itens, string? busca, ItemTipo? tipo, bool soFavoritos, string? tag, Ordem ordem)
    {
        var q = itens;
        if (soFavoritos) q = q.Where(i => i.Favorito);
        if (tipo is { } t) q = q.Where(i => i.Tipo == t);
        if (!string.IsNullOrEmpty(tag)) q = q.Where(i => i.ListaTags.Contains(tag));

        var termos = (busca ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(SemAcento).ToArray();
        if (termos.Length > 0)
        {
            q = q.Where(i =>
            {
                var texto = SemAcento($"{i.Titulo} {i.Conteudo} {i.Linguagem} {i.Url} {i.Tags}");
                var tagsItem = i.ListaTags.Select(SemAcento).ToArray();
                return termos.All(termo => termo.StartsWith('#')
                    ? tagsItem.Any(x => x.StartsWith(termo[1..]))
                    : texto.Contains(termo));
            });
        }

        return ordem switch
        {
            Ordem.MaisUsados => q.OrderByDescending(i => i.Usos).ThenByDescending(i => i.UltimoUso ?? i.AtualizadoEm),
            Ordem.Alfabetica => q.OrderBy(i => i.Titulo, StringComparer.Create(new CultureInfo("pt-BR"), true)),
            _ => q.OrderByDescending(i => i.AtualizadoEm),
        };
    }

    public static string SemAcento(string texto)
    {
        var decomposto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString();
    }
}
