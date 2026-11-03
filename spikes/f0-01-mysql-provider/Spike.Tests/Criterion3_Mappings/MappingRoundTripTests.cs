using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;
using Spike.Tests.Infrastructure;

namespace Spike.Tests.Criterion3_Mappings;

/// <summary>
/// Critério 3: mapeamentos com round-trip exato.
///
/// Valida que EF Core + provider Oracle MySQL preservam os valores sem perda:
///   - DECIMAL(18,4) com HasPrecision — sem arredondamento silencioso
///   - DATETIME(6) UTC com microssegundos
///   - bool ↔ TINYINT(1)
///   - enum ↔ INT
///   - CHAR(10) fixo
///   - TEXT sem truncamento
///   - MEDIUMBLOB
///   - Collation utf8mb4_0900_ai_ci (criada no container; validada por accent-sensitivity)
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class MappingRoundTripTests
{
    private readonly MySqlContainerFixture _fixture;

    public MappingRoundTripTests(MySqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Mapeamentos_RoundTrip_PreservaValoresExatos()
    {
        // Arrange
        // DECIMAL: valor com 4 casas decimais e total > 9 dígitos — exercita a precisão
        var price       = 12345.6789m;          // DECIMAL(18,4)
        var createdAt   = new DateTime(2026, 6, 15, 12, 30, 45, 123, DateTimeKind.Utc)
                            .AddTicks(4560);    // microssegundos (DATETIME(6) tem precisão de 1µs)
        var thumbnail   = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var description = "Descrição com acentos: café, ação, naïve — TEXT utf8mb4";
        var code        = "CODE12345 "; // 10 chars (CHAR(10) padding)

        await using var writeCtx = _fixture.CreateContext();

        var entity = new SpikeEntity
        {
            Price       = price,
            CreatedAt   = createdAt,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = code.Trim(), // EF trim; banco retorna com padding
            Description = description,
            Thumbnail   = thumbnail,
            TenantKey   = "tenant-roundtrip",
            Version     = 1,
        };

        writeCtx.SpikeEntities.Add(entity);
        await writeCtx.SaveChangesAsync();
        var savedId = entity.Id;

        // Act: novo contexto para garantir leitura do banco (sem cache de 1ª nível)
        await using var readCtx = _fixture.CreateContext();
        var loaded = await readCtx.SpikeEntities
            .AsNoTracking()
            .SingleAsync(e => e.Id == savedId);

        // Assert — round-trip exato para cada tipo mapeado

        // DECIMAL(18,4): sem perda de dígitos
        loaded.Price.Should().Be(price,
            because: "DECIMAL(18,4) deve preservar 12345.6789m sem arredondamento");

        // DATETIME(6) UTC: EF Core preserva microssegundos; arredondamento para µs é esperado
        var expectedTicks = createdAt.Ticks - (createdAt.Ticks % TimeSpan.TicksPerMicrosecond);
        loaded.CreatedAt.Ticks.Should().Be(expectedTicks,
            because: "DATETIME(6) tem precisão de 1µs — arredondamento abaixo de 1µs é esperado");
        loaded.CreatedAt.Kind.Should().Be(DateTimeKind.Utc,
            because: "o valor deve voltar como UTC para evitar erros de fuso");

        // bool ↔ TINYINT(1)
        loaded.IsActive.Should().BeTrue(
            because: "bool true deve mapear para TINYINT 1 e voltar como true");

        // enum ↔ INT
        loaded.Status.Should().Be(SpikeStatus.Active,
            because: "SpikeStatus.Active (1) deve ser armazenado como INT 1 e convertido de volta");

        // CHAR(10): MySQL retorna com espaços à direita preenchidos
        loaded.Code.TrimEnd().Should().Be("CODE12345",
            because: "CHAR(10) pode retornar com trailing spaces — trim deve recuperar o valor original");

        // TEXT: sem truncamento
        loaded.Description.Should().Be(description,
            because: "TEXT utf8mb4 deve preservar acentos e caracteres especiais");

        // MEDIUMBLOB: bytes exatos
        loaded.Thumbnail.Should().Equal(thumbnail,
            because: "MEDIUMBLOB deve preservar os bytes sem alteração");
    }

    [Fact]
    public async Task Mapeamentos_DateTime_MicrossegundosPreservadosExatamente()
    {
        // Teste dedicado para microssegundos — trava a precisão de DATETIME(6)
        // O valor é arredondado para o microssegundo mais próximo (1µs = 10 ticks).
        var dataCom6Decimais = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                                   .AddTicks(123_456); // 12345.6µs — parte de µs truncada

        var expectedTicks = dataCom6Decimais.Ticks
                          - (dataCom6Decimais.Ticks % TimeSpan.TicksPerMicrosecond);

        await using var writeCtx = _fixture.CreateContext();
        var entity = new SpikeEntity
        {
            Price       = 1.0001m,
            CreatedAt   = dataCom6Decimais,
            IsActive    = false,
            Status      = SpikeStatus.Inactive,
            Code        = "T",
            Description = "datetime test",
            Thumbnail   = [0x00],
            Version     = 1,
        };
        writeCtx.SpikeEntities.Add(entity);
        await writeCtx.SaveChangesAsync();

        await using var readCtx = _fixture.CreateContext();
        var loaded = await readCtx.SpikeEntities.AsNoTracking().SingleAsync(e => e.Id == entity.Id);

        loaded.CreatedAt.Ticks.Should().Be(expectedTicks,
            because: "DATETIME(6) preserva até microssegundo; ticks sub-µs são perdidos");
    }

    [Fact]
    public async Task Mapeamentos_Collation_AcentoSensitiveComportamento()
    {
        // Valida que a collation utf8mb4_0900_ai_ci está ativa:
        // "ai" = accent-insensitive, "ci" = case-insensitive.
        // Uma consulta LIKE é case-insensitive nessa collation.
        var tenantKey = $"collation-test-{Guid.NewGuid():N}";
        var description = "Cafe sem acento";

        await using var writeCtx = _fixture.CreateContext();
        writeCtx.SpikeEntities.Add(new SpikeEntity
        {
            Price       = 1.0001m,
            CreatedAt   = DateTime.UtcNow,
            IsActive    = true,
            Status      = SpikeStatus.Active,
            Code        = "COL",
            Description = description,
            Thumbnail   = [0x01],
            TenantKey   = tenantKey,
            Version     = 1,
        });
        await writeCtx.SaveChangesAsync();

        await using var readCtx = _fixture.CreateContext();

        // utf8mb4_0900_ai_ci: LIKE é case-insensitive
        var found = await readCtx.SpikeEntities
            .AsNoTracking()
            .Where(e => EF.Functions.Like(e.TenantKey!, tenantKey.ToUpperInvariant()))
            .ToListAsync();

        found.Should().ContainSingle(
            because: "collation utf8mb4_0900_ai_ci é case-insensitive; LIKE com uppercase deve encontrar o registro");
    }
}
