using Microsoft.EntityFrameworkCore;
using OrganizaJob.Components;
using OrganizaJob.Data;
using OrganizaJob.Services;

var builder = WebApplication.CreateBuilder(args);

// Pasta dos dados: appsettings.json -> OrganizaJob:PastaDados. Vazio = pasta App_Data ao lado do projeto.
var pastaConfig = builder.Configuration["OrganizaJob:PastaDados"];
var pastas = new PastasDados(string.IsNullOrWhiteSpace(pastaConfig)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data")
    : Environment.ExpandEnvironmentVariables(pastaConfig));

builder.Services.AddSingleton(pastas);
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={pastas.Banco}"));
builder.Services.AddSingleton<ImagemService>();
builder.Services.AddSingleton<ItemService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var fabrica = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = fabrica.CreateDbContext();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

// Recebe a imagem colada/arrastada (corpo bruto) e devolve o nome do arquivo salvo.
app.MapPost("/api/imagens", async (HttpRequest req, ImagemService imagens, CancellationToken ct) =>
{
    if (!ImagemService.TipoAceito(req.ContentType))
        return Results.BadRequest("Formato não suportado. Use PNG, JPG, GIF, WEBP ou BMP.");
    if (req.ContentLength > ImagemService.TamanhoMaximo)
        return Results.BadRequest("Imagem maior que 15 MB.");
    try
    {
        var nome = await imagens.SalvarAsync(req.Body, req.ContentType!, ct);
        return Results.Ok(new { arquivo = nome });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.MapGet("/imagens/{arquivo}", (string arquivo, ImagemService imagens) =>
{
    var caminho = imagens.Caminho(arquivo);
    return caminho is null ? Results.NotFound() : Results.File(caminho, ImagemService.ContentType(arquivo));
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Logger.LogInformation("Dados em {Pasta}", pastas.Raiz);
app.Run();
