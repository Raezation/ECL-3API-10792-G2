# Resultados dos testes

Execução real em 19/11/2026, com .NET 8, xUnit, WebApplicationFactory e SQLite em ficheiros temporários.

**21 testes aprovados, 0 falhados, 0 ignorados.**

Os testes fixam a data em 19/11/2026 para resultados repetíveis. A aplicação de demonstração usa a data local do servidor.

| Teste | Estado |
|---|---|
| EmprestimoSemStock_BloqueadoSemAlterarDados | Passed |
| DadosIniciais_CincoRegistosPorEntidade | Passed |
| Pesquisa_PorTituloAutorECategoria(pesquisa: "Tecnologia", titulo: "Viagem ao Mundo Digital") | Passed |
| Csrf_PedidoSemTokenERecusado | Passed |
| Livros_CRUDCompletoEPersistencia | Passed |
| LivroInvalido_NaoEGuardado(isbn: "9780000000057", titulo: "Teste", stock: -1) | Passed |
| IsbnDuplicado_NormalizaEspacosEHifenes | Passed |
| DoisPedidosSimultaneos_NaoEmprestamOMesmoExemplar | Passed |
| RegistosInexistentes_Devolvem404 | Passed |
| Pesquisa_PorTituloAutorECategoria(pesquisa: "Pedro Lopes", titulo: "Programar o Futuro") | Passed |
| LivroInvalido_NaoEGuardado(isbn: "9780000000057", titulo: "", stock: 2) | Passed |
| EliminarComHistorico_PreservaLivroELeitor | Passed |
| EdicaoAntiga_NaoAnulaEmprestimo | Passed |
| Leitores_CriarEditarApagar | Passed |
| EmprestimoEDevolucao_AtualizamStockUmaVez | Passed |
| Leitor_NumeroDuplicadoEEmailInvalido | Passed |
| BaseDeDados_ImpoeUnicidadeEChavesEstrangeiras | Passed |
| LivroInvalido_NaoEGuardado(isbn: "9780000000058", titulo: "Teste", stock: 2) | Passed |
| Pesquisa_PorTituloAutorECategoria(pesquisa: "MISTÉRIOS", titulo: "A Rede dos Mistérios") | Passed |
| DatasInvalidas_EReferenciasInexistentes_Bloqueadas | Passed |
| Atrasos_ConsultaRealExcluiDevolvidosEPrazoDeHoje | Passed |

## Evidência de correção

`antes-correcao.trx` conserva a primeira execução: 20 passaram e um teste falhou por tentar obter um novo token num ecrã de empréstimo já devolvido, onde corretamente não existe formulário. O teste foi corrigido para simular a repetição do POST com um token válido, mantendo a verificação de que o saldo só aumenta uma vez. `resultados.trx` regista a execução final com os 21 testes aprovados.

## Verificação pelo navegador

Foram abertos o painel, catálogo, leitores, empréstimos e atrasos. A tentativa de emprestar o livro 3, sem stock, foi recusada. Foi criado um empréstimo para o livro 4 e depois devolvido: o saldo regressou a 3. As capturas estão em `docs/imagens`. O histórico do ecrã passou de 5 para 6 empréstimos por causa dessa demonstração; a base de dados de trabalho não é distribuída.
