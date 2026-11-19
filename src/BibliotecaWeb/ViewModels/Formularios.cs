using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BibliotecaWeb.ViewModels;

public class IsbnValidoAttribute : ValidationAttribute
{
    public IsbnValidoAttribute() => ErrorMessage = "Indique um ISBN-10 ou ISBN-13 válido, incluindo o dígito de controlo.";
    public static string Normalizar(string? value) =>
        Regex.Replace(value ?? "", @"[\s-]", "").ToUpperInvariant();
    public override bool IsValid(object? value)
    {
        var isbn = Normalizar(value as string);
        if (Regex.IsMatch(isbn, @"^\d{13}$"))
            return isbn.Select((c, i) => (c-'0') * (i % 2 == 0 ? 1 : 3)).Sum() % 10 == 0;
        if (Regex.IsMatch(isbn, @"^\d{9}[\dX]$"))
            return isbn.Select((c, i) => (c == 'X' ? 10 : c-'0') * (10-i)).Sum() % 11 == 0;
        return false;
    }
}

public class LivroForm
{
    public int Id { get; set; }
    public int Versao { get; set; }
    [Required(ErrorMessage="O ISBN é obrigatório."), StringLength(30), IsbnValido]
    [Display(Name="ISBN")] public string Isbn { get; set; } = "";
    [Required(ErrorMessage="O título é obrigatório."), StringLength(160)]
    [Display(Name="Título")] public string Titulo { get; set; } = "";
    [Required(ErrorMessage="O autor é obrigatório."), StringLength(120)]
    public string Autor { get; set; } = "";
    [Required(ErrorMessage="A categoria é obrigatória."), StringLength(60)]
    public string Categoria { get; set; } = "";
    [Range(0, 10000, ErrorMessage="A quantidade deve estar entre 0 e 10000.")]
    [Display(Name="Exemplares disponíveis")] public int ExemplaresDisponiveis { get; set; }
}

public class LeitorForm
{
    public int Id { get; set; }
    public int Versao { get; set; }
    [Required(ErrorMessage="O nome é obrigatório."), StringLength(120)]
    public string Nome { get; set; } = "";
    [Required(ErrorMessage="O número é obrigatório."), StringLength(20)]
    [Display(Name="Número")] public string Numero { get; set; } = "";
    [Required(ErrorMessage="O email é obrigatório."), StringLength(160)]
    [EmailAddress(ErrorMessage="Indique um endereço de email válido.")]
    public string Email { get; set; } = "";
}

public class EmprestimoForm : IValidatableObject
{
    [Range(1,int.MaxValue,ErrorMessage="Selecione um livro.")]
    [Display(Name="Livro")] public int LivroId { get; set; }
    [Range(1,int.MaxValue,ErrorMessage="Selecione um leitor.")]
    [Display(Name="Leitor")] public int LeitorId { get; set; }
    [Required,DataType(DataType.Date),Display(Name="Data de início")]
    public DateTime? DataInicio { get; set; }
    [Required,DataType(DataType.Date),Display(Name="Data prevista de devolução")]
    public DateTime? DataPrevista { get; set; }
    public List<SelectListItem> Livros { get; set; } = [];
    public List<SelectListItem> Leitores { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (DataInicio.HasValue && DataPrevista.HasValue && DataPrevista.Value.Date < DataInicio.Value.Date)
            yield return new("A data prevista não pode ser anterior ao início.", [nameof(DataPrevista)]);
    }
}
