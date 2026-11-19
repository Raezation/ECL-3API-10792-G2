using System.ComponentModel.DataAnnotations;

namespace BibliotecaWeb.Models;

public class Livro
{
    public int Id { get; set; }
    [Required, MaxLength(13)] public string Isbn { get; set; } = "";
    [Required, MaxLength(160)] public string Titulo { get; set; } = "";
    [Required, MaxLength(120)] public string Autor { get; set; } = "";
    [Required, MaxLength(60)] public string Categoria { get; set; } = "";
    [Range(0, 10000)] public int ExemplaresDisponiveis { get; set; }
    // Incrementada também pelos empréstimos e devoluções para detetar edições antigas.
    [ConcurrencyCheck] public int Versao { get; set; }
    public ICollection<Emprestimo> Emprestimos { get; set; } = new List<Emprestimo>();
}
