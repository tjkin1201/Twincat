using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TwinCatQA.Core.Models;

namespace TwinCatQA.VsExtension.ToolWindows
{
    /// <summary>
    /// 위반 사항 뷰모델
    /// </summary>
    public class ViolationViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 표시 이름
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 위반 사항 데이터
        /// </summary>
        public Violation Violation { get; set; }

        /// <summary>
        /// 심각도
        /// </summary>
        public string Severity => Violation?.Severity.ToString() ?? "정보";

        /// <summary>
        /// 파일 경로
        /// </summary>
        public string FilePath => Violation?.FilePath ?? string.Empty;

        /// <summary>
        /// 라인 번호
        /// </summary>
        public int LineNumber => Violation?.LineNumber ?? 0;

        /// <summary>
        /// 컬럼 번호
        /// </summary>
        public int ColumnNumber => Violation?.ColumnNumber ?? 0;

        /// <summary>
        /// 메시지
        /// </summary>
        public string Message => Violation?.Message ?? string.Empty;

        /// <summary>
        /// 규칙 ID
        /// </summary>
        public string RuleId => Violation?.RuleId ?? string.Empty;

        /// <summary>
        /// 코드 스니펫
        /// </summary>
        public string CodeSnippet => Violation?.CodeSnippet ?? string.Empty;

        /// <summary>
        /// 자식 항목들
        /// </summary>
        public ObservableCollection<ViolationViewModel> Children { get; set; }

        /// <summary>
        /// 확장 여부
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 선택 여부
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 노드 타입 (File, Rule, Violation)
        /// </summary>
        public ViolationNodeType NodeType { get; set; }

        /// <summary>
        /// 위반 개수 (파일 또는 규칙 노드의 경우)
        /// </summary>
        public int ViolationCount { get; set; }

        /// <summary>
        /// 툴팁 텍스트
        /// </summary>
        public string ToolTip
        {
            get
            {
                switch (NodeType)
                {
                    case ViolationNodeType.File:
                        return $"{FilePath}\n위반 개수: {ViolationCount}";
                    case ViolationNodeType.Rule:
                        return $"규칙: {RuleId}\n위반 개수: {ViolationCount}";
                    case ViolationNodeType.Violation:
                        return $"{Message}\n파일: {FilePath}\n위치: 라인 {LineNumber}, 컬럼 {ColumnNumber}\n심각도: {Severity}";
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// 아이콘 경로
        /// </summary>
        public string IconPath
        {
            get
            {
                switch (NodeType)
                {
                    case ViolationNodeType.File:
                        return "pack://application:,,,/TwinCatQA.VsExtension;component/Resources/File.png";
                    case ViolationNodeType.Rule:
                        return "pack://application:,,,/TwinCatQA.VsExtension;component/Resources/Rule.png";
                    case ViolationNodeType.Violation:
                        return GetSeverityIcon();
                    default:
                        return string.Empty;
                }
            }
        }

        public ViolationViewModel()
        {
            Children = new ObservableCollection<ViolationViewModel>();
        }

        /// <summary>
        /// 심각도에 따른 아이콘 반환
        /// </summary>
        private string GetSeverityIcon()
        {
            if (Violation == null) return string.Empty;

            switch (Violation.Severity)
            {
                case ViolationSeverity.Error:
                    return "pack://application:,,,/TwinCatQA.VsExtension;component/Resources/Error.png";
                case ViolationSeverity.Warning:
                    return "pack://application:,,,/TwinCatQA.VsExtension;component/Resources/Warning.png";
                case ViolationSeverity.Info:
                    return "pack://application:,,,/TwinCatQA.VsExtension;component/Resources/Info.png";
                default:
                    return string.Empty;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 위반 사항 노드 타입
    /// </summary>
    public enum ViolationNodeType
    {
        /// <summary>
        /// 파일 노드
        /// </summary>
        File,

        /// <summary>
        /// 규칙 노드
        /// </summary>
        Rule,

        /// <summary>
        /// 위반 사항 노드
        /// </summary>
        Violation
    }
}
