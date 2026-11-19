using BibliotecaWeb.Data;
using BibliotecaWeb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Services;

public record Resultado(bool Sucesso, string Mensagem, int? Id = null);

public class EmprestimoService(BibliotecaContext db, TimeProvider relogio)
{
    public DateTime Hoje => relogio.GetLocalNow().Date;

    public async Task<Resultado> RegistarAsync(int livroId, int leitorId, DateTime inicio, DateTime prevista)
    {
        inicio = inicio.Date;
        prevista = prevista.Date;
        if (inicio > Hoje) return new(false, "O empréstimo não pode começar no futuro.");
        if (prevista < inicio) return new(false, "A data prevista não pode ser anterior ao início.");
        try
        {
            // Uma transação mantém a redução de stock e o empréstimo inseparáveis.
            await using var transaction = await db.Database.BeginTransactionAsync();
            if (!await db.Leitores.AnyAsync(l => l.Id == leitorId))
                return new(false, "O leitor selecionado não existe.");
            if (!await db.Livros.AnyAsync(l => l.Id == livroId))
                return new(false, "O livro selecionado não existe.");
            var alterados = await db.Livros.Where(l => l.Id == livroId && l.ExemplaresDisponiveis > 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.ExemplaresDisponiveis, l => l.ExemplaresDisponiveis - 1)
                    .SetProperty(l => l.Versao, l => l.Versao + 1));
            if (alterados == 0) return new(false, "Não existem exemplares disponíveis deste livro.");
            var emprestimo = new Emprestimo { LivroId=livroId, LeitorId=leitorId,
                DataInicio=inicio, DataPrevista=prevista };
            db.Emprestimos.Add(emprestimo);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new(true, "Empréstimo registado com sucesso.", emprestimo.Id);
        }
        catch (Exception ex) when (ex is DbUpdateException or SqliteException)
        {
            return new(false, "Não foi possível guardar o empréstimo. Atualize a página e tente novamente.");
        }
    }

    public async Task<Resultado> DevolverAsync(int id)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var emprestimo = await db.Emprestimos.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
            if (emprestimo is null) return new(false, "O empréstimo não existe.");
            if (emprestimo.DataDevolucao.HasValue) return new(false, "Este empréstimo já foi devolvido.");
            if (emprestimo.DataInicio > Hoje) return new(false, "A devolução não pode ser anterior ao início.");
            var alterados = await db.Emprestimos.Where(e => e.Id == id && e.DataDevolucao == null)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.DataDevolucao, Hoje));
            if (alterados == 0) return new(false, "Este empréstimo já foi devolvido.");
            // O limite impede que uma correção manual do saldo cause overflow na devolução.
            var stock = await db.Livros.Where(l => l.Id == emprestimo.LivroId && l.ExemplaresDisponiveis < 10000)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.ExemplaresDisponiveis, l => l.ExemplaresDisponiveis + 1)
                    .SetProperty(l => l.Versao, l => l.Versao + 1));
            if (stock == 0) return new(false, "O saldo atingiu o limite. Corrija o stock antes de registar a devolução.");
            await transaction.CommitAsync();
            return new(true, "Devolução registada com sucesso.");
        }
        catch (Exception ex) when (ex is DbUpdateException or SqliteException)
        {
            return new(false, "Não foi possível guardar a devolução. Atualize a página e tente novamente.");
        }
    }
}
