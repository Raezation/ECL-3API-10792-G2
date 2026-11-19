using System.Globalization;
using BibliotecaWeb.Data;
using BibliotecaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<BibliotecaContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Biblioteca")
        ?? "Data Source=biblioteca.db;Foreign Keys=True"));
builder.Services.AddScoped<EmprestimoService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")));

var app = builder.Build();
var cultura = new CultureInfo("pt-PT");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});
app.UseExceptionHandler("/Home/Erro");
app.UseStatusCodePagesWithReExecute("/Home/Estado", "?codigo={0}");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BibliotecaContext>();
    await db.Database.MigrateAsync();
    await DadosIniciais.InserirAsync(db,
        scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

await app.RunAsync();
public partial class Program { }
