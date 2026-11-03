namespace Spike.Data.Entities;

/// <summary>
/// Tabela de outbox mínima conforme especificação do card F0-01 §6.1, critério 6.
/// Usada nos testes de SKIP LOCKED (critério 6, T02).
///
/// DDL de referência (card §6.1):
///   CREATE TABLE T_OUTBOX_MESSAGE (
///     ID          BIGINT NOT NULL AUTO_INCREMENT,
///     EVENT_TYPE  VARCHAR(100) NOT NULL,
///     PAYLOAD     JSON NOT NULL,
///     CREATED_AT  DATETIME(6) NOT NULL,
///     PUBLISHED_AT DATETIME(6) NULL,
///     PRIMARY KEY (ID),
///     KEY IX_OUTBOX_PENDING (PUBLISHED_AT, ID)
///   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/// </summary>
public sealed class OutboxMessage
{
    public long       Id          { get; set; }
    public string     EventType   { get; set; } = string.Empty;

    // Armazenado como JSON no MySQL; lido/escrito como string pela aplicação.
    public string     Payload     { get; set; } = "{}";

    public DateTime   CreatedAt   { get; set; }
    public DateTime?  PublishedAt { get; set; }
}
