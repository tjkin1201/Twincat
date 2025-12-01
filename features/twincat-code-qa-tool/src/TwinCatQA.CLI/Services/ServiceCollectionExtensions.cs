using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwinCatQA.Application.Services;
using TwinCatQA.Domain.Contracts;
using TwinCatQA.Domain.Services;
using TwinCatQA.Infrastructure.Analysis;
using TwinCatQA.Infrastructure.Analysis.Confidence;
using TwinCatQA.Infrastructure.Compilation;
using TwinCatQA.Infrastructure.Configuration;
using TwinCatQA.Infrastructure.Parsers;
using TwinCatQA.Infrastructure.QA.Rules;
using TwinCatQA.Infrastructure.QA.Rules.TE1200;

namespace TwinCatQA.CLI.Services;

/// <summary>
/// 의존성 주입 설정 확장 메서드
///
/// CLI 애플리케이션에 필요한 모든 서비스를 등록합니다.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// TwinCAT QA 서비스 등록
    /// </summary>
    public static IServiceCollection AddTwinCatQAServices(this IServiceCollection services)
    {
        // 로깅
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Infrastructure Layer - 파싱
        services.AddSingleton<IParserService, AntlrParserService>();

        // Infrastructure Layer - 컴파일
        services.AddSingleton<ICompilationService, TwinCatCompilationService>();

        // Infrastructure Layer - 분석
        services.AddSingleton<IVariableUsageAnalyzer, VariableUsageAnalyzer>();
        services.AddSingleton<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddSingleton<IIOMappingValidator, IOMappingValidator>();

        // Application Layer - 오케스트레이터
        services.AddSingleton<IAdvancedAnalysisOrchestrator, AdvancedAnalysisOrchestrator>();

        // Application Layer - 시각화
        services.AddSingleton<GraphvizVisualizationService>();

        // Application Layer - QA 분석
        services.AddSingleton<QaAnalysisService>();
        services.AddSingleton<QaReportGenerator>();

        // Level 2 - AST 기반 분석 서비스
        services.AddSingleton<ConfidenceCalculator>();
        services.AddSingleton<QAConfigurationLoader>();
        services.AddSingleton<IEnhancedQAAnalysisService, EnhancedQAAnalysisService>();

        // Infrastructure Layer - QA 규칙 (기존 20개)
        services.AddSingleton<IQARuleChecker, TypeNarrowingRule>();
        services.AddSingleton<IQARuleChecker, NullCheckRule>();
        services.AddSingleton<IQARuleChecker, MagicNumberRule>();
        services.AddSingleton<IQARuleChecker, LongFunctionRule>();
        services.AddSingleton<IQARuleChecker, DeepNestingRule>();
        services.AddSingleton<IQARuleChecker, UnusedVariableRule>();
        services.AddSingleton<IQARuleChecker, UninitializedVariableRule>();
        services.AddSingleton<IQARuleChecker, InsufficientCommentsRule>();
        services.AddSingleton<IQARuleChecker, NamingConventionRule>();
        services.AddSingleton<IQARuleChecker, HighComplexityRule>();
        services.AddSingleton<IQARuleChecker, DuplicateCodeRule>();
        services.AddSingleton<IQARuleChecker, GlobalVariableOveruseRule>();
        services.AddSingleton<IQARuleChecker, TooManyParametersRule>();
        services.AddSingleton<IQARuleChecker, FloatingPointComparisonRule>();
        services.AddSingleton<IQARuleChecker, ArrayBoundsRule>();
        services.AddSingleton<IQARuleChecker, InfiniteLoopRiskRule>();
        services.AddSingleton<IQARuleChecker, HardcodedIOAddressRule>();
        services.AddSingleton<IQARuleChecker, MissingCaseElseRule>();
        services.AddSingleton<IQARuleChecker, InconsistentStyleRule>();
        services.AddSingleton<IQARuleChecker, ExcessivelyLongNameRule>();

        // TE1200 Static Analysis 규칙 (SA0001-SA0180, 180개)
        // Beckhoff TE1200 호환 규칙들
        foreach (var saRule in TE1200RuleRegistration.GetAllSARules())
        {
            services.AddSingleton<IQARuleChecker>(saRule);
        }

        return services;
    }
}
