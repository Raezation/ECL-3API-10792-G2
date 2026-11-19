using System.Net;
using System.Text.RegularExpressions;
using BibliotecaWeb.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BibliotecaWeb.Tests;

public class IntegracaoTests : IDisposable
{
    private readonly AplicacaoFactory app = new();
    private readonly HttpClient client;
    public IntegracaoTests() => client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
    public void Dispose() { client.Dispose(); app.Dispose(); }

    private static string Token(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private async Task<HttpResponseMessage> Post(string pagina, string destino, Dictionary<string,string> dados)
    {
        var html = await client.GetStringAsync(pagina);
        dados["__RequestVerificationToken"] = Token(html);
        Assert.NotEmpty(dados["__RequestVerificationToken"]);
        return await client.PostAsync(destino, new FormUrlEncodedContent(dados));
    }
    private static Dictionary<string,string> Livro(string isbn="9780000000057", string titulo="Livro de teste", int stock=2) =>
        new() { ["Isbn"]=isbn, ["Titulo"]=titulo, ["Autor"]="Autor fictício", ["Categoria"]="Teste", ["ExemplaresDisponiveis"]=stock.ToString() };
    private static Dictionary<string,string> Emprestimo(int livro=1, int leitor=1, int prazo=14) =>
        new() { ["LivroId"]=livro.ToString(), ["LeitorId"]=leitor.ToString(),
            ["DataInicio"]=AplicacaoFactory.Hoje.ToString("yyyy-MM-dd"),
            ["DataPrevista"]=AplicacaoFactory.Hoje.AddDays(prazo).ToString("yyyy-MM-dd") };
    private async Task<string> Texto(HttpResponseMessage response) => WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task DadosIniciais_CincoRegistosPorEntidade()
    {
        Assert.Equal((5,5,5), await app.Consultar(async db =>
            (await db.Livros.CountAsync(), await db.Leitores.CountAsync(), await db.Emprestimos.CountAsync())));
        Assert.Contains("Jorge e Israel", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task Livros_CRUDCompletoEPersistencia()
    {
        var criar = await Post("/Livros/Criar", "/Livros/Criar", Livro());
        Assert.Equal(HttpStatusCode.Redirect, criar.StatusCode);
        var livro = await app.Consultar(db => db.Livros.AsNoTracking().SingleAsync(l => l.Isbn=="9780000000057"));
        Assert.Contains("Livro de teste", await client.GetStringAsync($"/Livros/Detalhes/{livro.Id}"));
        var dados = Livro(titulo:"Título atualizado"); dados["Id"]=livro.Id.ToString(); dados["Versao"]=livro.Versao.ToString();
        Assert.Equal(HttpStatusCode.Redirect, (await Post($"/Livros/Editar/{livro.Id}",$"/Livros/Editar/{livro.Id}",dados)).StatusCode);
        // Uma nova ligação à mesma BD confirma persistência, sem usar objetos em memória do pedido.
        Assert.Equal("Título atualizado", await app.Consultar(db=>db.Livros.Where(l=>l.Id==livro.Id).Select(l=>l.Titulo).SingleAsync()));
        Assert.Equal(HttpStatusCode.Redirect,(await Post($"/Livros/Apagar/{livro.Id}",$"/Livros/Apagar/{livro.Id}",new())).StatusCode);
        Assert.False(await app.Consultar(db=>db.Livros.AnyAsync(l=>l.Id==livro.Id)));
    }

    [Fact]
    public async Task IsbnDuplicado_NormalizaEspacosEHifenes()
    {
        var resposta=await Post("/Livros/Criar","/Livros/Criar",Livro("978-0-0000-0000-2"));
        Assert.Equal(HttpStatusCode.OK,resposta.StatusCode);
        Assert.Contains("Já existe um livro com este ISBN",await Texto(resposta));
        Assert.Equal(5,await app.Consultar(db=>db.Livros.CountAsync()));
    }

    [Theory]
    [InlineData("9780000000058","Teste",2)]
    [InlineData("9780000000057","",2)]
    [InlineData("9780000000057","Teste",-1)]
    public async Task LivroInvalido_NaoEGuardado(string isbn,string titulo,int stock)
    {
        Assert.Equal(HttpStatusCode.OK,(await Post("/Livros/Criar","/Livros/Criar",Livro(isbn,titulo,stock))).StatusCode);
        Assert.Equal(5,await app.Consultar(db=>db.Livros.CountAsync()));
    }

    [Fact]
    public async Task Leitores_CriarEditarApagar()
    {
        var dados=new Dictionary<string,string> { ["Nome"]="Leitor de teste",["Numero"]="l006",["Email"]="teste@example.com" };
        Assert.Equal(HttpStatusCode.Redirect,(await Post("/Leitores/Criar","/Leitores/Criar",dados)).StatusCode);
        var l=await app.Consultar(db=>db.Leitores.AsNoTracking().SingleAsync(l=>l.Numero=="L006"));
        dados["Id"]=l.Id.ToString();dados["Versao"]=l.Versao.ToString();dados["Nome"]="Nome atualizado";
        Assert.Equal(HttpStatusCode.Redirect,(await Post($"/Leitores/Editar/{l.Id}",$"/Leitores/Editar/{l.Id}",dados)).StatusCode);
        Assert.Equal("Nome atualizado",await app.Consultar(db=>db.Leitores.Where(x=>x.Id==l.Id).Select(x=>x.Nome).SingleAsync()));
        await Post($"/Leitores/Apagar/{l.Id}",$"/Leitores/Apagar/{l.Id}",new());
        Assert.False(await app.Consultar(db=>db.Leitores.AnyAsync(x=>x.Id==l.Id)));
    }

    [Fact]
    public async Task Leitor_NumeroDuplicadoEEmailInvalido()
    {
        var dados=new Dictionary<string,string>{["Nome"]="Teste",["Numero"]=" l001 ",["Email"]="invalido"};
        var resposta=await Post("/Leitores/Criar","/Leitores/Criar",dados);
        Assert.Contains("Já existe um leitor com este número",await Texto(resposta));
        Assert.Contains("email válido",await Texto(resposta));
        Assert.Equal(5,await app.Consultar(db=>db.Leitores.CountAsync()));
    }

    [Fact]
    public async Task EmprestimoSemStock_BloqueadoSemAlterarDados()
    {
        var resposta=await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo(livro:3));
        Assert.Contains("Não existem exemplares disponíveis",await Texto(resposta));
        Assert.Equal(5,await app.Consultar(db=>db.Emprestimos.CountAsync()));
        Assert.Equal(0,await app.Consultar(db=>db.Livros.Where(l=>l.Id==3).Select(l=>l.ExemplaresDisponiveis).SingleAsync()));
    }

    [Fact]
    public async Task EmprestimoEDevolucao_AtualizamStockUmaVez()
    {
        Assert.Equal(HttpStatusCode.Redirect,(await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo())).StatusCode);
        Assert.Equal(1,await app.Consultar(db=>db.Livros.Where(l=>l.Id==1).Select(l=>l.ExemplaresDisponiveis).SingleAsync()));
        int id=await app.Consultar(db=>db.Emprestimos.MaxAsync(e=>e.Id));
        await Post($"/Emprestimos/Devolver/{id}",$"/Emprestimos/Devolver/{id}",new());
        // Simular uma repetição do POST com token válido; o ecrã de um registo
        // já devolvido não contém um novo formulário de devolução.
        var token=Token(await client.GetStringAsync("/Emprestimos/Criar"));
        var repetido=await client.PostAsync($"/Emprestimos/Devolver/{id}",new FormUrlEncodedContent(
            new Dictionary<string,string>{{"__RequestVerificationToken",token}}));
        Assert.Equal(HttpStatusCode.Redirect,repetido.StatusCode);
        Assert.Equal(2,await app.Consultar(db=>db.Livros.Where(l=>l.Id==1).Select(l=>l.ExemplaresDisponiveis).SingleAsync()));
        Assert.Equal(AplicacaoFactory.Hoje,await app.Consultar(db=>db.Emprestimos.Where(e=>e.Id==id).Select(e=>e.DataDevolucao).SingleAsync()));
    }

    [Fact]
    public async Task DatasInvalidas_EReferenciasInexistentes_Bloqueadas()
    {
        var prazo=await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo(prazo:-1));
        Assert.Contains("anterior ao início",await Texto(prazo));
        var futuro=Emprestimo();futuro["DataInicio"]=AplicacaoFactory.Hoje.AddDays(1).ToString("yyyy-MM-dd");
        Assert.Contains("começar no futuro",await Texto(await Post("/Emprestimos/Criar","/Emprestimos/Criar",futuro)));
        Assert.Contains("leitor selecionado não existe",await Texto(await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo(leitor:999))));
        Assert.Contains("livro selecionado não existe",await Texto(await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo(livro:999))));
        Assert.Equal(5,await app.Consultar(db=>db.Emprestimos.CountAsync()));
    }

    [Fact]
    public async Task Atrasos_ConsultaRealExcluiDevolvidosEPrazoDeHoje()
    {
        await app.Consultar(async db=>
        {
            var devolvido=await db.Emprestimos.SingleAsync(e=>e.Id==5);
            devolvido.DataPrevista=AplicacaoFactory.Hoje.AddDays(-1);
            var ativo=await db.Emprestimos.SingleAsync(e=>e.Id==3);
            ativo.DataPrevista=AplicacaoFactory.Hoje;
            return await db.SaveChangesAsync();
        });
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/Emprestimos/Atrasos"));
        Assert.Contains("Ana Exemplo",html);Assert.Contains("Bruno Exemplo",html);
        Assert.DoesNotContain("Eva Exemplo",html);Assert.DoesNotContain("Carla Exemplo",html);
        await Post("/Emprestimos/Devolver/1","/Emprestimos/Devolver/1",new());
        html=WebUtility.HtmlDecode(await client.GetStringAsync("/Emprestimos/Atrasos"));
        Assert.DoesNotContain("Ana Exemplo",html);
    }

    [Theory]
    [InlineData("MISTÉRIOS","A Rede dos Mistérios")]
    [InlineData("Pedro Lopes","Programar o Futuro")]
    [InlineData("Tecnologia","Viagem ao Mundo Digital")]
    public async Task Pesquisa_PorTituloAutorECategoria(string pesquisa,string titulo)
    {
        var html=WebUtility.HtmlDecode(await client.GetStringAsync("/Livros?pesquisa="+Uri.EscapeDataString(pesquisa)));
        Assert.Contains(titulo,html);Assert.DoesNotContain("O Meu Primeiro Projeto MVC",html);
    }

    [Fact]
    public async Task EliminarComHistorico_PreservaLivroELeitor()
    {
        await Post("/Livros/Apagar/1","/Livros/Apagar/1",new());
        await Post("/Leitores/Apagar/1","/Leitores/Apagar/1",new());
        Assert.True(await app.Consultar(db=>db.Livros.AnyAsync(l=>l.Id==1)));
        Assert.True(await app.Consultar(db=>db.Leitores.AnyAsync(l=>l.Id==1)));
    }

    [Fact]
    public async Task Csrf_PedidoSemTokenERecusado()
    {
        var resposta=await client.PostAsync("/Livros/Criar",new FormUrlEncodedContent(Livro()));
        Assert.Equal(HttpStatusCode.BadRequest,resposta.StatusCode);
        Assert.Equal(5,await app.Consultar(db=>db.Livros.CountAsync()));
    }

    [Fact]
    public async Task RegistosInexistentes_Devolvem404()
    {
        foreach(var url in new[]{"/Livros/Detalhes/999","/Livros/Editar/999","/Leitores/Editar/999","/Emprestimos/Devolver/999"})
            Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task EdicaoAntiga_NaoAnulaEmprestimo()
    {
        await client.GetAsync("/Livros/Editar/1");
        await Post("/Emprestimos/Criar","/Emprestimos/Criar",Emprestimo());
        var dados=Livro("9780000000002","Alteração antiga",2);dados["Id"]="1";dados["Versao"]="0";
        var resposta=await Post("/Livros/Editar/1","/Livros/Editar/1",dados);
        Assert.Contains("alterado entretanto",await Texto(resposta));
        Assert.Equal(1,await app.Consultar(db=>db.Livros.Where(l=>l.Id==1).Select(l=>l.ExemplaresDisponiveis).SingleAsync()));
    }

    [Fact]
    public async Task DoisPedidosSimultaneos_NaoEmprestamOMesmoExemplar()
    {
        var token=Token(await client.GetStringAsync("/Emprestimos/Criar"));
        var a=Emprestimo(livro:2,leitor:1);a["__RequestVerificationToken"]=token;
        var b=Emprestimo(livro:2,leitor:2);b["__RequestVerificationToken"]=token;
        var respostas=await Task.WhenAll(client.PostAsync("/Emprestimos/Criar",new FormUrlEncodedContent(a)),
            client.PostAsync("/Emprestimos/Criar",new FormUrlEncodedContent(b)));
        Assert.Single(respostas, r=>r.StatusCode==HttpStatusCode.Redirect);
        Assert.Equal(0,await app.Consultar(db=>db.Livros.Where(l=>l.Id==2).Select(l=>l.ExemplaresDisponiveis).SingleAsync()));
        Assert.Equal(6,await app.Consultar(db=>db.Emprestimos.CountAsync()));
    }

    [Fact]
    public async Task BaseDeDados_ImpoeUnicidadeEChavesEstrangeiras()
    {
        await Assert.ThrowsAsync<DbUpdateException>(()=>app.Consultar(async db=>
        {
            db.Livros.Add(new Livro{Isbn="9780000000002",Titulo="Repetido",Autor="Teste",Categoria="Teste"});
            return await db.SaveChangesAsync();
        }));
        await Assert.ThrowsAsync<DbUpdateException>(()=>app.Consultar(async db=>
        {
            db.Emprestimos.Add(new Emprestimo{LivroId=999,LeitorId=1,DataInicio=AplicacaoFactory.Hoje,DataPrevista=AplicacaoFactory.Hoje});
            return await db.SaveChangesAsync();
        }));
    }
}
