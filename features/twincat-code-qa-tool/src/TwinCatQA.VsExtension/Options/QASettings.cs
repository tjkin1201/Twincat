using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// TwinCAT QA 도구 설정 데이터 모델
    /// </summary>
    public class QASettings : INotifyPropertyChanged
    {
        private bool _enableAutoAnalysis;
        private int _analysisDelayMs;
        private OutputVerbosity _verbosity;
        private string _projectRoot;
        private string _outputFolder;
        private Dictionary<string, RuleConfiguration> _ruleConfigurations;

        public event PropertyChangedEventHandler PropertyChanged;

        public QASettings()
        {
            // 기본값 설정
            _enableAutoAnalysis = true;
            _analysisDelayMs = 500;
            _verbosity = OutputVerbosity.Normal;
            _projectRoot = string.Empty;
            _outputFolder = string.Empty;
            _ruleConfigurations = new Dictionary<string, RuleConfiguration>();

            InitializeDefaultRules();
        }

        /// <summary>
        /// 자동 분석 활성화 여부
        /// </summary>
        public bool EnableAutoAnalysis
        {
            get => _enableAutoAnalysis;
            set
            {
                if (_enableAutoAnalysis != value)
                {
                    _enableAutoAnalysis = value;
                    OnPropertyChanged(nameof(EnableAutoAnalysis));
                }
            }
        }

        /// <summary>
        /// 분석 지연 시간 (밀리초)
        /// </summary>
        public int AnalysisDelayMs
        {
            get => _analysisDelayMs;
            set
            {
                if (_analysisDelayMs != value)
                {
                    _analysisDelayMs = Math.Max(100, Math.Min(5000, value));
                    OnPropertyChanged(nameof(AnalysisDelayMs));
                }
            }
        }

        /// <summary>
        /// 출력 상세도
        /// </summary>
        public OutputVerbosity Verbosity
        {
            get => _verbosity;
            set
            {
                if (_verbosity != value)
                {
                    _verbosity = value;
                    OnPropertyChanged(nameof(Verbosity));
                }
            }
        }

        /// <summary>
        /// 프로젝트 루트 경로
        /// </summary>
        public string ProjectRoot
        {
            get => _projectRoot;
            set
            {
                if (_projectRoot != value)
                {
                    _projectRoot = value ?? string.Empty;
                    OnPropertyChanged(nameof(ProjectRoot));
                }
            }
        }

        /// <summary>
        /// 출력 폴더 경로
        /// </summary>
        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (_outputFolder != value)
                {
                    _outputFolder = value ?? string.Empty;
                    OnPropertyChanged(nameof(OutputFolder));
                }
            }
        }

        /// <summary>
        /// 규칙 설정 컬렉션
        /// </summary>
        public Dictionary<string, RuleConfiguration> RuleConfigurations
        {
            get => _ruleConfigurations;
            set
            {
                if (_ruleConfigurations != value)
                {
                    _ruleConfigurations = value ?? new Dictionary<string, RuleConfiguration>();
                    OnPropertyChanged(nameof(RuleConfigurations));
                }
            }
        }

        /// <summary>
        /// 기본 규칙 초기화
        /// </summary>
        private void InitializeDefaultRules()
        {
            // 명명 규칙
            AddRule("NC001", "함수 블록 명명 규칙", RuleSeverity.Warning, true);
            AddRule("NC002", "변수 명명 규칙", RuleSeverity.Warning, true);
            AddRule("NC003", "상수 명명 규칙", RuleSeverity.Warning, true);
            AddRule("NC004", "전역 변수 명명 규칙", RuleSeverity.Warning, true);

            // 복잡도 규칙
            AddRule("CC001", "사이클로매틱 복잡도 초과", RuleSeverity.Warning, true);
            AddRule("CC002", "중첩 깊이 초과", RuleSeverity.Warning, true);
            AddRule("CC003", "함수 길이 초과", RuleSeverity.Info, true);

            // 코드 품질 규칙
            AddRule("CQ001", "주석 누락", RuleSeverity.Info, true);
            AddRule("CQ002", "매직 넘버 사용", RuleSeverity.Warning, true);
            AddRule("CQ003", "미사용 변수", RuleSeverity.Warning, true);
            AddRule("CQ004", "중복 코드", RuleSeverity.Info, true);

            // 안전성 규칙
            AddRule("SF001", "0으로 나누기 가능성", RuleSeverity.Error, true);
            AddRule("SF002", "배열 인덱스 범위 초과", RuleSeverity.Error, true);
            AddRule("SF003", "NULL 참조 가능성", RuleSeverity.Warning, true);

            // 성능 규칙
            AddRule("PF001", "루프 내 복잡한 연산", RuleSeverity.Info, true);
            AddRule("PF002", "불필요한 형변환", RuleSeverity.Info, true);

            // IEC 61131-3 표준 규칙
            AddRule("ST001", "표준 준수: 데이터 타입", RuleSeverity.Warning, true);
            AddRule("ST002", "표준 준수: 제어 구조", RuleSeverity.Warning, true);
        }

        /// <summary>
        /// 규칙 추가
        /// </summary>
        private void AddRule(string id, string description, RuleSeverity severity, bool enabled)
        {
            _ruleConfigurations[id] = new RuleConfiguration
            {
                Id = id,
                Description = description,
                Severity = severity,
                Enabled = enabled
            };
        }

        /// <summary>
        /// 특정 규칙 가져오기
        /// </summary>
        public RuleConfiguration GetRule(string ruleId)
        {
            return _ruleConfigurations.TryGetValue(ruleId, out var config)
                ? config
                : null;
        }

        /// <summary>
        /// 설정 복사본 생성
        /// </summary>
        public QASettings Clone()
        {
            var clone = new QASettings
            {
                EnableAutoAnalysis = this.EnableAutoAnalysis,
                AnalysisDelayMs = this.AnalysisDelayMs,
                Verbosity = this.Verbosity,
                ProjectRoot = this.ProjectRoot,
                OutputFolder = this.OutputFolder
            };

            foreach (var kvp in _ruleConfigurations)
            {
                clone._ruleConfigurations[kvp.Key] = kvp.Value.Clone();
            }

            return clone;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 개별 규칙 설정
    /// </summary>
    public class RuleConfiguration
    {
        /// <summary>
        /// 규칙 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 규칙 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 규칙 심각도
        /// </summary>
        public RuleSeverity Severity { get; set; }

        /// <summary>
        /// 규칙 활성화 여부
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 규칙 복사본 생성
        /// </summary>
        public RuleConfiguration Clone()
        {
            return new RuleConfiguration
            {
                Id = this.Id,
                Description = this.Description,
                Severity = this.Severity,
                Enabled = this.Enabled
            };
        }
    }

    /// <summary>
    /// 규칙 심각도
    /// </summary>
    public enum RuleSeverity
    {
        /// <summary>
        /// 정보
        /// </summary>
        [Description("정보")]
        Info = 0,

        /// <summary>
        /// 경고
        /// </summary>
        [Description("경고")]
        Warning = 1,

        /// <summary>
        /// 오류
        /// </summary>
        [Description("오류")]
        Error = 2
    }

    /// <summary>
    /// 출력 상세도
    /// </summary>
    public enum OutputVerbosity
    {
        /// <summary>
        /// 최소
        /// </summary>
        [Description("최소")]
        Minimal = 0,

        /// <summary>
        /// 일반
        /// </summary>
        [Description("일반")]
        Normal = 1,

        /// <summary>
        /// 상세
        /// </summary>
        [Description("상세")]
        Detailed = 2,

        /// <summary>
        /// 진단
        /// </summary>
        [Description("진단")]
        Diagnostic = 3
    }
}
