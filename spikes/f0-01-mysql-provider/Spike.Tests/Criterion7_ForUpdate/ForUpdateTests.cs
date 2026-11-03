using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion7_ForUpdate;

/// <summary>
/// Critério 7: SELECT … FOR UPDATE serializa incrementos concorrentes.
///
/// Padrão de domínio (RN-172/173):
///   Contadores de uso por tenant (orçamentos emitidos, usuários ativos, etc.)
///   precisam ser incrementados atomicamente. Sem FOR UPDATE, leituras simultâneas
///   retornam o mesmo valor e todas escrevem valor+1, resultando em perda de
///   incrementos (lost-update). Com FOR UPDATE, cada transação espera a anterior
///   commitar antes de ler, garantindo incremento sequencial.
///
/// O teste lança 10 tarefas concorrentes, cada uma faz READ-MODIFY-WRITE
/// via FOR UPDATE, e o resultado final deve ser exatamente 10.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class ForUpdateTests
{
    private readonly MySqlContainerFixture _fixture;

    public ForUpdateTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ForUpdate_10IncrementosParalelos_ResultadoExato10()
    {
        // ── Setup: cria contador zerado ────────────────────────────────────────
        await using var setupCtx = _fixture.CreateContext();
        var counter = new SpikeCounter { Value = 0 };
        setupCtx.SpikeCounters.Add(counter);
        await setupCtx.SaveChangesAsync();
        var counterId = counter.Id;

        // ── 10 tarefas paralelas: cada uma incrementa via FOR UPDATE ───────────
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await using var ctx = _fixture.CreateContext();

            // REPEATABLE READ (padrão MySQL): SELECT FOR UPDATE força leitura corrente
            // (current read), não snapshot — a fila de espera pelo lock garante ordem.
            await using var tx = await ctx.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.RepeatableRead);

            // Bloqueia a linha antes de ler — sem FOR UPDATE duas tarefas leriam o
            // mesmo valor e ambas escreveriam value+1, perdendo um incremento.
            var locked = await ctx.SpikeCounters
                .FromSql($"SELECT * FROM T_SPIKE_COUNTER WHERE ID = {counterId} FOR UPDATE")
                .SingleAsync();

            locked.Value++;
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
        })).ToList();

        await Task.WhenAll(tasks);

        // ── Verifica valor final ────────────────────────────────────────────────
        await using var verifyCtx = _fixture.CreateContext();
        var final = await verifyCtx.SpikeCounters
            .AsNoTracking()
            .SingleAsync(c => c.Id == counterId);

        final.Value.Should().Be(10,
            because: "10 incrementos serializados via FOR UPDATE devem resultar em exatamente 10");
    }
}
