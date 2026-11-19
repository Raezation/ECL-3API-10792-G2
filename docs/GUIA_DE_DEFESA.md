# Guia de demonstração e defesa

Grupo 2 · Jorge e Israel · UFCD 10792

Este guia propõe a organização da intervenção; não comprova tarefas já realizadas por cada formando. Ambos devem conseguir explicar a solução completa.

## Preparação

1. Extrair o projeto e executar os comandos do README. Abrir http://127.0.0.1:5082.
2. Confirmar o catálogo e os leitores. Numa base de dados nova existem cinco registos por entidade.
3. Ter abertos `Livro.cs`, `BibliotecaContext.cs`, `LivrosController.cs`, `EmprestimoService.cs` e `Views/Emprestimos/Atrasos.cshtml`.
4. Executar `dotnet test` antes da apresentação. Confirmar os resultados no próprio computador; não anunciar testes que não tenham sido executados nesse ambiente.

## Percurso de demonstração (cerca de 8 a 10 minutos)

| Intervenção sugerida | Ação | O que explicar |
|---|---|---|
| Jorge | Abrir o painel e o catálogo | MVC; dados persistidos; cinco livros e saldos |
| Jorge | Pesquisar por `Mistério`, depois `Pedro Lopes` | A mesma caixa pesquisa título, autor e categoria |
| Jorge | Criar um livro de teste | ISBN, título, autor, categoria e quantidade; validações |
| Jorge | Tentar repetir `9780000000002` | Normalização e índice único impedem duplicação |
| Israel | Criar e editar um leitor fictício | Número único, nome e email; proteção do formulário |
| Israel | Emprestar `Viagem ao Mundo Digital` | Guardar datas e leitor; disponibilidade diminui uma unidade |
| Israel | Tentar emprestar `A Biblioteca Esquecida` com saldo zero | O servidor recusa e a BD fica inalterada |
| Israel | Devolver o empréstimo acabado de criar | A data é registada pelo servidor; o saldo aumenta uma unidade |
| Ambos | Abrir Atrasos e devolver um dos atrasados | A consulta é dinâmica; o leitor deixa de aparecer após a devolução |
| Ambos | Mostrar os testes e a migration | Evidência técnica, relações, restrições e limites da solução |

Para criar um livro novo pode usar o ISBN fictício válido `9780000000057`, desde que ainda não exista. Se já existir, edite-o ou use outro ISBN com dígito de controlo válido. Uma demonstração altera dados; não espere que as quantidades continuem iguais às capturas após repetir operações.

## Perguntas e respostas justificadas

### 1. Qual é a diferença entre Model, View e Controller?

O Model representa os dados; por exemplo, `Livro` tem título e quantidade disponível. O Controller recebe o pedido, consulta ou altera os dados e escolhe a resposta; `LivrosController.Index` obtém os livros. A View recebe esses dados e produz HTML; `Views/Livros/Index.cshtml` percorre a lista para construir a tabela. A consulta não é escrita na vista, porque essa responsabilidade pertence ao servidor.

### 2. Onde ficam os dados depois de fechar a aplicação?

Ficam no ficheiro SQLite `biblioteca.db`. O `BibliotecaContext` usa o fornecedor SQLite do EF Core. Ao reiniciar, a aplicação volta a ligar-se ao ficheiro em vez de recriar uma lista de objetos. `DadosIniciais` só insere a demonstração quando as três tabelas estão vazias.

### 3. O que são chaves primárias e estrangeiras?

`Livro.Id`, `Leitor.Id` e `Emprestimo.Id` identificam unicamente cada registo nas respetivas tabelas. `Emprestimo.LivroId` e `Emprestimo.LeitorId` apontam para registos existentes de livro e leitor. As FKs impedem guardar um empréstimo que refira um livro inexistente. A relação é um-para-muitos: o mesmo livro pode aparecer em vários empréstimos, mas cada empréstimo corresponde a um livro.

### 4. Porque existe um ViewModel para o formulário?

`LivroForm` contém apenas os campos que o formulário deve aceitar, além do identificador e da versão. A entidade `Livro` também tem a coleção de empréstimos, que não deve ser alterada por esse formulário. O controlador copia explicitamente os campos permitidos, pelo que enviar campos adicionais não modifica o histórico.

### 5. Como é evitado um ISBN duplicado?

Primeiro removem-se espaços e hífenes, para que as duas representações do mesmo código fiquem iguais. Depois verifica-se a existência desse ISBN antes de guardar, para apresentar uma mensagem compreensível. Finalmente, o índice único da BD garante a regra mesmo que dois pedidos passem a verificação ao mesmo tempo. A validação do dígito de controlo deteta erros de escrita; não prova que a obra esteja oficialmente registada.

### 6. Porque não basta desativar o botão quando não há stock?

Um utilizador pode enviar um POST diretamente, sem usar o botão. Além disso, o saldo pode mudar entre a abertura e a submissão do formulário. Por isso, `RegistarAsync` executa um UPDATE cuja condição exige disponibilidade positiva. Só prossegue se esse UPDATE alterar uma linha.

### 7. Para que serve a transação?

Um empréstimo requer duas alterações: reduzir o saldo do livro e inserir o empréstimo. Se só uma fosse guardada, os dados ficariam incoerentes. A transação é confirmada apenas depois das duas operações. Se ocorrer erro ou a função regressar antes da confirmação, a transação é revertida.

### 8. O que acontece se duas pessoas tentarem o último exemplar?

O UPDATE exige `ExemplaresDisponiveis > 0`. Depois de um pedido reduzir 1 para 0, o outro já não consegue cumprir essa condição. A transação e a escrita condicionada impedem duas saídas para o mesmo saldo. O teste `DoisPedidosSimultaneos_NaoEmprestamOMesmoExemplar` verifica um único novo empréstimo e saldo final zero.

### 9. Como se impede contar uma devolução duas vezes?

A atualização do empréstimo exige `DataDevolucao == null`. O saldo só é incrementado depois de essa atualização alterar uma linha, e ambos os passos estão na mesma transação. Um segundo pedido encontra o empréstimo devolvido e não acrescenta outro exemplar.

### 10. Como é calculado um atraso?

O servidor seleciona empréstimos sem data de devolução cuja data prevista é anterior à data local atual. Um empréstimo previsto para hoje ainda não está atrasado. Um empréstimo devolvido não entra na lista, mesmo que a devolução tenha ocorrido depois do prazo. A consulta EF usa `Include` para trazer o livro e o leitor associados.

### 11. Para que serve Versao?

Um formulário pode ficar aberto enquanto outro pedido altera o mesmo registo. O POST envia a versão que o utilizador viu. O EF inclui essa versão na condição de atualização. Se entretanto tiver mudado, a atualização não afeta nenhuma linha e gera uma exceção de concorrência, tratada com uma mensagem. Os empréstimos e as devoluções também incrementam a versão do livro para uma edição antiga não repor um saldo desatualizado.

### 12. O que protege contra CSRF?

Os formulários POST incluem um token antiforgery gerado pelos Tag Helpers. O filtro global `AutoValidateAntiforgeryTokenAttribute` exige a correspondência entre o token submetido e a proteção do navegador. Um POST sem token recebe HTTP 400. As páginas de erro estão isentas porque não alteram dados. Isto não substitui autenticação, que não existe nesta versão.

### 13. Como se evita SQL injection e HTML inserido pelo utilizador?

As consultas e atualizações usam o EF Core, que passa valores como parâmetros; não há SQL construído por concatenação de texto recebido. Razor codifica por omissão os valores escritos com `@`. Por exemplo, texto com `<script>` é apresentado como texto, sem recorrer a `Html.Raw`.

### 14. Porque não posso apagar um livro que já foi devolvido?

O empréstimo continua a fazer parte do histórico. Apagar o livro deixaria de permitir identificar a obra desse registo. O controlador bloqueia a operação e a FK tem `DeleteBehavior.Restrict`. O mesmo princípio é aplicado ao leitor.

### 15. O que fazem as migrations?

Guardam as alterações do esquema em C#. A migration `Inicial` cria tabelas, chaves, índices e CHECK. `MigrateAsync()` aplica migrations ainda não executadas. O ficheiro SQL é uma exportação dessa migration. Os cinco registos de demonstração são criados separadamente por `DadosIniciais`.

### 16. Que limitações devem ser reconhecidas?

Não há login, perfis de acesso, multas, notificações, paginação nem registo individual de exemplares físicos. O saldo disponível é um contador corrigível no inventário. A pesquisa carrega o catálogo em memória e filtra-o em C#, o que é adequado aos poucos registos usados. A publicação no GitHub e os contributos efetivos dos dois formandos não são comprovados por um ZIP preparado com IA.

## Pequena alteração possível durante a defesa

Uma alteração simples é mudar o prazo inicial de empréstimo de 14 para 21 dias em `EmprestimosController.Criar` (GET). O valor é apenas a sugestão inicial do formulário; a validação da data prevista mantém-se. Guardar, recompilar, abrir um novo formulário e verificar a diferença. Ambos devem conseguir explicar por que motivo isso não altera os empréstimos antigos, cujas datas já estão guardadas na BD.
