using System;
using System.Collections.Generic;

namespace TwinCatQA.Application.Configuration
{
    /// <summary>
    /// TwinCAT 코드 품질 검증 도구의 전체 설정을 담는 루트 클래스
    /// </summary>
    public class QualitySettings
    {
        /// <summary>
        /// 전역 설정
        /// </summary>
        public GlobalSettings Global { get; set; } = new GlobalSettings();

        /// <summary>
        /// 검증 규칙 설정
        /// </summary>
        public RuleSettings Rules { get; set; } = new RuleSettings();

        /// <summary>
        /// 보고서 생성 설정
        /// </summary>
        public ReportSettings Reports { get; set; } = new ReportSettings();

        /// <summary>
        /// Git 통합 설정
        /// </summary>
        public GitSettings Git { get; set; } = new GitSettings();
    }

    /// <summary>
    /// 전역 설정 클래스
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// 기본 검증 모드 (Full: 전체 검증, Incremental: 변경된 파일만 검증)
        /// </summary>
        public ValidationMode DefaultMode { get; set; } = ValidationMode.Full;

        /// <summary>
        /// 병렬 처리 활성화 여부
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// 병렬 처리 최대 스레드 수 (기본값: 4)
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// 타임아웃 시간 (초 단위, 기본값: 300초 = 5분)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 로깅 수준 (Debug, Info, Warning, Error)
        /// </summary>
        public string LogLevel { get; set; } = "Info";
    }

    /// <summary>
    /// 검증 규칙 설정 클래스
    /// </summary>
    public class RuleSettings
    {
        /// <summary>
        /// 규칙별 상세 설정 (규칙 ID를 키로 사용)
        /// </summary>
        public Dictionary<string, RuleConfiguration> Configurations { get; set; } = new Dictionary<string, RuleConfiguration>();

        /// <summary>
        /// 모든 규칙 활성화 여부 (기본값: true)
        /// </summary>
        public bool EnableAllRules { get; set; } = true;

        /// <summary>
        /// 커스텀 규칙 디렉토리 경로
        /// </summary>
        public string CustomRulesPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 개별 규칙 설정 클래스
    /// </summary>
    public class RuleConfiguration
    {
        /// <summary>
        /// 규칙 활성화 여부 (기본값: true)
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 위반 심각도 (Info, Low, Medium, High, Critical)
        /// </summary>
        public ViolationSeverity Severity { get; set; } = ViolationSeverity.Medium;

        /// <summary>
        /// 규칙별 매개변수 (규칙마다 다른 설정값)
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 예외 처리할 파일 패턴 (Glob 패턴)
        /// </summary>
        public List<string> ExcludePatterns { get; set; } = new List<string>();
    }

    /// <summary>
    /// 보고서 생성 설정 클래스
    /// </summary>
    public class ReportSettings
    {
        /// <summary>
        /// HTML 보고서 생성 여부 (기본값: true)
        /// </summary>
        public bool GenerateHtml { get; set; } = true;

        /// <summary>
        /// PDF 보고서 생성 여부 (기본값: false)
        /// </summary>
        public bool GeneratePdf { get; set; } = false;

        /// <summary>
        /// JSON 보고서 생성 여부 (기본값: true, CI/CD용)
        /// </summary>
        public bool GenerateJson { get; set; } = true;

        /// <summary>
        /// 보고서 출력 경로 (기본값: .twincat-qa/reports)
        /// </summary>
        public string OutputPath { get; set; } = ".twincat-qa/reports";

        /// <summary>
        /// 보고서에 소스 코드 포함 여부 (기본값: true)
        /// </summary>
        public bool IncludeSourceCode { get; set; } = true;

        /// <summary>
        /// 보고서 파일명 템플릿 (기본값: report-{timestamp})
        /// </summary>
        public string FileNameTemplate { get; set; } = "report-{timestamp}";

        /// <summary>
        /// 이전 보고서 보관 개수 (0: 무제한, 기본값: 10)
        /// </summary>
        public int KeepReportsCount { get; set; } = 10;
    }

    /// <summary>
    /// Git 통합 설정 클래스
    /// </summary>
    public class GitSettings
    {
        /// <summary>
        /// Pre-commit 훅 활성화 여부 (기본값: false)
        /// </summary>
        public bool EnablePreCommitHook { get; set; } = false;

        /// <summary>
        /// Critical 위반 발생 시 커밋 차단 여부 (기본값: true)
        /// </summary>
        public bool BlockOnCriticalViolations { get; set; } = true;

        /// <summary>
        /// High 위반 발생 시 커밋 차단 여부 (기본값: false)
        /// </summary>
        public bool BlockOnHighViolations { get; set; } = false;

        /// <summary>
        /// 증분 검증 모드 활성화 (변경된 파일만 검증, 기본값: true)
        /// </summary>
        public bool IncrementalMode { get; set; } = true;

        /// <summary>
        /// Git 훅 설치 경로 (자동 감지 시 빈 문자열)
        /// </summary>
        public string HooksPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 검증 모드 열거형
    /// </summary>
    public enum ValidationMode
    {
        /// <summary>
        /// 전체 검증 (프로젝트 내 모든 파일)
        /// </summary>
        Full,

        /// <summary>
        /// 증분 검증 (변경된 파일만)
        /// </summary>
        Incremental,

        /// <summary>
        /// 빠른 검증 (중요 규칙만)
        /// </summary>
        Quick
    }

    /// <summary>
    /// 위반 심각도 열거형
    /// </summary>
    public enum ViolationSeverity
    {
        /// <summary>
        /// 정보성 (권장사항)
        /// </summary>
        Info = 0,

        /// <summary>
        /// 낮음 (사소한 문제)
        /// </summary>
        Low = 1,

        /// <summary>
        /// 보통 (개선 필요)
        /// </summary>
        Medium = 2,

        /// <summary>
        /// 높음 (수정 필요)
        /// </summary>
        High = 3,

        /// <summary>
        /// 치명적 (즉시 수정 필요)
        /// </summary>
        Critical = 4
    }
}
