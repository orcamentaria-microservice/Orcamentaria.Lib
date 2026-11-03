namespace Spike.Data.Entities;

/// <summary>
/// Entidade de prova que concentra todos os mapeamentos a validar nos critérios 3–5:
///   - DECIMAL(18,4) via HasPrecision
///   - DATETIME(6) UTC com round-trip exato
///   - bool ↔ TINYINT(1)
///   - enum ↔ INT
///   - CHAR(n) fixo
///   - TEXT
///   - MEDIUMBLOB
///   - Collation utf8mb4_0900_ai_ci (definida no DbContext/database)
///   - HasCheckConstraint (critério 4)
///   - Índice único sobre coluna anulável (critério 4)
///   - VERSION como IsConcurrencyToken (critério 5)
/// </summary>
public sealed class SpikeEntity
{
    public long Id { get; set; }

    // DECIMAL(18,4) — HasPrecision(18, 4) no modelo
    public decimal Price { get; set; }

    // DATETIME(6) UTC — round-trip com microssegundos
    public DateTime CreatedAt { get; set; }

    // bool ↔ TINYINT(1)
    public bool IsActive { get; set; }

    // enum ↔ INT — conversão explícita via HasConversion<int>()
    public SpikeStatus Status { get; set; }

    // CHAR(10) — comprimento fixo
    public string Code { get; set; } = string.Empty;

    // TEXT — sem limite de tamanho definido no modelo
    public string Description { get; set; } = string.Empty;

    // MEDIUMBLOB — para provar mapeamento de blob binário
    public byte[] Thumbnail { get; set; } = [];

    // Coluna anulável: índice único permite vários NULLs, bloqueia duplicata non-null
    // Padrão do projeto: ACTIVE_UNIQUE_KEY / DEFAULT_UNIQUE_KEY / OPEN_CNPJ_KEY
    public string? TenantKey { get; set; }

    // Critério 5: token de concorrência otimista gerenciado pela aplicação.
    // EF Core inclui o valor original no WHERE: UPDATE ... WHERE ID=? AND VERSION=?
    // Se outra transação já incrementou, UPDATE afeta 0 linhas → DbUpdateConcurrencyException.
    public uint Version { get; set; }
}

/// <summary>
/// Status da entidade de prova — mapeado como INT no banco.
/// </summary>
public enum SpikeStatus
{
    Active   = 1,
    Inactive = 2,
}
