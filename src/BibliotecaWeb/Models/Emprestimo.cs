using System.ComponentModel.DataAnnotations;

namespace BibliotecaWeb.Models;

public class Emprestimo
{
    public int Id { get; set; }
    public int LivroId { get; set; }
    public Livro Livro { get; set; } = null!;
    public int LeitorId { get; set; }
    public Leitor Leitor { get; set; } = null!;
    [DataType(DataType.Date)] public DateTime DataInicio { get; set; }
    [DataType(DataType.Date)] public DateTime DataPrevista { get; set; }
    [DataType(DataType.Date)] public DateTime? DataDevolucao { get; set; }
}
