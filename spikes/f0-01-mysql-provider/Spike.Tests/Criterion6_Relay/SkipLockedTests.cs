using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion6_Relay;

/// <summary>
/// Critério 6: relay com FOR UPDATE SKIP LOCKED.
///
/// Duas transações concorrentes executam a query do relay:
///   SELECT * FROM T_OUTBOX_MESSAGE
///   WHERE PUBLISHED_AT IS NULL
///   ORDER BY ID
///   LIMIT 50
///   FOR UPDATE SKIP LOCKED
///
/// Cada transação deve receber um conjunto **disjunto** de mensagens:
/// linhas já bloqueadas pela T1 são puladas pelo SKIP LOCKED da T2.
/// Isso é a base do relay de outbox multi-worker (O1-F4-P1a/b).
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class SkipLockedTests
{
    private readonly MySqlContainerFixture _fixture;

    public SkipLockedTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SkipLocked_DuasTransacoes_RecebemConjuntosDisjuntos()
    {
        // ── Setup: insere 100 mensagens pendentes ──────────────────────────────
        await using var setupCtx = _fixture.CreateContext();

        var messages = Enumerable.Range(1, 100).Select(i => new OutboxMessage
        {
            EventType   = "spike.test.created",
            Payload     = $"{{\"seq\":{i}}}",
            CreatedAt   = DateTime.UtcNow,
            PublishedAt = null,     // pendente
        }).ToList();

        setupCtx.OutboxMessages.AddRange(messages);
        await setupCtx.SaveChangesAsync();

        // ── Sincronização entre as duas transações ────────────────────────────
        // T1 bloqueia 50 linhas, sinaliza T2, espera T2 terminar, então commita.
        // SemaphoreSlim garante a ordem sem polling.
        using var t1Locked  = new SemaphoreSlim(0, 1); // T1 → "já bloqueei minhas linhas"
        using var t2Fetched = new SemaphoreSlim(0, 1); // T2 → "já busquei minhas linhas"

        var setT1 = new List<long>();
        var setT2 = new List<long>();

        // ── Transação 1 ────────────────────────────────────────────────────────
        var task1 = Task.Run(async () =>
        {
            await using var ctx = _fixture.CreateContext();
            // READ COMMITTED: T2 não precisa esperar T1 commitar para ver linhas não bloqueadas
            await using var tx = await ctx.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            // Query do relay — exatamente como especificado no card F0-01 §6.1
            setT1 = await ctx.OutboxMessages
                .FromSqlRaw(
                    "SELECT * FROM T_OUTBOX_MESSAGE " +
                    "WHERE PUBLISHED_AT IS NULL " +
                    "ORDER BY ID " +
                    "LIMIT 50 " +
                    "FOR UPDATE SKIP LOCKED")
                .AsNoTracking()
                .Select(m => m.Id)
                .ToListAsync();

            t1Locked.Release();             // informa T2: pode buscar agora
            await t2Fetched.WaitAsync();    // espera T2 terminar antes de commitar

            await tx.CommitAsync();
        });

        // ── Transação 2 ────────────────────────────────────────────────────────
        var task2 = Task.Run(async () =>
        {
            await t1Locked.WaitAsync();     // aguarda T1 bloquear suas linhas

            await using var ctx = _fixture.CreateContext();
            await using var tx = await ctx.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted);

            // SKIP LOCKED: recebe apenas as linhas NÃO bloqueadas por T1
            setT2 = await ctx.OutboxMessages
                .FromSqlRaw(
                    "SELECT * FROM T_OUTBOX_MESSAGE " +
                    "WHERE PUBLISHED_AT IS NULL " +
                    "ORDER BY ID " +
                    "LIMIT 50 " +
                    "FOR UPDATE SKIP LOCKED")
                .AsNoTracking()
                .Select(m => m.Id)
                .ToListAsync();

            await tx.CommitAsync();
            t2Fetched.Release();            // libera T1 para commitar
        });

        await Task.WhenAll(task1, task2);

        // ── Assert ─────────────────────────────────────────────────────────────
        setT1.Should().HaveCount(50,
            because: "T1 deve bloquear 50 mensagens pendentes com LIMIT 50");

        setT2.Should().HaveCount(50,
            because: "T2 deve encontrar as outras 50 mensagens com SKIP LOCKED");

        setT1.Intersect(setT2).Should().BeEmpty(
            because: "FOR UPDATE SKIP LOCKED garante conjuntos disjuntos: " +
                     "linhas bloqueadas por T1 são puladas por T2");
    }
}
