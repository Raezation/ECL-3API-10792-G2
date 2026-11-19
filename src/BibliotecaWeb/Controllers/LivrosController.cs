using BibliotecaWeb.Data;
using BibliotecaWeb.Models;
using BibliotecaWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaWeb.Controllers;

public class LivrosController(BibliotecaContext db) : Controller
{
    public async Task<IActionResult> Index(string? pesquisa)
    {
        var livros = await db.Livros.AsNoTracking().OrderBy(l => l.Titulo).ToListAsync();
        pesquisa = pesquisa?.Trim();
        if (!string.IsNullOrEmpty(pesquisa))
        {
            // Pesquisa Unicode em C#, adequada ao pequeno catálogo escolar.
            livros = livros.Where(l => l.Titulo.Contains(pesquisa, StringComparison.OrdinalIgnoreCase)
                || l.Autor.Contains(pesquisa, StringComparison.OrdinalIgnoreCase)
                || l.Categoria.Contains(pesquisa, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.Pesquisa = pesquisa;
        return View(livros);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var livro = await db.Livros.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
        return livro is null ? NotFound() : View(livro);
    }

    [HttpGet]
    public IActionResult Criar() => View("Formulario", new LivroForm());

    [HttpPost]
    public async Task<IActionResult> Criar(LivroForm form)
    {
        form.Isbn = IsbnValidoAttribute.Normalizar(form.Isbn);
        await VerificarIsbn(form);
        if (!ModelState.IsValid) return View("Formulario", form);
        var livro = new Livro();
        Copiar(form, livro);
        db.Livros.Add(livro);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Isbn", "Não foi possível guardar. Confirme que o ISBN não está repetido.");
            return View("Formulario", form);
        }
        TempData["Sucesso"] = "Livro criado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var livro = await db.Livros.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
        if (livro is null) return NotFound();
        return View("Formulario", new LivroForm { Id=livro.Id, Versao=livro.Versao,
            Isbn=livro.Isbn, Titulo=livro.Titulo, Autor=livro.Autor,
            Categoria=livro.Categoria, ExemplaresDisponiveis=livro.ExemplaresDisponiveis });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, LivroForm form)
    {
        if (id != form.Id) return BadRequest();
        var livro = await db.Livros.SingleOrDefaultAsync(l => l.Id == id);
        if (livro is null) return NotFound();
        form.Isbn = IsbnValidoAttribute.Normalizar(form.Isbn);
        await VerificarIsbn(form);
        if (!ModelState.IsValid) return View("Formulario", form);
        db.Entry(livro).Property(l => l.Versao).OriginalValue = form.Versao;
        Copiar(form, livro);
        livro.Versao = form.Versao + 1;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "O livro foi alterado entretanto. Reabra a edição para consultar os dados atuais.");
            return View("Formulario", form);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("Isbn", "Não foi possível guardar. Confirme que o ISBN não está repetido.");
            return View("Formulario", form);
        }
        TempData["Sucesso"] = "Livro atualizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Apagar(int id)
    {
        var livro = await db.Livros.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
        return livro is null ? NotFound() : View(livro);
    }

    [HttpPost, ActionName("Apagar")]
    public async Task<IActionResult> ApagarConfirmado(int id)
    {
        var livro = await db.Livros.SingleOrDefaultAsync(l => l.Id == id);
        if (livro is null) return NotFound();
        if (await db.Emprestimos.AnyAsync(e => e.LivroId == id))
        {
            TempData["Erro"] = "Não é possível apagar um livro com histórico de empréstimos.";
            return RedirectToAction(nameof(Index));
        }
        db.Livros.Remove(livro);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            TempData["Erro"] = "O livro foi alterado ou tem empréstimos associados. Atualize a página.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Sucesso"] = "Livro apagado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task VerificarIsbn(LivroForm form)
    {
        if (await db.Livros.AnyAsync(l => l.Isbn == form.Isbn && l.Id != form.Id))
            ModelState.AddModelError("Isbn", "Já existe um livro com este ISBN.");
    }

    private static void Copiar(LivroForm f, Livro l)
    {
        l.Isbn=f.Isbn; l.Titulo=f.Titulo.Trim(); l.Autor=f.Autor.Trim();
        l.Categoria=f.Categoria.Trim(); l.ExemplaresDisponiveis=f.ExemplaresDisponiveis;
    }
}
