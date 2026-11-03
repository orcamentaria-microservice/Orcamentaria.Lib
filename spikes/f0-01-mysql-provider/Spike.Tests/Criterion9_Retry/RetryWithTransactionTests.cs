using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Spike.Data;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion9_Retry;

/// <summary>
/// Critério 9: EnableRetryOnFailure convive com transação explícita.
///
/// EF Core, quando configurado com uma execution strategy de retry, lança
/// InvalidOperationException se a aplicação tenta abrir uma transação
/// diretamente via Database.BeginTransaction() — a estratégia não sabe como
/// repetir uma transação já iniciada pelo usuário.
///
/// A solução correta (e necessária para o padrão UoW do projeto) é encapsular
/// a transação dentro de Database.CreateExecutionStrategy().ExecuteAsync(...).
/// Dentro desse escopo, o EF pode repetir o bloco inteiro se ocorrer falha
/// transiente (dead-lock, connection reset, timeout).
///
/// Este teste valida que o padrão correto funciona sem lançar exceção,
/// enquanto o anti-padrão é documentado com o erro esperado.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class RetryWithTransactionTests
{
    private readonly MySqlContainerFixture _fixture;

    public RetryWithTransactionTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RetryOnFailure_ComTransacaoExplicita_UsaExecutionStrategySemErro()
    {
        // ── Arrange: DbContext com EnableRetryOnFailure ────────────────────────
        await using var ctx = CreateContextWithRetry();

        // ── Act: padrão correto — transação dentro da execution strategy ────────
        var strategy = ctx.Database.CreateExecutionStrategy();

        var saved = false;
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await ctx.Database.BeginTransactionAsync();

            ctx.SpikeEntities.Add(new SpikeEntity
            {
                Price       = 1.0001m,
                CreatedAt   = DateTime.UtcNow,
                IsActive    = true,
                Status      = SpikeStatus.Active,
                Code        = "RTRY",
                Description = "retry test",
                Thumbnail   = [0xAA],
                TenantKey   = $"retry-{Guid.NewGuid():N}",
                Version     = 1,
            });

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            saved = true;
        });

        // ── Assert: nenhuma exceção, operação completada ────────────────────────
        saved.Should().BeTrue(
            because: "CreateExecutionStrategy().ExecuteAsync() deve permitir transação explícita " +
                     "com EnableRetryOnFailure ativo");
    }

    [Fact]
    public async Task RetryOnFailure_TransacaoDireta_ProviderOracleNaoEnforcaGuard()
    {
        // ── ACHADO DO SPIKE (critério 9): comportamento Oracle ≠ Pomelo ──────────
        //
        // O provider Oracle MySQL (MySql.EntityFrameworkCore 10.x) NÃO lança
        // InvalidOperationException quando BeginTransaction() é chamado diretamente
        // com EnableRetryOnFailure configurado.
        //
        // O Pomelo lança: "The configured execution strategy does not support
        // user-initiated transactions. Use CreateExecutionStrategy()."
        //
        // O Oracle é silenciosamente permissivo — a transação abre sem erro,
        // mas NÃO será repetida em caso de falha transiente (a execution strategy
        // não gerencia transações abertas fora do seu escopo).
        //
        // Implicação para o projeto (F0-02):
        //   A Lib DEVE usar o padrão CreateExecutionStrategy().ExecuteAsync()
        //   por consistência e para garantir retentativas, mesmo que o provider
        //   Oracle não force. Código que usa BeginTransaction() direta
        //   ficará silenciosamente sem retry — difícil de debugar em produção.
        await using var ctx = CreateContextWithRetry();

        var act = () => ctx.Database.BeginTransactionAsync();

        // Oracle permite — ao contrário do Pomelo; documentado como achado do spike
        await act.Should().NotThrowAsync(
            because: "Oracle MySQL provider NÃO lança InvalidOperationException para " +
                     "BeginTransaction() direta com retry ativo (comportamento difere do Pomelo). " +
                     "Detalhe: sem o wrapper da execution strategy, retentativas não funcionam.");
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private SpikeDbContext CreateContextWithRetry()
    {
        var options = new DbContextOptionsBuilder<SpikeDbContext>()
            .UseMySQL(
                _fixture.ConnectionString,
                mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                    maxRetryCount:    3,
                    maxRetryDelay:    TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null))
            .Options;

        return new SpikeDbContext(options);
    }
}
