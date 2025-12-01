using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace TwinCatQA.VsExtension.Options
{
    /// <summary>
    /// TwinCAT QA 규칙 설정 페이지
    /// Tools > Options > TwinCAT QA > 규칙
    /// </summary>
    [Guid("7C9F4B2D-8E3A-4F6B-A8D5-2E1C9B4F7A6D")]
    [ComVisible(true)]
    public class RulesOptionsPage : DialogPage
    {
        private QASettings _settings;
        private SettingsManager _settingsManager;
        private RulesOptionsControl _optionsControl;

        public RulesOptionsPage()
        {
            // 설정 관리자는 LoadSettingsFromStorage에서 초기화
        }

        #region 명명 규칙

        [Category("1. 명명 규칙")]
        [DisplayName("NC001: 함수 블록 명명 규칙")]
        [Description("함수 블록이 FB_ 접두사로 시작하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableNC001 { get; set; } = true;

        [Category("1. 명명 규칙")]
        [DisplayName("NC001 심각도")]
        [Description("NC001 규칙의 심각도를 설정합니다.")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityNC001 { get; set; } = RuleSeverity.Warning;

        [Category("1. 명명 규칙")]
        [DisplayName("NC002: 변수 명명 규칙")]
        [Description("변수가 camelCase 명명 규칙을 따르는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableNC002 { get; set; } = true;

        [Category("1. 명명 규칙")]
        [DisplayName("NC002 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityNC002 { get; set; } = RuleSeverity.Warning;

        [Category("1. 명명 규칙")]
        [DisplayName("NC003: 상수 명명 규칙")]
        [Description("상수가 UPPER_CASE 명명 규칙을 따르는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableNC003 { get; set; } = true;

        [Category("1. 명명 규칙")]
        [DisplayName("NC003 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityNC003 { get; set; } = RuleSeverity.Warning;

        [Category("1. 명명 규칙")]
        [DisplayName("NC004: 전역 변수 명명 규칙")]
        [Description("전역 변수가 g 접두사로 시작하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableNC004 { get; set; } = true;

        [Category("1. 명명 규칙")]
        [DisplayName("NC004 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityNC004 { get; set; } = RuleSeverity.Warning;

        #endregion

        #region 복잡도 규칙

        [Category("2. 복잡도")]
        [DisplayName("CC001: 사이클로매틱 복잡도")]
        [Description("함수/메서드의 사이클로매틱 복잡도가 임계값을 초과하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCC001 { get; set; } = true;

        [Category("2. 복잡도")]
        [DisplayName("CC001 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCC001 { get; set; } = RuleSeverity.Warning;

        [Category("2. 복잡도")]
        [DisplayName("CC002: 중첩 깊이")]
        [Description("코드 블록의 중첩 깊이가 임계값을 초과하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCC002 { get; set; } = true;

        [Category("2. 복잡도")]
        [DisplayName("CC002 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCC002 { get; set; } = RuleSeverity.Warning;

        [Category("2. 복잡도")]
        [DisplayName("CC003: 함수 길이")]
        [Description("함수/메서드의 줄 수가 임계값을 초과하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCC003 { get; set; } = true;

        [Category("2. 복잡도")]
        [DisplayName("CC003 심각도")]
        [DefaultValue(RuleSeverity.Info)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCC003 { get; set; } = RuleSeverity.Info;

        #endregion

        #region 코드 품질

        [Category("3. 코드 품질")]
        [DisplayName("CQ001: 주석 누락")]
        [Description("함수/변수에 주석이 없는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCQ001 { get; set; } = true;

        [Category("3. 코드 품질")]
        [DisplayName("CQ001 심각도")]
        [DefaultValue(RuleSeverity.Info)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCQ001 { get; set; } = RuleSeverity.Info;

        [Category("3. 코드 품질")]
        [DisplayName("CQ002: 매직 넘버")]
        [Description("하드코딩된 숫자 리터럴 사용을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCQ002 { get; set; } = true;

        [Category("3. 코드 품질")]
        [DisplayName("CQ002 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCQ002 { get; set; } = RuleSeverity.Warning;

        [Category("3. 코드 품질")]
        [DisplayName("CQ003: 미사용 변수")]
        [Description("선언되었지만 사용되지 않는 변수를 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCQ003 { get; set; } = true;

        [Category("3. 코드 품질")]
        [DisplayName("CQ003 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCQ003 { get; set; } = RuleSeverity.Warning;

        [Category("3. 코드 품질")]
        [DisplayName("CQ004: 중복 코드")]
        [Description("중복되는 코드 블록을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableCQ004 { get; set; } = true;

        [Category("3. 코드 품질")]
        [DisplayName("CQ004 심각도")]
        [DefaultValue(RuleSeverity.Info)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityCQ004 { get; set; } = RuleSeverity.Info;

        #endregion

        #region 안전성

        [Category("4. 안전성")]
        [DisplayName("SF001: 0으로 나누기")]
        [Description("0으로 나누기 가능성이 있는 코드를 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableSF001 { get; set; } = true;

        [Category("4. 안전성")]
        [DisplayName("SF001 심각도")]
        [DefaultValue(RuleSeverity.Error)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeveritySF001 { get; set; } = RuleSeverity.Error;

        [Category("4. 안전성")]
        [DisplayName("SF002: 배열 인덱스 초과")]
        [Description("배열 인덱스 범위 초과 가능성을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableSF002 { get; set; } = true;

        [Category("4. 안전성")]
        [DisplayName("SF002 심각도")]
        [DefaultValue(RuleSeverity.Error)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeveritySF002 { get; set; } = RuleSeverity.Error;

        [Category("4. 안전성")]
        [DisplayName("SF003: NULL 참조")]
        [Description("NULL 참조 가능성이 있는 코드를 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableSF003 { get; set; } = true;

        [Category("4. 안전성")]
        [DisplayName("SF003 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeveritySF003 { get; set; } = RuleSeverity.Warning;

        #endregion

        #region 성능

        [Category("5. 성능")]
        [DisplayName("PF001: 루프 내 복잡한 연산")]
        [Description("루프 내에서 불필요하게 복잡한 연산을 수행하는지 확인합니다.")]
        [DefaultValue(true)]
        public bool EnablePF001 { get; set; } = true;

        [Category("5. 성능")]
        [DisplayName("PF001 심각도")]
        [DefaultValue(RuleSeverity.Info)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityPF001 { get; set; } = RuleSeverity.Info;

        [Category("5. 성능")]
        [DisplayName("PF002: 불필요한 형변환")]
        [Description("불필요한 형변환을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnablePF002 { get; set; } = true;

        [Category("5. 성능")]
        [DisplayName("PF002 심각도")]
        [DefaultValue(RuleSeverity.Info)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityPF002 { get; set; } = RuleSeverity.Info;

        #endregion

        #region IEC 61131-3 표준

        [Category("6. IEC 61131-3 표준")]
        [DisplayName("ST001: 데이터 타입 표준")]
        [Description("IEC 61131-3 표준 데이터 타입 사용을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableST001 { get; set; } = true;

        [Category("6. IEC 61131-3 표준")]
        [DisplayName("ST001 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityST001 { get; set; } = RuleSeverity.Warning;

        [Category("6. IEC 61131-3 표준")]
        [DisplayName("ST002: 제어 구조 표준")]
        [Description("IEC 61131-3 표준 제어 구조 사용을 확인합니다.")]
        [DefaultValue(true)]
        public bool EnableST002 { get; set; } = true;

        [Category("6. IEC 61131-3 표준")]
        [DisplayName("ST002 심각도")]
        [DefaultValue(RuleSeverity.Warning)]
        [TypeConverter(typeof(EnumConverter))]
        public RuleSeverity SeverityST002 { get; set; } = RuleSeverity.Warning;

        #endregion

        #region DialogPage 재정의

        /// <summary>
        /// 설정 로드
        /// </summary>
        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                _settings = _settingsManager.LoadSettings();

                // 각 규칙 설정 로드
                LoadRuleSettings();

                base.LoadSettingsFromStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"규칙 설정 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                if (_settings == null)
                {
                    _settings = new QASettings();
                }

                // 각 규칙 설정 저장
                SaveRuleSettings();

                _settingsManager.SaveSettings(_settings);

                base.SaveSettingsToStorage();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"규칙 설정 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 설정 초기화
        /// </summary>
        public override void ResetSettings()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.GetInstance(Site);
                }

                _settingsManager.ResetSettings();
                LoadSettingsFromStorage();

                base.ResetSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"규칙 설정 초기화 실패: {ex.Message}");
            }
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 규칙 설정 로드
        /// </summary>
        private void LoadRuleSettings()
        {
            // 명명 규칙
            LoadRule("NC001", ref EnableNC001, ref SeverityNC001);
            LoadRule("NC002", ref EnableNC002, ref SeverityNC002);
            LoadRule("NC003", ref EnableNC003, ref SeverityNC003);
            LoadRule("NC004", ref EnableNC004, ref SeverityNC004);

            // 복잡도
            LoadRule("CC001", ref EnableCC001, ref SeverityCC001);
            LoadRule("CC002", ref EnableCC002, ref SeverityCC002);
            LoadRule("CC003", ref EnableCC003, ref SeverityCC003);

            // 코드 품질
            LoadRule("CQ001", ref EnableCQ001, ref SeverityCQ001);
            LoadRule("CQ002", ref EnableCQ002, ref SeverityCQ002);
            LoadRule("CQ003", ref EnableCQ003, ref SeverityCQ003);
            LoadRule("CQ004", ref EnableCQ004, ref SeverityCQ004);

            // 안전성
            LoadRule("SF001", ref EnableSF001, ref SeveritySF001);
            LoadRule("SF002", ref EnableSF002, ref SeveritySF002);
            LoadRule("SF003", ref EnableSF003, ref SeveritySF003);

            // 성능
            LoadRule("PF001", ref EnablePF001, ref SeverityPF001);
            LoadRule("PF002", ref EnablePF002, ref SeverityPF002);

            // 표준
            LoadRule("ST001", ref EnableST001, ref SeverityST001);
            LoadRule("ST002", ref EnableST002, ref SeverityST002);
        }

        /// <summary>
        /// 개별 규칙 로드
        /// </summary>
        private void LoadRule(string ruleId, ref bool enabled, ref RuleSeverity severity)
        {
            var rule = _settings.GetRule(ruleId);
            if (rule != null)
            {
                enabled = rule.Enabled;
                severity = rule.Severity;
            }
        }

        /// <summary>
        /// 규칙 설정 저장
        /// </summary>
        private void SaveRuleSettings()
        {
            // 명명 규칙
            SaveRule("NC001", EnableNC001, SeverityNC001);
            SaveRule("NC002", EnableNC002, SeverityNC002);
            SaveRule("NC003", EnableNC003, SeverityNC003);
            SaveRule("NC004", EnableNC004, SeverityNC004);

            // 복잡도
            SaveRule("CC001", EnableCC001, SeverityCC001);
            SaveRule("CC002", EnableCC002, SeverityCC002);
            SaveRule("CC003", EnableCC003, SeverityCC003);

            // 코드 품질
            SaveRule("CQ001", EnableCQ001, SeverityCQ001);
            SaveRule("CQ002", EnableCQ002, SeverityCQ002);
            SaveRule("CQ003", EnableCQ003, SeverityCQ003);
            SaveRule("CQ004", EnableCQ004, SeverityCQ004);

            // 안전성
            SaveRule("SF001", EnableSF001, SeveritySF001);
            SaveRule("SF002", EnableSF002, SeveritySF002);
            SaveRule("SF003", EnableSF003, SeveritySF003);

            // 성능
            SaveRule("PF001", EnablePF001, SeverityPF001);
            SaveRule("PF002", EnablePF002, SeverityPF002);

            // 표준
            SaveRule("ST001", EnableST001, SeverityST001);
            SaveRule("ST002", EnableST002, SeverityST002);
        }

        /// <summary>
        /// 개별 규칙 저장
        /// </summary>
        private void SaveRule(string ruleId, bool enabled, RuleSeverity severity)
        {
            var rule = _settings.GetRule(ruleId);
            if (rule != null)
            {
                rule.Enabled = enabled;
                rule.Severity = severity;
            }
        }

        #endregion
    }
}
