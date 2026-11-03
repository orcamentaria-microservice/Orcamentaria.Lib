using Microsoft.EntityFrameworkCore;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion8_Upsert;

/// <summary>
/// Critério 8: upsert condicional de réplica + ExecuteUpdate/ExecuteDelete.
///
/// Padrão de réplica (O1-F3-Px):
///   Mensagens de domínio chegam via broker em ordem não garantida.
///   O upsert condicional garante que uma mensagem com SOURCE_VERSION menor
///   não sobrescreva dados já atualizados — idempotência e consistência eventual.
///
///   SQL usado (Oracle MySQL usa VALUES() que ainda é suportado em MySQL 8.0):
///     INSERT INTO T_SPIKE_REPLICA (ID, DATA, SOURCE_VERSION)
///     VALUES (@id, @data, @ver)
///     ON DUPLICATE KEY UPDATE
///       DATA = IF(VALUES(SOURCE_VERSION) > SOURCE_VERSION, VALUES(DATA), DATA),
///       SOURCE_VERSION = IF(VALUES(SOURCE_VERSION) > SOURCE_VERSION, VALUES(SOURCE_VERSION), SOURCE_VERSION)
///
/// ExecuteUpdate/ExecuteDelete:
///   API do EF Core 7+ que gera UPDATE/DELETE direto sem carregar entidades em memória.
///   Valida que o provider Oracle MySQL 10 os suporta corretamente.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class UpsertReplicaTests
{
    private readonly MySqlContainerFixture _fixture;

    public UpsertReplicaTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UpsertReplica_VersaoMenor_NaoSobrescreve()
    {
        // ── Setup: insere réplica com SourceVersion=2, Data="v2" ───────────────
        var sourceId = (long)new Random().Next(100_000, 999_999);
        await Upsert(_fixture.CreateContext, sourceId, data: "v2", version: 2);

        // ── Act: tenta upsert com SourceVersion=1 (menor) ─────────────────────
        await Upsert(_fixture.CreateContext, sourceId, data: "v1-tentativa", version: 1);

        // ── Assert: dados originais (v2) não foram sobrescritos ────────────────
        await using var readCtx = _fixture.CreateContext();
        var loaded = await readCtx.SpikeReplicas.AsNoTracking().SingleAsync(r => r.Id == sourceId);

        loaded.Data.Should().Be("v2",
            because: "SOURCE_VERSION 1 < SOURCE_VERSION 2: IF(...) não deve sobrescrever");
        loaded.SourceVersion.Should().Be(2,
            because: "versão no banco (2) deve ser preservada quando chegou versão menor (1)");
    }

    [Fact]
    public async Task UpsertReplica_VersaoMaior_Sobrescreve()
    {
        // Verifica o caminho feliz: versão maior SIM sobrescreve
        var sourceId = (long)new Random().Next(1_000_000, 9_999_999);
        await Upsert(_fixture.CreateContext, sourceId, data: "original", version: 1);
        await Upsert(_fixture.CreateContext, sourceId, data: "atualizado", version: 5);

        await using var readCtx = _fixture.CreateContext();
        var loaded = await readCtx.SpikeReplicas.AsNoTracking().SingleAsync(r => r.Id == sourceId);

        loaded.Data.Should().Be("atualizado",
            because: "SOURCE_VERSION 5 > SOURCE_VERSION 1: IF(...) deve sobrescrever");
        loaded.SourceVersion.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteUpdateDelete_Funcionam()
    {
        // ── Setup: cria 3 réplicas com sourceId distinto ───────────────────────
        var prefix = new Random().Next(10_000_000, 99_999_999);
        var ids    = Enumerable.Range(0, 3).Select(i => (long)(prefix + i)).ToList();

        foreach (var id in ids)
            await Upsert(_fixture.CreateContext, id, data: "inicial", version: 1);

        // ── ExecuteUpdate: altera Data via UPDATE direto (sem carregar em memória) ──
        await using var updateCtx = _fixture.CreateContext();
        var updatedCount = await updateCtx.SpikeReplicas
            .Where(r => ids.Contains(r.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Data, "atualizado-bulk")
                .SetProperty(r => r.SourceVersion, 99));

        updatedCount.Should().Be(3,
            because: "ExecuteUpdate deve atualizar as 3 réplicas criadas");

        // ── Verifica valores após update ───────────────────────────────────────
        await using var verifyCtx = _fixture.CreateContext();
        var afterUpdate = await verifyCtx.SpikeReplicas
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        afterUpdate.Should().AllSatisfy(r =>
        {
            r.Data.Should().Be("atualizado-bulk");
            r.SourceVersion.Should().Be(99);
        });

        // ── ExecuteDelete: remove todas via DELETE direto ─────────────────────
        await using var deleteCtx = _fixture.CreateContext();
        var deletedCount = await deleteCtx.SpikeReplicas
            .Where(r => ids.Contains(r.Id))
            .ExecuteDeleteAsync();

        deletedCount.Should().Be(3,
            because: "ExecuteDelete deve remover as 3 réplicas");

        // Confirma que sumiram
        await using var finalCtx = _fixture.CreateContext();
        var remaining = await finalCtx.SpikeReplicas
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .CountAsync();

        remaining.Should().Be(0, because: "após ExecuteDelete as réplicas não devem existir");
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Executa o upsert condicional de réplica via SQL cru.
    ///
    /// A sintaxe ON DUPLICATE KEY UPDATE + IF(VALUES(...)) é específica do MySQL 8.0
    /// e é o padrão definido na arquitetura do projeto para réplicas (O1-F3-Px).
    /// A função VALUES() está depreciada no MySQL 8.0.20 em favor de aliases,
    /// mas ainda é suportada em 8.0.x e é o formato atual nos cards de arquitetura.
    /// </summary>
    private static async Task Upsert(
        Func<Spike.Data.SpikeDbContext> factory,
        long   id,
        string data,
        int    version)
    {
        await using var ctx = factory();

        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO T_SPIKE_REPLICA (ID, DATA, SOURCE_VERSION) " +
            "VALUES ({0}, {1}, {2}) " +
            "ON DUPLICATE KEY UPDATE " +
            "  DATA          = IF(VALUES(SOURCE_VERSION) > SOURCE_VERSION, VALUES(DATA),          DATA), " +
            "  SOURCE_VERSION = IF(VALUES(SOURCE_VERSION) > SOURCE_VERSION, VALUES(SOURCE_VERSION), SOURCE_VERSION)",
            id, data, version);
    }
}
