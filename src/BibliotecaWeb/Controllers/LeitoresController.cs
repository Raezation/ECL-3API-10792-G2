using BibliotecaWeb.Data;
using BibliotecaWeb.Models;
using BibliotecaWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Controllers;

public class LeitoresController(BibliotecaContext db) : Controller
{
    public async Task<IActionResult> Index() =>
        View(await db.Leitores.AsNoTracking().OrderBy(l => l.Nome).ToListAsync());

    [HttpGet]
    public IActionResult Criar() => View("Formulario", new LeitorForm());

    [HttpPost]
    public async Task<IActionResult> Criar(LeitorForm form)
    {
        form.Numero = form.Numero?.Trim().ToUpperInvariant() ?? "";
        await VerificarNumero(form);
        if (!ModelState.IsValid) return View("Formulario", form);
        var leitor = new Leitor();
        Copiar(form, leitor);
        db.Leitores.Add(leitor);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Numero", "Não foi possível guardar. Confirme que o número não está repetido.");
            return View("Formulario", form);
        }
        TempData["Sucesso"] = "Leitor criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var leitor = await db.Leitores.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
        if (leitor is null) return NotFound();
        return View("Formulario", new LeitorForm { Id=leitor.Id, Versao=leitor.Versao,
            Nome=leitor.Nome, Numero=leitor.Numero, Email=leitor.Email });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, LeitorForm form)
    {
        if (id != form.Id) return BadRequest();
        var leitor = await db.Leitores.SingleOrDefaultAsync(l => l.Id == id);
        if (leitor is null) return NotFound();
        form.Numero = form.Numero?.Trim().ToUpperInvariant() ?? "";
        await VerificarNumero(form);
        if (!ModelState.IsValid) return View("Formulario", form);
        db.Entry(leitor).Property(l => l.Versao).OriginalValue = form.Versao;
        Copiar(form, leitor);
        leitor.Versao = form.Versao + 1;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "O leitor foi alterado entretanto. Reabra a edição antes de guardar.");
            return View("Formulario", form);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Numero", "Não foi possível guardar. Confirme que o número não está repetido.");
            return View("Formulario", form);
        }
        TempData["Sucesso"] = "Leitor atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Apagar(int id)
    {
        var leitor = await db.Leitores.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
        return leitor is null ? NotFound() : View(leitor);
    }

    [HttpPost, ActionName("Apagar")]
    public async Task<IActionResult> ApagarConfirmado(int id)
    {
        var leitor = await db.Leitores.SingleOrDefaultAsync(l => l.Id == id);
        if (leitor is null) return NotFound();
        if (await db.Emprestimos.AnyAsync(e => e.LeitorId == id))
        {
            TempData["Erro"] = "Não é possível apagar um leitor com histórico de empréstimos.";
            return RedirectToAction(nameof(Index));
        }
        db.Leitores.Remove(leitor);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "O leitor foi alterado ou tem empréstimos associados. Atualize a página.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Sucesso"] = "Leitor apagado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task VerificarNumero(LeitorForm form)
    {
        if (await db.Leitores.AnyAsync(l => l.Numero == form.Numero && l.Id != form.Id))
            ModelState.AddModelError("Numero", "Já existe um leitor com este número.");
    }
    private static void Copiar(LeitorForm f, Leitor l)
    {
        l.Nome=f.Nome.Trim(); l.Numero=f.Numero; l.Email=f.Email.Trim();
    }
}
