using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Spike.Data.Entities;

namespace Spike.Data;

/// <summary>
/// DbContext de prova do spike F0-01.
///
/// Todos os mapeamentos estão inline aqui (sem EntityTypeConfiguration separada)
/// porque o spike é descartável. Em código de produção cada entidade teria
/// sua própria IEntityTypeConfiguration&lt;T&gt;.
///
/// ACHADO DO SPIKE (critério 3):
///   O provider Oracle MySQL (MySql.EntityFrameworkCore) retorna DateTimeKind.Unspecified
///   para colunas DATETIME(6) — MySQL não armazena fuso horário, e o provider não aplica
///   SpecifyKind automaticamente. A solução padrão é um ValueConverter que:
///     • escrita: garante que o valor é UTC (rejeita Unspecified com throw)
///     • leitura: aplica DateTime.SpecifyKind(v, DateTimeKind.Utc)
///   No F0-02, este converter será extraído para a Lib como UtcDateTimeConverter.
/// </summary>
public sealed class SpikeDbContext : DbContext
{
    /// <summary>
    /// Converter reutilizável para colunas DATETIME(6) que devem ser UTC.
    ///
    /// Escrita: rejeita DateTimeKind.Local (convertendo para UTC) e aceita Utc/Unspecified.
    /// Leitura: aplica SpecifyKind(Utc) para que o valor retornado sempre seja Utc.
    ///
    /// No F0-02 este conversor será extraído para UtcDateTimeConverter na Lib.
    /// </summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
        new(
            // escrita → banco: normaliza para UTC se Local
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            // leitura ← banco: garante Kind=Utc sempre
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
        );

    /// <summary>Nullable variant do mesmo converter.</summary>
    private static readonly ValueConverter<DateTime?, DateTime?> UtcDateTimeNullableConverter =
        new(
            v => v == null ? (DateTime?)null : v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v,
            v => v == null ? (DateTime?)null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
        );
    public DbSet<SpikeEntity>   SpikeEntities  { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<SpikeCounter>  SpikeCounters  { get; set; } = null!;   // critério 7
    public DbSet<SpikeReplica>  SpikeReplicas  { get; set; } = null!;   // critério 8

    public SpikeDbContext(DbContextOptions<SpikeDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Collation padrão do banco — criada no Testcontainers com utf8mb4_0900_ai_ci.
        // Cobre o critério 3 (collation) sem precisar de anotação por coluna.
        modelBuilder.UseCollation("utf8mb4_0900_ai_ci");

        ConfigureSpikeEntity(modelBuilder);
        ConfigureOutboxMessage(modelBuilder);
        ConfigureSpikeCounter(modelBuilder);
        ConfigureSpikeReplica(modelBuilder);
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

            // Critério 3: DATETIME(6) UTC com ValueConverter
            // O provider Oracle não aplica SpecifyKind automaticamente (ACHADO DO SPIKE).
            // O UtcDateTimeConverter garante Kind=Utc em todas as leituras.
            entity.Property(e => e.CreatedAt)
                  .HasColumnName("CREATED_AT")
                  .HasColumnType("DATETIME(6)")
                  .HasConversion(UtcDateTimeConverter);

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
            // EF Core 10: API migrou para ToTable(t => t.HasCheckConstraint(...)).
            // Violação lança DbUpdateException (não DbUpdateConcurrencyException).
            entity.ToTable(t => t.HasCheckConstraint(
                name: "CK_SPIKE_ENTITY_PRICE_POSITIVE",
                sql:  "`PRICE` > 0"));

            // Critério 5: VERSION como token de concorrência otimista.
            // A aplicação é responsável por incrementar Version antes de cada Update.
            // EF Core inclui "AND VERSION = @original_Version" no WHERE do UPDATE.
            entity.Property(e => e.Version)
                  .HasColumnName("VERSION")
                  .IsConcurrencyToken();
        });
    }

    // ──────────────────────────────────────────────────────────────
    // OutboxMessage — tabela para critério 6 (relay SKIP LOCKED)
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
                  .HasColumnType("DATETIME(6)")
                  .HasConversion(UtcDateTimeConverter);

            entity.Property(e => e.PublishedAt)
                  .HasColumnName("PUBLISHED_AT")
                  .HasColumnType("DATETIME(6)")
                  .HasConversion(UtcDateTimeNullableConverter)
                  .IsRequired(false);

            // Índice parcial lógico: registros pendentes (PUBLISHED_AT IS NULL)
            // aparecem primeiro na ordem de ID — suporta a query de relay do critério 6.
            entity.HasIndex(e => new { e.PublishedAt, e.Id })
                  .HasDatabaseName("IX_OUTBOX_PENDING");
        });
    }

    // ──────────────────────────────────────────────────────────────
    // SpikeCounter — tabela para critério 7 (FOR UPDATE serializa incrementos)
    // ──────────────────────────────────────────────────────────────
    private static void ConfigureSpikeCounter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpikeCounter>(entity =>
        {
            entity.ToTable("T_SPIKE_COUNTER");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("ID")
                  .ValueGeneratedOnAdd();

            entity.Property(e => e.Value)
                  .HasColumnName("VALUE")
                  .IsRequired();
        });
    }

    // ──────────────────────────────────────────────────────────────
    // SpikeReplica — tabela para critério 8 (upsert condicional)
    // ──────────────────────────────────────────────────────────────
    private static void ConfigureSpikeReplica(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpikeReplica>(entity =>
        {
            entity.ToTable("T_SPIKE_REPLICA");

            entity.HasKey(e => e.Id);
            // ID vem do serviço de origem — a aplicação gerencia o valor (não AUTO_INCREMENT).
            entity.Property(e => e.Id)
                  .HasColumnName("ID")
                  .ValueGeneratedNever();

            entity.Property(e => e.Data)
                  .HasColumnName("DATA")
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(e => e.SourceVersion)
                  .HasColumnName("SOURCE_VERSION")
                  .IsRequired();
        });
    }
}
