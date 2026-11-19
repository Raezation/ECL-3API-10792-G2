using BibliotecaWeb.Data;
using BibliotecaWeb.Services;
using BibliotecaWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Controllers;

public class EmprestimosController(BibliotecaContext db, EmprestimoService servico) : Controller
{
    public async Task<IActionResult> Index(bool apenasAtivos = false)
    {
        var query = db.Emprestimos.Include(e => e.Livro).Include(e => e.Leitor).AsNoTracking();
        if (apenasAtivos) query = query.Where(e => e.DataDevolucao == null);
        ViewBag.ApenasAtivos = apenasAtivos;
        ViewBag.Hoje = servico.Hoje;
        return View(await query.OrderByDescending(e => e.Id).ToListAsync());
    }

    public async Task<IActionResult> Atrasos()
    {
        var hoje = servico.Hoje;
        // A seleção é feita no servidor e traduzida para SQL pelo EF Core.
        var atrasos = await db.Emprestimos.Include(e => e.Livro).Include(e => e.Leitor)
            .AsNoTracking().Where(e => e.DataDevolucao == null && e.DataPrevista < hoje)
            .OrderBy(e => e.DataPrevista).ToListAsync();
        ViewBag.Hoje = hoje;
        return View(atrasos);
    }

    [HttpGet]
    public async Task<IActionResult> Criar(int? livroId)
    {
        var form = new EmprestimoForm { LivroId=livroId ?? 0,
            DataInicio=servico.Hoje, DataPrevista=servico.Hoje.AddDays(14) };
        await PreencherListas(form);
        return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(EmprestimoForm form)
    {
        if (ModelState.IsValid)
        {
            var resultado = await servico.RegistarAsync(form.LivroId, form.LeitorId,
                form.DataInicio!.Value, form.DataPrevista!.Value);
            if (resultado.Sucesso)
            {
                TempData["Sucesso"] = resultado.Mensagem;
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", resultado.Mensagem);
        }
        await PreencherListas(form);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> Devolver(int id)
    {
        var emprestimo = await db.Emprestimos.Include(e => e.Livro).Include(e => e.Leitor)
            .AsNoTracking().SingleOrDefaultAsync(e => e.Id == id);
        return emprestimo is null ? NotFound() : View(emprestimo);
    }

    [HttpPost, ActionName("Devolver")]
    public async Task<IActionResult> DevolverConfirmado(int id)
    {
        if (!await db.Emprestimos.AnyAsync(e => e.Id == id)) return NotFound();
        var resultado = await servico.DevolverAsync(id);
        TempData[resultado.Sucesso ? "Sucesso" : "Erro"] = resultado.Mensagem;
        return RedirectToAction(nameof(Index));
    }

    private async Task PreencherListas(EmprestimoForm form)
    {
        var livros = await db.Livros.AsNoTracking().OrderBy(l => l.Titulo).ToListAsync();
        form.Livros = livros.Select(l => new SelectListItem(
            $"{l.Titulo} ({l.ExemplaresDisponiveis} disponíveis)", l.Id.ToString())).ToList();
        var leitores = await db.Leitores.AsNoTracking().OrderBy(l => l.Nome).ToListAsync();
        form.Leitores = leitores.Select(l => new SelectListItem(
            $"{l.Numero} - {l.Nome}", l.Id.ToString())).ToList();
    }
}
