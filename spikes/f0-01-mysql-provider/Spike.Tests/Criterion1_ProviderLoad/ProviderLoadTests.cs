using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Spike.Data;
using System.Reflection;

namespace Spike.Tests.Criterion1_ProviderLoad;

/// <summary>
/// Critério 1: pacote estável (sem -preview / -rc), compatível com EF Core 10 e net10.0.
///
/// Este teste NÃO precisa de container MySQL — valida apenas que o provider
/// carrega corretamente no runtime e não tem sufixo de pré-lançamento.
///
/// Evidência principal de "versão estável" no PackageReference do Spike.Data.csproj:
///   MySql.EntityFrameworkCore Version="10.0.12"
/// (sem -preview / -rc, publicado em 2026-10-21 — registrado no README do spike).
/// </summary>
public sealed class ProviderLoadTests
{
    /// <summary>
    /// Obtém o assembly do provider Oracle MySQL EF.
    /// Estratégia: cria DbContextOptions com UseMySQL e extrai o assembly da extensão
    /// registrada — sem depender do nome interno do tipo (pode variar entre versões).
    /// </summary>
    private static Assembly GetProviderAssembly()
    {
        var options = new DbContextOptionsBuilder<SpikeDbContext>()
            .UseMySQL("Server=127.0.0.1;Database=probe;Uid=root;Pwd=probe;")
            .Options;

        // A extensão do provider tem "MySQL" no nome — é a única que o Oracle registra.
        var providerExtension = options.Extensions
            .First(e => e.GetType().Name.Contains("MySQL", StringComparison.OrdinalIgnoreCase));

        return providerExtension.GetType().Assembly;
    }

    [Fact]
    public void Provider_AssemblyCarregada_NaoContemSufixoPreRelease()
    {
        // Arrange
        var providerAssembly = GetProviderAssembly();

        var informationalVersion = providerAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? string.Empty;

        var fileVersion = providerAssembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()
            ?.Version ?? string.Empty;

        // Assert: versão informacional não contém sufixo de pré-lançamento
        informationalVersion.ToLowerInvariant().Should().NotContain("preview",
            because: "MySql.EntityFrameworkCore 10.0.12 é estável (§6.2, regra 1)");

        informationalVersion.ToLowerInvariant().Should().NotContain("-rc",
            because: "release candidate não é aceitável para produção (§6.2)");

        fileVersion.Should().NotBeNullOrEmpty(
            because: "assembly de release deve ter versão de arquivo");
    }

    [Fact]
    public void Provider_UseMySQL_RetornaOpcoesValidas()
    {
        // Arrange + Act: cria opções sem conexão real — apenas valida que UseMySQL
        // registra a extensão corretamente para net10.0 + EF Core 10.
        var options = new DbContextOptionsBuilder<SpikeDbContext>()
            .UseMySQL("Server=127.0.0.1;Database=probe;Uid=root;Pwd=probe;")
            .Options;

        // Assert
        options.Should().NotBeNull();
        options.Extensions.Should().Contain(
            ext => ext.GetType().Name.Contains("MySQL", StringComparison.OrdinalIgnoreCase),
            because: "UseMySQL deve registrar a extensão do provider Oracle no DbContextOptions");
    }

    [Fact]
    public void Provider_TargetFramework_EhNet10()
    {
        // Garante que o assembly do provider tem TFM net10.0
        // (não está rodando via compatibilidade de versão anterior)
        var providerAssembly = GetProviderAssembly();

        var targetFramework = providerAssembly
            .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()
            ?.FrameworkName ?? string.Empty;

        // net10.0 aparece como ".NETCoreApp,Version=v10.0"
        targetFramework.Should().Contain("10",
            because: "MySql.EntityFrameworkCore 10.0.12 deve ter TFM net10.0 (§6.1, critério 1)");
    }
}
