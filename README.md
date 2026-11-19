# Gestão de Biblioteca e Empréstimos

**Escola do Comércio de Lisboa · 3.º API · UFCD 10792**  
**Grupo 2: Jorge e Israel · Ano letivo 2026/2027**

Aplicação em C#, ASP.NET Core MVC, Razor e Entity Framework Core com SQLite. Permite gerir livros e leitores, pesquisar o catálogo, registar empréstimos e devoluções e consultar atrasos.

## Executar no computador

Pré-requisitos: .NET SDK **8**, navegador e acesso à Internet no primeiro restauro dos pacotes NuGet. Não é necessário instalar SQLite, SQL Server, Node.js ou ferramentas de front-end.

Extraia o ZIP e abra um terminal na pasta `ECL-3API-10792-G2`:

```bash
dotnet restore
dotnet run --project src/BibliotecaWeb --no-launch-profile --urls http://127.0.0.1:5082
```

Abra **http://127.0.0.1:5082**. Em Linux/macOS também pode usar `bash iniciar.sh`.

A primeira execução aplica a migration `Inicial`, cria `src/BibliotecaWeb/biblioteca.db` e insere cinco livros, cinco leitores e cinco empréstimos fictícios. A criação dos dados é feita por `DadosIniciais`, não pelo ficheiro SQL. Nas execuções seguintes os dados são mantidos. Não há login nem utilizadores de demonstração: esta versão destina-se à demonstração local ou a uma porta privada do Codespaces. Não existe envio de emails.

Os ISBN de teste têm dígito de controlo válido, mas são identificadores fictícios do exercício; não são uma atribuição editorial real. Os leitores e endereços `example.com` também são fictícios. O estado inicial inclui quatro empréstimos ativos, um devolvido e dois atrasados no dia da criação. As datas são calculadas relativamente à data local do servidor. Para demonstração em Portugal, configure o sistema com o fuso horário Europe/Lisbon.

## GitHub Codespaces

O ficheiro `.devcontainer/devcontainer.json` configura .NET 8 e a porta 5082. Depois de abrir um Codespace do repositório:

```bash
dotnet run --project src/BibliotecaWeb --no-launch-profile --urls http://0.0.0.0:5082
```

No painel **Ports**, mantenha a porta 5082 **Private** e selecione **Open in Browser**. Esta configuração de Codespaces é fornecida; os testes registados nesta entrega foram executados localmente, em Ubuntu e .NET 8.

## Compilar e testar

```bash
dotnet build -c Release
dotnet test --logger "trx;LogFileName=resultados.trx" --results-directory docs/testes
```

Os testes de integração usam a aplicação MVC e ficheiros SQLite temporários separados. Não alteram a base de dados da demonstração. Incluem CRUD, pesquisa, validação, CSRF, integridade referencial, disponibilidade, devolução repetida e dois pedidos simultâneos para o último exemplar. Consulte `docs/testes/RESUMO.md` e os resultados TRX.

## Base de dados e migrations

```bash
dotnet tool restore
dotnet ef database update --project src/BibliotecaWeb
dotnet ef migrations script --project src/BibliotecaWeb --output database/esquema.sql
```

As migrations estão em `src/BibliotecaWeb/Data/Migrations`; o esquema SQL está em `database/esquema.sql`. A aplicação também chama `MigrateAsync()` ao iniciar. O comando `database update` cria apenas o esquema; os dados fictícios são inseridos ao iniciar a aplicação.

A ligação é configurada em `appsettings.json` e pode ser substituída, por exemplo, pela variável `ConnectionStrings__Biblioteca`. As chaves locais de proteção dos formulários ficam em `src/BibliotecaWeb/App_Data/keys`. Esta pasta, os ficheiros SQLite, `bin` e `obj` estão excluídos do Git. As chaves de proteção e a base de dados de trabalho não fazem parte do ZIP de código.

## Funcionalidades

| Área | Operações |
|---|---|
| Catálogo | Criar, listar, consultar detalhes, editar e apagar livros sem histórico |
| Pesquisa | Filtrar por título, autor ou categoria, sem distinguir maiúsculas de minúsculas |
| Leitores | Criar, listar, editar e apagar leitores sem histórico |
| Empréstimos | Escolher livro/leitor e datas; reduzir um exemplar numa transação |
| Devoluções | Confirmar devolução; repor um exemplar uma única vez |
| Atrasos | Consultar empréstimos ativos com data prevista anterior a hoje e respetivos leitores |

## Regras e decisões

- O ISBN é normalizado, validado por dígito de controlo e protegido por índice único. O número de leitor é normalizado para maiúsculas e também é único.
- Não são permitidas quantidades negativas, datas previstas anteriores ao início nem empréstimos com início no futuro.
- A redução de stock é um `UPDATE` condicionado a stock positivo, dentro da mesma transação que guarda o empréstimo. A devolução é condicionada a `DataDevolucao == null`.
- O saldo disponível pode ser corrigido no formulário do livro para inventário/aquisição. Não representa o total de exemplares existentes; os exemplares emprestados não estão incluídos nesse saldo.
- Há um limite de 10000 exemplares disponíveis por livro. Se o saldo já estiver nesse limite, uma devolução é recusada e a transação é revertida; deve corrigir-se o inventário primeiro.
- Um livro ou leitor com histórico não pode ser apagado, mesmo depois da devolução, para preservar o registo.
- As edições verificam `Versao` para evitar substituir alterações recentes. Os ViewModels limitam os campos aceites. Os POST são protegidos por antiforgery e Razor codifica o texto apresentado.
- A pesquisa Unicode é aplicada no servidor em C# depois de carregar o catálogo, para funcionar com letras acentuadas. É adequada ao conjunto pequeno deste trabalho; um catálogo grande precisaria de pesquisa e paginação na base de dados.

## Organização

```text
src/BibliotecaWeb/       Aplicação MVC, dados, serviços e migrations
tests/BibliotecaWeb.Tests/  Testes de integração
database/esquema.sql     Esquema gerado a partir da migration
docs/                   Relatório, esquema, guia de defesa e evidências reais
.devcontainer/          Configuração para Codespaces
```

## Documentação e apresentação

- `docs/Relatorio_Grupo_02_Jorge_Israel.pdf`: relatório técnico com requisitos, modelação, implementação, segurança, testes e capturas.
- `docs/GUIA_DE_DEFESA.md`: percurso da demonstração, divisão sugerida de intervenção e questões técnicas explicadas.
- `docs/MODELO_DADOS.md`: entidades, relações e restrições.
- `docs/ENTREGA_GITHUB.md`: publicação e registo de contribuições reais.

## Autoria, IA e estado da publicação

Esta versão foi preparada com apoio de IA (Codex), incluindo código, testes, documentação e relatório. O histórico local identifica explicitamente essa preparação; não atribui commits artificiais a Jorge ou Israel. Ambos devem rever, compreender e adaptar a solução, registar contribuições efetivas nas suas contas e confirmar o conteúdo do relatório antes da entrega.

Repositório privado: [Raezation/ECL-3API-10792-G2](https://github.com/Raezation/ECL-3API-10792-G2). A publicação foi efetuada com apoio de IA através da conta autorizada. O histórico original da preparação (três commits identificados como Codex) está preservado em `docs/historico-preparacao.bundle`; os commits de publicação na conta Raezation não são apresentados como prova de trabalho autónomo dos formandos. Continuam pendentes a identificação do colaborador Jorge, o acesso do formador e as contribuições efetivas de ambos.

## Referências

- [Microsoft Learn — CRUD com MVC e EF Core](https://learn.microsoft.com/en-us/aspnet/core/data/ef-mvc/crud?view=aspnetcore-8.0)
- [Microsoft Learn — Validação de modelos](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-8.0)
- [Microsoft Learn — Transações no EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Microsoft Learn — Proteção antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0)
