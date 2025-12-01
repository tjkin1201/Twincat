using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace TwinCatQA.VsExtension.ToolWindows
{
    /// <summary>
    /// TwinCAT QA 결과를 표시하는 Tool Window
    /// </summary>
    [Guid("9F4A5E2D-8B3C-4D7F-A1E6-3C8B9D5F7E2A")]
    public class QAResultsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// QA Results Tool Window 생성자
        /// </summary>
        public QAResultsToolWindow() : base(null)
        {
            // 툴 윈도우 제목 설정
            this.Caption = "TwinCAT QA 결과";

            // 툴바 설정 (필요한 경우)
            this.ToolBar = new System.ComponentModel.Design.CommandID(
                GuidList.guidTwinCatQAToolWindowCmdSet,
                PkgCmdIDList.QAResultsToolbar);

            // WPF 컨트롤 설정
            this.Content = new QAResultsToolWindowControl();
        }
    }

    /// <summary>
    /// GUID 정의
    /// </summary>
    internal static class GuidList
    {
        /// <summary>
        /// 패키지 GUID
        /// </summary>
        public const string guidTwinCatQAPackageString = "A7B3C8D1-2E4F-5G6H-7I8J-9K0L1M2N3O4P";
        public static readonly Guid guidTwinCatQAPackage = new Guid(guidTwinCatQAPackageString);

        /// <summary>
        /// 커맨드 셋 GUID
        /// </summary>
        public const string guidTwinCatQAToolWindowCmdSetString = "B8C4D9E2-3F5G-6H7I-8J9K-0L1M2N3O4P5Q";
        public static readonly Guid guidTwinCatQAToolWindowCmdSet = new Guid(guidTwinCatQAToolWindowCmdSetString);

        /// <summary>
        /// 툴 윈도우 GUID
        /// </summary>
        public const string guidQAResultsToolWindowString = "9F4A5E2D-8B3C-4D7F-A1E6-3C8B9D5F7E2A";
        public static readonly Guid guidQAResultsToolWindow = new Guid(guidQAResultsToolWindowString);
    }

    /// <summary>
    /// 커맨드 ID 정의
    /// </summary>
    internal static class PkgCmdIDList
    {
        /// <summary>
        /// QA Results Tool Window 표시 커맨드
        /// </summary>
        public const int ShowQAResultsToolWindow = 0x0100;

        /// <summary>
        /// QA Results Toolbar
        /// </summary>
        public const int QAResultsToolbar = 0x1000;

        /// <summary>
        /// 새로고침 커맨드
        /// </summary>
        public const int RefreshCommand = 0x1001;

        /// <summary>
        /// 설정 커맨드
        /// </summary>
        public const int SettingsCommand = 0x1002;

        /// <summary>
        /// 내보내기 커맨드
        /// </summary>
        public const int ExportCommand = 0x1003;

        /// <summary>
        /// 분석 실행 커맨드
        /// </summary>
        public const int AnalyzeCommand = 0x1004;

        /// <summary>
        /// 현재 파일 분석 커맨드
        /// </summary>
        public const int AnalyzeCurrentFileCommand = 0x1005;

        /// <summary>
        /// 프로젝트 분석 커맨드
        /// </summary>
        public const int AnalyzeProjectCommand = 0x1006;

        /// <summary>
        /// 솔루션 분석 커맨드
        /// </summary>
        public const int AnalyzeSolutionCommand = 0x1007;
    }

    /// <summary>
    /// 툴 윈도우 제공자 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ProvideToolWindowAttribute : Attribute
    {
        private readonly Type _toolWindowType;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="toolWindowType">툴 윈도우 타입</param>
        public ProvideToolWindowAttribute(Type toolWindowType)
        {
            _toolWindowType = toolWindowType ?? throw new ArgumentNullException(nameof(toolWindowType));
        }

        /// <summary>
        /// 툴 윈도우 타입
        /// </summary>
        public Type ToolWindowType => _toolWindowType;

        /// <summary>
        /// 스타일 플래그
        /// </summary>
        public VsDockStyle Style { get; set; } = VsDockStyle.Tabbed;

        /// <summary>
        /// 도킹 위치 GUID
        /// </summary>
        public string Window { get; set; } = Microsoft.VisualStudio.Shell.Interop.ToolWindowGuids.Outputwindow;

        /// <summary>
        /// 다중 인스턴스 허용 여부
        /// </summary>
        public bool MultiInstances { get; set; } = false;

        /// <summary>
        /// 일시적 여부
        /// </summary>
        public bool Transient { get; set; } = false;

        /// <summary>
        /// 도구 상자 위치
        /// </summary>
        public int Orientation { get; set; } = (int)Microsoft.VisualStudio.Shell.Interop.ToolWindowOrientation.none;

        /// <summary>
        /// 표시 이름 리소스 ID
        /// </summary>
        public int NameResourceID { get; set; } = 0;
    }

    /// <summary>
    /// 도킹 스타일
    /// </summary>
    public enum VsDockStyle
    {
        /// <summary>
        /// 탭으로 표시
        /// </summary>
        Tabbed = 0,

        /// <summary>
        /// 떠있는 상태
        /// </summary>
        Float = 1,

        /// <summary>
        /// 링크됨
        /// </summary>
        Linked = 2,

        /// <summary>
        /// MDI 문서
        /// </summary>
        MDI = 3,

        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = -1
    }
}
