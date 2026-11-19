using BibliotecaWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Data;

public static class DadosIniciais
{
    public static async Task InserirAsync(BibliotecaContext db, TimeProvider relogio)
    {
        // Executar apenas numa base de dados totalmente vazia; nunca repor dados apagados.
        if (await db.Livros.AnyAsync() || await db.Leitores.AnyAsync()
            || await db.Emprestimos.AnyAsync()) return;
        var hoje = relogio.GetLocalNow().Date;
        var livros = new[]
        {
            new Livro { Isbn="9780000000002", Titulo="A Rede dos Mistérios", Autor="Ana Martins", Categoria="Mistério", ExemplaresDisponiveis=2 },
            new Livro { Isbn="9780000000019", Titulo="Programar o Futuro", Autor="Pedro Lopes", Categoria="Tecnologia", ExemplaresDisponiveis=1 },
            new Livro { Isbn="9780000000026", Titulo="A Biblioteca Esquecida", Autor="Sofia Ferreira", Categoria="Mistério", ExemplaresDisponiveis=0 },
            new Livro { Isbn="9780000000033", Titulo="Viagem ao Mundo Digital", Autor="João Costa", Categoria="Tecnologia", ExemplaresDisponiveis=3 },
            new Livro { Isbn="9780000000040", Titulo="O Meu Primeiro Projeto MVC", Autor="Marta Rodrigues", Categoria="Programação", ExemplaresDisponiveis=2 }
        };
        var leitores = new[]
        {
            new Leitor { Nome="Ana Exemplo", Numero="L001", Email="ana@example.com" },
            new Leitor { Nome="Bruno Exemplo", Numero="L002", Email="bruno@example.com" },
            new Leitor { Nome="Carla Exemplo", Numero="L003", Email="carla@example.com" },
            new Leitor { Nome="Diogo Exemplo", Numero="L004", Email="diogo@example.com" },
            new Leitor { Nome="Eva Exemplo", Numero="L005", Email="eva@example.com" }
        };
        for (int i = 0; i < 5; i++)
            db.Emprestimos.Add(new Emprestimo
            {
                Livro = livros[i], Leitor = leitores[i], DataInicio = hoje.AddDays(-20),
                DataPrevista = i < 2 ? hoje.AddDays(-5-i) : hoje.AddDays(7),
                DataDevolucao = i == 4 ? hoje.AddDays(-2) : null
            });
        // Os saldos acima já descontam os quatro empréstimos ativos.
        await db.SaveChangesAsync();
    }
}
