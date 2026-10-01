namespace OrganizaJob.Services;

public class ImagemService(PastasDados pastas)
{
    public const long TamanhoMaximo = 15 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/bmp"] = ".bmp",
    };

    public static bool TipoAceito(string? contentType) =>
        contentType is not null && Extensoes.ContainsKey(contentType.Split(';')[0].Trim());

    /// <summary>Grava a imagem e devolve o nome do arquivo gerado.</summary>
    public async Task<string> SalvarAsync(Stream conteudo, string contentType, CancellationToken ct = default)
    {
        var ext = Extensoes[contentType.Split(';')[0].Trim()];
        var nome = $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var caminho = Path.Combine(pastas.Imagens, nome);

        await using var arquivo = File.Create(caminho);
        var buffer = new byte[81920];
        long total = 0;
        int lidos;
        while ((lidos = await conteudo.ReadAsync(buffer, ct)) > 0)
        {
            total += lidos;
            if (total > TamanhoMaximo)
            {
                arquivo.Close();
                File.Delete(caminho);
                throw new InvalidOperationException("Imagem maior que 15 MB.");
            }
            await arquivo.WriteAsync(buffer.AsMemory(0, lidos), ct);
        }
        return nome;
    }

    /// <summary>Caminho completo, ou null se o nome for inválido ou o arquivo não existir.</summary>
    public string? Caminho(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome != Path.GetFileName(nome)) return null;
        var caminho = Path.Combine(pastas.Imagens, nome);
        return File.Exists(caminho) ? caminho : null;
    }

    public static string ContentType(string nome) => Path.GetExtension(nome).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        _ => "application/octet-stream"
    };

    public void Excluir(string? nome)
    {
        var caminho = Caminho(nome);
        if (caminho is not null) File.Delete(caminho);
    }
}
