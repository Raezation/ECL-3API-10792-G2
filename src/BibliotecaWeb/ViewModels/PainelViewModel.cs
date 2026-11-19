using BibliotecaWeb.Models;
namespace BibliotecaWeb.ViewModels;

public class PainelViewModel
{
    public int Livros { get; set; }
    public int Leitores { get; set; }
    public int Ativos { get; set; }
    public List<Emprestimo> Atrasos { get; set; } = [];
}
