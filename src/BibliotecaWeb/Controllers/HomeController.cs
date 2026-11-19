using BibliotecaWeb.Data;
using BibliotecaWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Controllers;

public class HomeController(BibliotecaContext db, TimeProvider relogio) : Controller
{
    public async Task<IActionResult> Index()
    {
        var hoje = relogio.GetLocalNow().Date;
        var painel = new PainelViewModel
        {
            Livros = await db.Livros.CountAsync(), Leitores = await db.Leitores.CountAsync(),
            Ativos = await db.Emprestimos.CountAsync(e => e.DataDevolucao == null),
            Atrasos = await db.Emprestimos.Include(e => e.Livro).Include(e => e.Leitor)
                .AsNoTracking().Where(e => e.DataDevolucao == null && e.DataPrevista < hoje)
                .OrderBy(e => e.DataPrevista).ToListAsync()
        };
        return View(painel);
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Erro()
    {
        Response.StatusCode = 500;
        return View("Estado", 500);
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Estado(int codigo)
    {
        if (codigo < 400 || codigo > 599) codigo = 404;
        Response.StatusCode = codigo;
        return View(codigo);
    }
}
