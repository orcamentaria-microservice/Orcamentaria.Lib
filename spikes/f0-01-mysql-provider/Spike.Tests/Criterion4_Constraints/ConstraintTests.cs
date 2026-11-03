using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion4_Constraints;

/// <summary>
/// Critério 4: HasCheckConstraint e índice único sobre coluna anulável.
///
/// HasCheckConstraint:
///   - Violação de CHECK (`PRICE` > 0) → DbUpdateException
///   - MySQL 8.0 valida constraints CHECK por padrão (diferente do 5.7 que ignorava)
///
/// Índice único sobre coluna anulável:
///   - Múltiplos NULLs são permitidos (comportamento padrão do MySQL UNIQUE)
///   - Duas linhas com o mesmo valor non-null disparam duplicata → DbUpdateException
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class ConstraintTests
{
    private readonly MySqlContainerFixture _fixture;

    public ConstraintTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckConstraint_ViolacaoLancaDbUpdateException()
    {
        // Arrange: preço negativo viola CK_SPIKE_ENTITY_PRICE_POSITIVE (`PRICE` > 0)
        await using var ctx = _fixture.CreateContext();

        ctx.SpikeEntities.Add(new SpikeEntity
        {
            Price       = -0.0001m,   // viola o CHECK
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = "CHK",
            Description = "check test",
            Thumbnail   = [0x01],
            Version     = 1,
        });

        // Act + Assert
        var act = () => ctx.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            because: "PRICE <= 0 viola CK_SPIKE_ENTITY_PRICE_POSITIVE; MySQL 8.0 lança erro ao tentar inserir");
    }

    [Fact]
    public async Task IndiceUnicoAnulavel_PermiteVariosNullsEBloqueiaDuplicado()
    {
        // Parte 1: múltiplos NULLs devem ser ACEITOS no índice UNIQUE
        // MySQL UNIQUE permite múltiplos NULLs porque NULL != NULL na comparação de índice.
        await using var ctxNull = _fixture.CreateContext();

        ctxNull.SpikeEntities.Add(MakeEntity(tenantKey: null, code: "NL1"));
        ctxNull.SpikeEntities.Add(MakeEntity(tenantKey: null, code: "NL2"));
        ctxNull.SpikeEntities.Add(MakeEntity(tenantKey: null, code: "NL3"));

        var actNulls = () => ctxNull.SaveChangesAsync();
        await actNulls.Should().NotThrowAsync(
            because: "MySQL UNIQUE permite múltiplos NULLs na coluna TenantKey");

        // Parte 2: duplicata de valor não-nulo deve ser BLOQUEADA
        var uniqueKey = $"dup-{Guid.NewGuid():N}";

        await using var ctxDup = _fixture.CreateContext();
        ctxDup.SpikeEntities.Add(MakeEntity(tenantKey: uniqueKey, code: "DU1"));
        ctxDup.SpikeEntities.Add(MakeEntity(tenantKey: uniqueKey, code: "DU2")); // duplicata

        var actDup = () => ctxDup.SaveChangesAsync();
        await actDup.Should().ThrowAsync<DbUpdateException>(
            because: "UX_SPIKE_ENTITY_TENANT_KEY bloqueia dois registros com o mesmo TenantKey não-nulo");
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static SpikeEntity MakeEntity(string? tenantKey, string code) =>
        new()
        {
            Price       = 1.0001m,
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = code.PadRight(1)[..Math.Min(code.Length, 10)],
            Description = "constraint test",
            Thumbnail   = [0xFF],
            TenantKey   = tenantKey,
            Version     = 1,
        };
}
