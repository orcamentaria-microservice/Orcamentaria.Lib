# Spike F0-01 — Validação .NET 10 + Provider MySQL para EF Core 10

**Data:** 2026-11-03  
**SDK .NET 10:** `10.0.402` (Windows) / `10.0.114` (Ubuntu WSL2)  
**dotnet-ef:** `10.0.14`  
**Docker:** `29.9.2` (WSL2 — Ubuntu 26.04 LTS)  
**MySQL testado:** `mysql:8.0` (Testcontainers + container isolado para bundle)

---

## Decisão

**Provider escolhido: `MySql.EntityFrameworkCore` `10.0.12`** (Oracle Corporation)  
Publicado em **2026-10-21** · Versão estável (sem `-preview`/`-rc`) · TFM `net10.0`  
Segue a **Regra 1 do §6.2**: provider Oracle passando em todos os critérios → mantém ADR-001.

Pomelo `Pomelo.EntityFrameworkCore.MySql` **não testado**: versão estável mais recente (9.0.0)
não tem suporte para EF Core 10, tornando-o inviável para a migração.

---

## Tabela critério × resultado

| # | Critério | Provider Oracle 10.0.12 | Achados / Observações |
|---|---|:---:|---|
| 1 | Pacote estável (sem `-preview`/`-rc`), net10.0 | ✅ PASS | Assembly TFM `net10.0`, versão informacional sem sufixo |
| 2 | `efbundle` aplicado 2× em banco vazio — idempotente | ✅ PASS | `bundle-test.sh`; "No migrations were applied. The database is already up to date." |
| 3 | Round-trip DECIMAL(18,4), DATETIME(6) UTC, bool/TINYINT, enum/INT, CHAR, TEXT, MEDIUMBLOB, collation | ✅ PASS | **Achado A1** (ver abaixo) |
| 4 | `HasCheckConstraint` via `ToTable(t => t.Has…)` + índice único anulável | ✅ PASS | API EF Core 10: `ToTable(t => t.HasCheckConstraint(...))` — chamada direta obsoleta (CS0618) |
| 5 | `VERSION` como `IsConcurrencyToken` → `DbUpdateConcurrencyException` | ✅ PASS | `Entries[0].Entity` é `SpikeEntity`; merge/reload do lado da app |
| 6 | `FOR UPDATE SKIP LOCKED` — conjuntos disjuntos | ✅ PASS | `FromSqlRaw` funciona (ver snippet abaixo) |
| 7 | `FOR UPDATE` serializa 10 incrementos paralelos | ✅ PASS | Valor final = 10; sem lost-updates |
| 8 | `ON DUPLICATE KEY UPDATE IF(VALUES(…))` + `ExecuteUpdate`/`ExecuteDelete` | ✅ PASS | Versão menor não sobrescreve; bulk update/delete funcionam |
| 9 | `EnableRetryOnFailure` convive com transação explícita via `CreateExecutionStrategy` | ✅ PASS | **Achado A2** (ver abaixo) |

**Resultado: 9/9 critérios passando.**

---

## Achados documentados

### A1 — `DATETIME(6)` retorna `DateTimeKind.Unspecified`

O provider Oracle não aplica `SpecifyKind(Utc)` automaticamente na leitura. Sem tratamento, o
valor volta como `DateTimeKind.Unspecified`, o que silencia bugs de fuso horário em conversões
e comparações.

**Solução aplicada no spike (vai para a Lib no F0-02):**

```csharp
// SpikeDbContext.cs — extrair para UtcDateTimeConverter na Lib
private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
    toDb:   v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
    fromDb: v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
);

// No mapeamento de cada propriedade DATETIME(6) UTC:
entity.Property(e => e.CreatedAt)
      .HasColumnType("DATETIME(6)")
      .HasConversion(UtcDateTimeConverter);
```

**No F0-02:** criar `UtcDateTimeConverter` e `UtcDateTimeNullableConverter` em
`Orcamentaria.Lib.Infrastructure/Persistence/Converters/`.

---

### A2 — Oracle provider não guarda `BeginTransaction()` direto com retry ativo

O Pomelo lança `InvalidOperationException` ao chamar `BeginTransactionAsync()` diretamente
quando `EnableRetryOnFailure` está configurado, forçando o uso do padrão correto.
O Oracle provider **não lança** — é silenciosamente permissivo.

Consequência: código que usa `BeginTransaction()` direto abre a transação sem erro, mas
**não terá retentativas** em caso de falha transiente (dead-lock, connection reset, timeout).
Difícil de diagnosticar em produção.

**Padrão obrigatório na Lib (F0-02) — mesmo o provider não forçando:**

```csharp
// ❌ Anti-padrão — funciona mas não tem retry (Oracle não lança, mas Pomelo lança)
await using var tx = await ctx.Database.BeginTransactionAsync();

// ✅ Padrão correto — retry transparente para falhas transientes
var strategy = ctx.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await ctx.Database.BeginTransactionAsync();
    // ... operações da unidade de trabalho ...
    await tx.CommitAsync();
});
```

---

### A3 — `HasCheckConstraint` obsoleto no EF Core 10

`entity.HasCheckConstraint(name, sql)` gera warning `CS0618` no EF Core 10.
A API migrou para `entity.ToTable(t => t.HasCheckConstraint(name, sql))`.

---

### A4 — `SslMode=None` inválido no Oracle connector

O Oracle MySQL connector não reconhece `SslMode=None` (lança "Requested value 'None' was not found.").
Usar `SslMode=Disabled` para desabilitar TLS.

---

## Snippet do relay (critério 6)

```csharp
// Relay outbox — dois workers recebem conjuntos disjuntos de mensagens pendentes.
// Usar FromSqlRaw (não FromSql com interpolação) para evitar parametrização do LIMIT,
// que pode não ser suportada em todos os contextos do Oracle provider.

var messages = await ctx.OutboxMessages
    .FromSqlRaw(
        "SELECT * FROM T_OUTBOX_MESSAGE " +
        "WHERE PUBLISHED_AT IS NULL " +
        "ORDER BY ID " +
        "LIMIT 50 " +
        "FOR UPDATE SKIP LOCKED")
    .AsNoTracking()
    .ToListAsync();
```

---

## Versão fixada do servidor (§6.3)

`MySqlServerVersionInfo.Version = "8.0.36"` — a entrar na Lib no **F0-02**.  
Usado pela `IDesignTimeDbContextFactory` sem auto-detect para SQL determinístico.

---

## Como reproduzir

```bash
# Pré-requisito: Docker + .NET 10 SDK + dotnet-ef 10.x

cd spikes/f0-01-mysql-provider

# Testes de integração (critérios 1–9)
dotnet test Spike.Tests/Spike.Tests.csproj --configuration Release

# Critério 2 — bundle idempotente (requer efbundle compilado)
bash scripts/bundle-test.sh

# Recompilar o bundle se necessário
cd Spike.Data
dotnet ef migrations bundle --self-contained -r linux-x64 -o ../efbundle
```

---

## Próximo passo

Card **F0-02** — migrar todos os serviços para `net10.0` com:
- `MySql.EntityFrameworkCore` `10.0.12`
- `UtcDateTimeConverter` / `UtcDateTimeNullableConverter` extraídos para a Lib
- `MySqlServerVersionInfo.Version = "8.0.36"` como constante pública
- `CreateExecutionStrategy()` padrão obrigatório em todos os repositories
- `SslMode=Disabled` (não `None`) na connection string de dev
