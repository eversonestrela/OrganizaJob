# OrganizaJob

Cofre pessoal para scripts, prints, lembretes, links e notas. Substitui o "chat aleatório do Teams".

## Como rodar

Precisa do **.NET 8 SDK** (`dotnet --version` deve mostrar 8.x).

- Dê dois cliques em `rodar.bat`, **ou**
- abra `OrganizaJob.sln` no Visual Studio e aperte F5, **ou**
- no terminal: `dotnet run --project src\OrganizaJob`

Abra http://localhost:5080. Na primeira execução o NuGet baixa o EF Core e o banco é criado sozinho.

## Onde ficam os dados

Por padrão em `src\OrganizaJob\App_Data\`:

- `organizajob.db`: banco SQLite com todos os itens
- `imagens\`: os prints

Para guardar em outro lugar (uma pasta do OneDrive, por exemplo), altere `OrganizaJob:PastaDados` em
`src\OrganizaJob\appsettings.json`. Aceita variáveis como `%USERPROFILE%\OneDrive\OrganizaJob`.
Backup = copiar essa pasta. A `App_Data` está no `.gitignore`.

## Uso

| Ação | Como |
|---|---|
| Novo item | `Ctrl+N` ou botão **+ Novo item** |
| Buscar | `Ctrl+K`. Ignora acentos e maiúsculas. `#sql` filtra pela tag |
| Salvar print | `Ctrl+V` em qualquer lugar da tela (abre um item novo já com a imagem), ou arrastar o arquivo |
| Salvar / cancelar | `Ctrl+Enter` / `Esc` |
| Variáveis em script | Escreva `{{servidor}}` no script. Na hora de copiar aparecem campos para preencher |
| Mais usados | Cada cópia conta um uso. Ordene por **Mais usados** na lista |
| Lembretes | Com data/hora e repetição semanal ou mensal. Clique em **Ativar avisos de lembrete** para receber notificação do Windows enquanto a página estiver aberta |

## Estrutura

```
src/OrganizaJob/
  Program.cs              configuração, endpoints de imagem
  Models/Item.cs          o item (um só modelo para os 5 tipos)
  Data/AppDbContext.cs    EF Core + SQLite
  Services/
    ItemService.cs        salvar, excluir, favoritos, usos, lembretes, busca
    ImagemService.cs      grava e serve os prints
    Destaque.cs           realce de sintaxe e variáveis {{ }}
  Components/
    Pages/Home.razor      tela principal (navegação, lista, detalhe)
    Shared/ItemDetalhe.razor
    Shared/ItemEditor.razor
  wwwroot/app.css, wwwroot/js/organizajob.js
```

O app só escuta em `localhost` e não tem login. Se um dia for para um servidor, é preciso acrescentar autenticação.

## Próximos passos possíveis

- Importar o histórico do chat do Teams
- Exportar/importar tudo em JSON
- Migrações do EF (hoje o banco é criado com `EnsureCreated`; se o modelo mudar, use migrations)
