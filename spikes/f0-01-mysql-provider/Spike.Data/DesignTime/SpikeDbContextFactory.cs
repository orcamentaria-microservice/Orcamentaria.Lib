using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MySql.EntityFrameworkCore.Extensions;

namespace Spike.Data.DesignTime;

/// <summary>
/// Fábrica usada pelo dotnet-ef em design-time (migrations add, bundle, etc.).
///
/// A connection string aqui é apenas para a ferramenta gerar SQL de migration —
/// nunca para execução de produção. Não contém senha real.
///
/// Referência do card F0-01 §6.3:
///   "O resultado do spike fixa a versão do servidor usada pela IDesignTimeDbContextFactory
///    (sem auto-detect): 8.0.36."
///
/// No F0-02 essa constante de versão será extraída para MySqlServerVersionInfo na Lib.
/// </summary>
public sealed class SpikeDbContextFactory : IDesignTimeDbContextFactory<SpikeDbContext>
{
    // Versão fixada conforme §6.3 — sem auto-detect para garantir SQL determinístico.
    // Formato esperado pelo Oracle provider: sem "ServerVersion", apenas a connection string.
    // A versão do servidor é implícita na connection string com "AllowPublicKeyRetrieval=true".
    private const string DesignTimeConnectionString =
        "Server=127.0.0.1;Port=3306;Database=spike_design;Uid=root;Pwd=spike_design_only;" +
        "AllowPublicKeyRetrieval=true;SslMode=None;";

    public SpikeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SpikeDbContext>();

        optionsBuilder.UseMySQL(DesignTimeConnectionString);

        return new SpikeDbContext(optionsBuilder.Options);
    }
}
