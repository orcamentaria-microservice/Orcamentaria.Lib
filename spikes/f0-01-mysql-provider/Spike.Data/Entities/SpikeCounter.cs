namespace Spike.Data.Entities;

/// <summary>
/// Contador usado no critério 7: SELECT … FOR UPDATE serializa incrementos concorrentes.
///
/// A entidade é intencionalmente simples — o que se prova é o comportamento do
/// SELECT … FOR UPDATE no banco, não a modelagem do domínio.
///
/// RN-172/173 do projeto: contadores de uso (orçamentos emitidos, usuários ativos, etc.)
/// precisam de atomicidade por tenant — o FOR UPDATE garante que duas requisições
/// simultâneas não percam o incremento uma da outra.
/// </summary>
public sealed class SpikeCounter
{
    public int Id    { get; set; }
    public int Value { get; set; }
}
