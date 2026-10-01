namespace OrganizaJob.Services;

/// <summary>Onde ficam o banco e as imagens. Configurável em appsettings.json (OrganizaJob:PastaDados).</summary>
public class PastasDados
{
    public string Raiz { get; }
    public string Imagens { get; }
    public string Banco { get; }

    public PastasDados(string raiz)
    {
        Raiz = Path.GetFullPath(raiz);
        Imagens = Path.Combine(Raiz, "imagens");
        Banco = Path.Combine(Raiz, "organizajob.db");
        Directory.CreateDirectory(Imagens);
    }
}
