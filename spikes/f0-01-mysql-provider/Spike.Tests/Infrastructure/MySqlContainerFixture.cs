using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Spike.Data;
using Testcontainers.MySql;

namespace Spike.Tests.Infrastructure;

/// <summary>
/// Fixture compartilhada entre todos os testes de integração.
///
/// Inicia UM container MySQL 8.0 por sessão de teste e aplica as migrations
/// (EnsureCreatedAsync) antes da primeira classe de teste. Cada teste recebe
/// um DbContext isolado; o container fica ativo até o Dispose da coleção.
///
/// O banco é criado com collation utf8mb4_0900_ai_ci para validar o critério 3.
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    // mysql:8.0 — versão especificada no card F0-01 §6.1
    // Testcontainers 4.16+: passar a imagem no construtor (parameterless foi depreciado)
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.0")
        .WithDatabase("spike_test")
        .WithUsername("spike")
        .WithPassword("spike_test_only")
        // Garante collation utf8mb4_0900_ai_ci no banco criado pelo container
        .WithEnvironment("MYSQL_CHARACTER_SET_SERVER", "utf8mb4")
        .WithEnvironment("MYSQL_COLLATION_SERVER",     "utf8mb4_0900_ai_ci")
        .Build();

    /// <summary>
    /// Connection string gerada pelo Testcontainers após o container estar pronto.
    /// Disponível para uso nos testes.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();

        // Aplica o schema via EnsureCreated (cria todas as tabelas do DbContext).
        // O bundle (critério 2) é testado separadamente em T02 via script shell.
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Cria um DbContext novo para cada chamada.
    /// Cada teste deve chamar este método para obter um contexto isolado.
    /// </summary>
    public SpikeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SpikeDbContext>()
            .UseMySQL(ConnectionString)
            .Options;

        return new SpikeDbContext(options);
    }
}

/// <summary>
/// Coleção xUnit que compartilha o container MySQL entre todas as classes de teste.
/// Reduz o custo de startup: um container por run de `dotnet test`.
/// </summary>
[CollectionDefinition(MySqlCollection.Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
{
    public const string Name = "MySQL Integration";
}
