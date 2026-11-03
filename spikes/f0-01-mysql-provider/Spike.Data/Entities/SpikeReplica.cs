namespace Spike.Data.Entities;

/// <summary>
/// Réplica local de uma entidade de outro serviço — critério 8.
///
/// O padrão de réplica do projeto (O1-F3-Px) mantém uma cópia local de dados
/// de outros serviços para evitar chamadas síncronas em runtime. O upsert condicional
/// garante que uma mensagem com versão menor não sobrescreva dados mais recentes
/// (entregas fora de ordem são comuns em sistemas com broker de mensagens).
///
/// Chave: ID é o identificador na origem (não AUTO_INCREMENT aqui).
/// ValueGeneratedNever() no DbContext instrui EF a não gerar o valor.
/// </summary>
public sealed class SpikeReplica
{
    /// <summary>Identificador da entidade na origem (serviço source).</summary>
    public long   Id            { get; set; }

    /// <summary>Dado replicado — representa qualquer coluna de negócio.</summary>
    public string Data          { get; set; } = string.Empty;

    /// <summary>Versão da entidade na origem; upsert só sobrescreve se maior.</summary>
    public int    SourceVersion { get; set; }
}
