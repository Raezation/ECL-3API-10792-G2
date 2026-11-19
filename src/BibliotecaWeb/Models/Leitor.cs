using System.ComponentModel.DataAnnotations;

namespace BibliotecaWeb.Models;

public class Leitor
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Nome { get; set; } = "";
    [Required, MaxLength(20)] public string Numero { get; set; } = "";
    [Required, MaxLength(160), EmailAddress] public string Email { get; set; } = "";
    [ConcurrencyCheck] public int Versao { get; set; }
    public ICollection<Emprestimo> Emprestimos { get; set; } = new List<Emprestimo>();
}
