using System;
using TwinCatQA.Application.Configuration;

namespace TwinCatQA.Examples
{
    /// <summary>
    /// ConfigurationService 사용 예제
    /// </summary>
    public class ConfigurationUsageExample
    {
        /// <summary>
        /// 기본 사용 예제: 설정 로드 및 저장
        /// </summary>
        public static void BasicUsageExample()
        {
            Console.WriteLine("=== 기본 사용 예제 ===\n");

            var configService = new ConfigurationService();
            var projectPath = @"D:\TwinCAT_Projects\MyProject";

            // 1. 설정 파일이 없으면 기본 설정 반환
            var settings = configService.LoadSettings(projectPath);
            Console.WriteLine("설정 로드 완료");
            Console.WriteLine(settings.ToSummaryString());

            // 2. 설정 수정
            settings.Global.MaxDegreeOfParallelism = 8;
            settings.Reports.GeneratePdf = true;

            // 3. 설정 저장
            configService.SaveSettings(projectPath, settings);
            Console.WriteLine("\n설정 저장 완료: .twincat-qa/settings.yml\n");
        }

        /// <summary>
        /// 설정 파일 초기화 예제
        /// </summary>
        public static void InitializeSettingsExample()
        {
            Console.WriteLine("=== 설정 파일 초기화 예제 ===\n");

            var configService = new ConfigurationService();
            var projectPath = @"D:\TwinCAT_Projects\NewProject";

            try
            {
                // 프로젝트에 기본 설정 파일 생성
                configService.InitializeSettingsFile(projectPath);
                Console.WriteLine("설정 파일 초기화 완료");
                Console.WriteLine($"생성 위치: {projectPath}\\.twincat-qa\\settings.yml\n");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
                Console.WriteLine("기존 파일을 덮어쓰려면 overwriteExisting: true 옵션을 사용하세요.\n");
            }
        }

        /// <summary>
        /// 규칙 설정 커스터마이징 예제
        /// </summary>
        public static void CustomizeRulesExample()
        {
            Console.WriteLine("=== 규칙 설정 커스터마이징 예제 ===\n");

            var configService = new ConfigurationService();
            var projectPath = @"D:\TwinCAT_Projects\MyProject";

            // 설정 로드
            var settings = configService.LoadSettings(projectPath);

            // 메서드 체이닝으로 규칙 설정 변경
            settings
                .SetRuleEnabled("FR-2-KOREAN-COMMENT", true)
                .SetRuleSeverity("FR-2-KOREAN-COMMENT", ViolationSeverity.Critical)
                .SetRuleParameter("FR-2-KOREAN-COMMENT", "requiredKoreanRatio", 0.90)
                .SetRuleEnabled("FR-7-CYCLOMATIC-COMPLEXITY", true)
                .SetRuleParameter("FR-7-CYCLOMATIC-COMPLEXITY", "criticalThreshold", 25);

            Console.WriteLine("규칙 설정 변경 완료:");
            Console.WriteLine("- FR-2-KOREAN-COMMENT: Critical 심각도, 한글 비율 90%");
            Console.WriteLine("- FR-7-CYCLOMATIC-COMPLEXITY: Critical 임계값 25\n");

            // 변경된 설정 저장
            configService.SaveSettings(projectPath, settings);
            Console.WriteLine("설정 저장 완료\n");
        }

        /// <summary>
        /// 설정 유효성 검사 예제
        /// </summary>
        public static void ValidateSettingsExample()
        {
            Console.WriteLine("=== 설정 유효성 검사 예제 ===\n");

            var configService = new ConfigurationService();
            var settings = configService.GetDefaultSettings();

            // 잘못된 값 설정 (테스트용)
            settings.Global.MaxDegreeOfParallelism = -1;
            settings.Reports.KeepReportsCount = -5;

            // 유효성 검사
            var validationResult = settings.Validate();

            if (validationResult.IsValid)
            {
                Console.WriteLine("설정이 유효합니다.");
            }
            else
            {
                Console.WriteLine("설정에 오류가 있습니다:");
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Git 통합 설정 예제
        /// </summary>
        public static void GitIntegrationExample()
        {
            Console.WriteLine("=== Git 통합 설정 예제 ===\n");

            var configService = new ConfigurationService();
            var projectPath = @"D:\TwinCAT_Projects\MyProject";

            var settings = configService.LoadSettings(projectPath);

            // Git 훅 활성화 및 설정
            settings.Git.EnablePreCommitHook = true;
            settings.Git.BlockOnCriticalViolations = true;
            settings.Git.BlockOnHighViolations = true;
            settings.Git.IncrementalMode = true;

            Console.WriteLine("Git 통합 설정 완료:");
            Console.WriteLine($"- Pre-commit 훅: {(settings.Git.EnablePreCommitHook ? "활성화" : "비활성화")}");
            Console.WriteLine($"- Critical 위반 차단: {(settings.Git.BlockOnCriticalViolations ? "활성화" : "비활성화")}");
            Console.WriteLine($"- High 위반 차단: {(settings.Git.BlockOnHighViolations ? "활성화" : "비활성화")}");
            Console.WriteLine($"- 증분 모드: {(settings.Git.IncrementalMode ? "활성화" : "비활성화")}\n");

            configService.SaveSettings(projectPath, settings);
        }

        /// <summary>
        /// 보고서 설정 예제
        /// </summary>
        public static void ReportSettingsExample()
        {
            Console.WriteLine("=== 보고서 설정 예제 ===\n");

            var configService = new ConfigurationService();
            var settings = configService.GetDefaultSettings();

            // 보고서 설정 커스터마이징
            settings.Reports.GenerateHtml = true;
            settings.Reports.GeneratePdf = true;
            settings.Reports.GenerateJson = true;
            settings.Reports.OutputPath = "reports/quality-checks";
            settings.Reports.FileNameTemplate = "qa-report-{date}-{time}";
            settings.Reports.IncludeSourceCode = true;
            settings.Reports.KeepReportsCount = 20;

            Console.WriteLine("보고서 설정:");
            Console.WriteLine($"- HTML 생성: {settings.Reports.GenerateHtml}");
            Console.WriteLine($"- PDF 생성: {settings.Reports.GeneratePdf}");
            Console.WriteLine($"- JSON 생성: {settings.Reports.GenerateJson}");
            Console.WriteLine($"- 출력 경로: {settings.Reports.OutputPath}");
            Console.WriteLine($"- 파일명 템플릿: {settings.Reports.FileNameTemplate}");
            Console.WriteLine($"- 보관 개수: {settings.Reports.KeepReportsCount}개\n");
        }

        /// <summary>
        /// 모든 예제 실행
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("TwinCAT 코드 품질 검증 도구 - 설정 관리 예제\n");
            Console.WriteLine("================================================\n");

            try
            {
                BasicUsageExample();
                Console.WriteLine("\n------------------------------------------------\n");

                InitializeSettingsExample();
                Console.WriteLine("\n------------------------------------------------\n");

                CustomizeRulesExample();
                Console.WriteLine("\n------------------------------------------------\n");

                ValidateSettingsExample();
                Console.WriteLine("\n------------------------------------------------\n");

                GitIntegrationExample();
                Console.WriteLine("\n------------------------------------------------\n");

                ReportSettingsExample();

                Console.WriteLine("\n================================================");
                Console.WriteLine("모든 예제 실행 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n오류 발생: {ex.Message}");
                Console.WriteLine($"상세 정보: {ex.StackTrace}");
            }
        }
    }
}
