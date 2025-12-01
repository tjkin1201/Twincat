using Microsoft.Extensions.DependencyInjection;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Infrastructure.Configuration;
using TwinCatQA.Infrastructure.Reports;

namespace TwinCatQA.Infrastructure;

/// <summary>
/// Infrastructure 레이어 의존성 주입 확장 메서드
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure 서비스를 DI 컨테이너에 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Configuration Services
        services.AddSingleton<QAConfigurationLoader>();
        services.AddScoped<SuppressionChecker>();
        services.AddScoped<RuleFilter>();

        // Report Generators
        services.AddSingleton<IReportGenerator, SimpleHtmlReportGenerator>();

        // 향후 추가될 서비스들
        // services.AddScoped<IGitService, LibGit2SharpGitService>();
        // services.AddScoped<ICompilationService, TwinCatCompilationService>();

        return services;
    }

    /// <summary>
    /// QA Configuration을 로드하고 설정 기반 서비스를 등록
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="projectRootPath">프로젝트 루트 경로</param>
    /// <returns>서비스 컬렉션</returns>
    public static async Task<IServiceCollection> AddInfrastructureWithConfigurationAsync(
        this IServiceCollection services,
        string projectRootPath)
    {
        // 기본 Infrastructure 서비스 등록
        services.AddInfrastructure();

        // 설정 로드를 위한 임시 서비스 프로바이더 생성
        using var tempProvider = services.BuildServiceProvider();
        var loader = tempProvider.GetRequiredService<QAConfigurationLoader>();

        // 설정 로드
        var configuration = await loader.LoadConfigurationAsync(projectRootPath);

        // 설정을 싱글톤으로 등록
        services.AddSingleton(configuration);
        services.AddSingleton(configuration.InlineSuppressions);

        return services;
    }
}
