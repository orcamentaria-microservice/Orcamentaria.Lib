using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion5_Concurrency;

/// <summary>
/// Critério 5: VERSION como IsConcurrencyToken gera DbUpdateConcurrencyException.
///
/// O padrão do projeto usa um uint VERSION gerenciado pela aplicação:
///   - No Update, a aplicação incrementa Version antes de SaveChanges.
///   - EF Core inclui "AND VERSION = @original_version" no WHERE do UPDATE.
///   - Se outro contexto já salvou (incrementou VERSION), o UPDATE afeta 0 linhas
///     e EF lança DbUpdateConcurrencyException (detalhado via DbUpdateConcurrencyException.Entries).
///
/// Este padrão evita dependência de AUTO_INCREMENT de contadores no banco
/// e funciona sem ROWVERSION nativo (inexistente no MySQL 8.0).
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class ConcurrencyTests
{
    private readonly MySqlContainerFixture _fixture;

    public ConcurrencyTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Version_AtualizacaoConcorrente_LancaConcurrencyException()
    {
        // ── Setup: insere uma entidade base ──────────────────────────────────
        await using var setupCtx = _fixture.CreateContext();
        var entity = new SpikeEntity
        {
            Price       = 99.9900m,
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = "CONC",
            Description = "concurrency test",
            Thumbnail   = [0xAB, 0xCD],
            TenantKey   = $"concurrency-{Guid.NewGuid():N}",
            Version     = 1,
        };
        setupCtx.SpikeEntities.Add(entity);
        await setupCtx.SaveChangesAsync();
        var entityId = entity.Id;

        // ── Contexto 1: carrega a entidade (Version = 1) ─────────────────────
        await using var ctx1 = _fixture.CreateContext();
        var entity1 = await ctx1.SpikeEntities.SingleAsync(e => e.Id == entityId);

        // ── Contexto 2: carrega a mesma entidade (Version = 1) ───────────────
        await using var ctx2 = _fixture.CreateContext();
        var entity2 = await ctx2.SpikeEntities.SingleAsync(e => e.Id == entityId);

        // ── ctx1 salva primeiro (Version 1 → 2) ──────────────────────────────
        entity1.Description = "atualizado pelo ctx1";
        entity1.Version++;  // incrementa: 1 → 2
        await ctx1.SaveChangesAsync();  // UPDATE WHERE VERSION = 1 → sucesso, VERSION = 2

        // ── ctx2 tenta salvar com Version desatualizada (ainda tem Version = 1) ──
        entity2.Description = "atualizado pelo ctx2 — deve falhar";
        entity2.Version++;  // ctx2 incrementa de 1 → 2, mas o banco já tem 2
        // EF vai gerar: UPDATE ... WHERE Id = @id AND VERSION = 1
        // O banco encontra VERSION = 2 → 0 rows affected → DbUpdateConcurrencyException

        var act = () => ctx2.SaveChangesAsync();

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            because: "o banco já tem VERSION=2 (salvo pelo ctx1); " +
                     "ctx2 usa WHERE VERSION=1, que não encontra nenhuma linha");
    }

    [Fact]
    public async Task Version_AtualizacaoSequencial_Sucesso()
    {
        // Caso de sucesso: mesma entidade atualizada duas vezes em sequência,
        // cada vez incrementando Version. Valida o caminho feliz do token.
        await using var ctx = _fixture.CreateContext();
        var entity = new SpikeEntity
        {
            Price       = 10.0000m,
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = "SEQ",
            Description = "v1",
            Thumbnail   = [0x01],
            TenantKey   = $"seq-{Guid.NewGuid():N}",
            Version     = 1,
        };
        ctx.SpikeEntities.Add(entity);
        await ctx.SaveChangesAsync();

        // Primeira atualização sequencial: Version 1 → 2
        entity.Description = "v2";
        entity.Version++;
        var act1 = () => ctx.SaveChangesAsync();
        await act1.Should().NotThrowAsync(
            because: "primeira atualização sequencial: WHERE VERSION=1 encontra a linha");

        // Segunda atualização sequencial: Version 2 → 3
        entity.Description = "v3";
        entity.Version++;
        var act2 = () => ctx.SaveChangesAsync();
        await act2.Should().NotThrowAsync(
            because: "segunda atualização sequencial: WHERE VERSION=2 encontra a linha");

        // Confirma Version final no banco
        await using var verifyCtx = _fixture.CreateContext();
        var loaded = await verifyCtx.SpikeEntities.AsNoTracking().SingleAsync(e => e.Id == entity.Id);
        loaded.Version.Should().Be(3u,
            because: "duas atualizações a partir de Version=1 devem resultar em Version=3");
        loaded.Description.Should().Be("v3");
    }

    [Fact]
    public async Task Version_EntryDeConcorrencia_ContemDadosDoConflito()
    {
        // Valida que DbUpdateConcurrencyException.Entries contém a entrada com conflito,
        // permitindo ao handler de domínio fazer merge ou notificar o usuário.
        await using var setupCtx = _fixture.CreateContext();
        var entity = new SpikeEntity
        {
            Price       = 5.0000m,
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = "ENTR",
            Description = "entry test",
            Thumbnail   = [0x02],
            TenantKey   = $"entry-{Guid.NewGuid():N}",
            Version     = 1,
        };
        setupCtx.SpikeEntities.Add(entity);
        await setupCtx.SaveChangesAsync();

        await using var ctx1 = _fixture.CreateContext();
        var entity1 = await ctx1.SpikeEntities.SingleAsync(e => e.Id == entity.Id);

        await using var ctx2 = _fixture.CreateContext();
        var entity2 = await ctx2.SpikeEntities.SingleAsync(e => e.Id == entity.Id);

        // ctx1 ganha a corrida
        entity1.Version++;
        entity1.Price = 6.0000m;
        await ctx1.SaveChangesAsync();

        // ctx2 perde; captura a exceção para inspecionar Entries
        entity2.Version++;
        entity2.Price = 7.0000m;

        DbUpdateConcurrencyException? caught = null;
        try
        {
            await ctx2.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            caught = ex;
        }

        caught.Should().NotBeNull();
        caught!.Entries.Should().ContainSingle(
            because: "apenas uma entidade estava em conflito");
        caught.Entries[0].Entity.Should().BeOfType<SpikeEntity>(
            because: "a entrada em conflito deve ser a SpikeEntity que ctx2 tentou salvar");
    }
}
