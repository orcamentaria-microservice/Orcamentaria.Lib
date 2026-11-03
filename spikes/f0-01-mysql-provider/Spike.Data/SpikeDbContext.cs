using Microsoft.EntityFrameworkCore;
using Spike.Data.Entities;

namespace Spike.Data;

/// <summary>
/// DbContext de prova do spike F0-01.
///
/// Todos os mapeamentos estão inline aqui (sem EntityTypeConfiguration separada)
/// porque o spike é descartável. Em código de produção cada entidade teria
/// sua própria IEntityTypeConfiguration&lt;T&gt;.
/// </summary>
public sealed class SpikeDbContext : DbContext
{
    public DbSet<SpikeEntity>   SpikeEntities  { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public SpikeDbContext(DbContextOptions<SpikeDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collation padrão do banco — criada no Testcontainers com utf8mb4_0900_ai_ci.
        // Cobre o critério 3 (collation) sem precisar de anotação por coluna.
        modelBuilder.UseCollation("utf8mb4_0900_ai_ci");

        ConfigureSpikeEntity(modelBuilder);
        ConfigureOutboxMessage(modelBuilder);
    }

    // ──────────────────────────────────────────────────────────────
    // SpikeEntity — cobre critérios 3, 4 e 5
    // ──────────────────────────────────────────────────────────────
    private static void ConfigureSpikeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpikeEntity>(entity =>
        {
            entity.ToTable("T_SPIKE_ENTITY");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("ID")
                  .ValueGeneratedOnAdd();

            // Critério 3: DECIMAL(18,4) com HasPrecision — sem arredondamento silencioso
            entity.Property(e => e.Price)
                  .HasColumnName("PRICE")
                  .HasPrecision(18, 4);

            // Critério 3: DATETIME(6) UTC
            // O tipo DATETIME(6) preserva microssegundos; o round-trip exato é validado
            // forçando DateTimeKind.Utc antes de salvar (veja MappingRoundTripTests).
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CREATED_AT")
                  .HasColumnType("DATETIME(6)");

            // Critério 3: bool ↔ TINYINT(1)
            entity.Property(e => e.IsActive)
                  .HasColumnName("IS_ACTIVE")
                  .HasColumnType("tinyint(1)");

            // Critério 3: enum ↔ INT — conversão explícita para garantir portabilidade
            entity.Property(e => e.Status)
                  .HasColumnName("STATUS")
                  .HasConversion<int>();

            // Critério 3: CHAR(n) de comprimento fixo
            entity.Property(e => e.Code)
                  .HasColumnName("CODE")
                  .HasColumnType("CHAR(10)")
                  .HasMaxLength(10)
                  .IsFixedLength();

            // Critério 3: TEXT — sem limite de caracteres definido no modelo
            entity.Property(e => e.Description)
                  .HasColumnName("DESCRIPTION")
                  .HasColumnType("TEXT");

            // Critério 3: MEDIUMBLOB — até ~16 MB por linha
            entity.Property(e => e.Thumbnail)
                  .HasColumnName("THUMBNAIL")
                  .HasColumnType("MEDIUMBLOB");

            // Critério 4: índice único sobre coluna anulável.
            // MySQL permite múltiplos NULLs num índice UNIQUE — comportamento confirmado
            // no teste IndiceUnicoAnulavel_PermiteVariosNullsEBloqueiaDuplicado.
            entity.Property(e => e.TenantKey)
                  .HasColumnName("TENANT_KEY")
                  .HasMaxLength(100)
                  .IsRequired(false);

            entity.HasIndex(e => e.TenantKey)
                  .IsUnique()
                  .HasDatabaseName("UX_SPIKE_ENTITY_TENANT_KEY");

            // Critério 4: check constraint — preço deve ser positivo.
            // Violação lança DbUpdateException (não DbUpdateConcurrencyException).
            entity.HasCheckConstraint(
                name: "CK_SPIKE_ENTITY_PRICE_POSITIVE",
                sql: "`PRICE` > 0");

            // Critério 5: VERSION como token de concorrência otimista.
            // A aplicação é responsável por incrementar Version antes de cada Update.
            // EF Core inclui "AND VERSION = @original_Version" no WHERE do UPDATE.
            entity.Property(e => e.Version)
                  .HasColumnName("VERSION")
                  .IsConcurrencyToken();
        });
    }

    // ──────────────────────────────────────────────────────────────
    // OutboxMessage — tabela para critérios 6 e 7 (T02)
    // ──────────────────────────────────────────────────────────────
    private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("T_OUTBOX_MESSAGE");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("ID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.EventType)
                  .HasColumnName("EVENT_TYPE")
                  .HasMaxLength(100)
                  .IsRequired();

            // JSON armazenado como string — o provider Oracle trata VARCHAR/TEXT com JSON
            entity.Property(e => e.Payload)
                  .HasColumnName("PAYLOAD")
                  .HasColumnType("JSON")
                  .IsRequired();

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CREATED_AT")
                  .HasColumnType("DATETIME(6)");

            entity.Property(e => e.PublishedAt)
                  .HasColumnName("PUBLISHED_AT")
                  .HasColumnType("DATETIME(6)")
                  .IsRequired(false);

            // Índice parcial lógico: registros pendentes (PUBLISHED_AT IS NULL)
            // aparecem primeiro na ordem de ID — suporta a query de relay do critério 6.
            entity.HasIndex(e => new { e.PublishedAt, e.Id })
                  .HasDatabaseName("IX_OUTBOX_PENDING");
        });
    }
}
