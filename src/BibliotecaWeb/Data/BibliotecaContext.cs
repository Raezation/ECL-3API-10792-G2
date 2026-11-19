using BibliotecaWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Data;

public class BibliotecaContext(DbContextOptions<BibliotecaContext> options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Leitor> Leitores => Set<Leitor>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Livro>().HasIndex(l => l.Isbn).IsUnique();
        model.Entity<Livro>().ToTable(t => t.HasCheckConstraint(
            "CK_Livros_Stock", "ExemplaresDisponiveis >= 0 AND ExemplaresDisponiveis <= 10000"));
        model.Entity<Leitor>().HasIndex(l => l.Numero).IsUnique();
        model.Entity<Emprestimo>().HasOne(e => e.Livro).WithMany(l => l.Emprestimos)
            .HasForeignKey(e => e.LivroId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Emprestimo>().HasOne(e => e.Leitor).WithMany(l => l.Emprestimos)
            .HasForeignKey(e => e.LeitorId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Emprestimo>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Emprestimos_Prazo", "DataPrevista >= DataInicio");
            t.HasCheckConstraint("CK_Emprestimos_Devolucao",
                "DataDevolucao IS NULL OR DataDevolucao >= DataInicio");
        });
        model.Entity<Emprestimo>().HasIndex(e => new { e.DataDevolucao, e.DataPrevista });
    }
}
