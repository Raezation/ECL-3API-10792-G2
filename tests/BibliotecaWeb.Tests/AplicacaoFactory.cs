using BibliotecaWeb.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BibliotecaWeb.Tests;

public class AplicacaoFactory : WebApplicationFactory<Program>
{
    public static readonly DateTime Hoje = new(2026, 11, 19);
    private readonly string ficheiro = Path.Combine(Path.GetTempPath(), $"biblioteca-teste-{Guid.NewGuid()}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BibliotecaContext>>();
            services.RemoveAll<BibliotecaContext>();
            services.AddDbContext<BibliotecaContext>(options =>
                options.UseSqlite($"Data Source={ficheiro};Foreign Keys=True"));
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new RelogioFixo());
        });
    }
    public async Task<T> Consultar<T>(Func<BibliotecaContext, Task<T>> consulta)
    {
        using var scope = Services.CreateScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<BibliotecaContext>());
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(ficheiro); }
    }
    private class RelogioFixo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Hoje, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
