using System;
using System.Collections.Generic;
using System.Linq;
using TwinCatQA.Application.Models;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.Application.Services
{
    /// <summary>
    /// Chart.js 호환 차트 데이터를 생성하는 헬퍼 클래스
    /// </summary>
    public class ChartDataBuilder
    {
        /// <summary>
        /// 품질 점수 추이 Line 차트 데이터 생성
        /// </summary>
        /// <param name="sessions">검증 세션 목록 (시간순 정렬)</param>
        /// <returns>Chart.js Line 차트 데이터</returns>
        public Models.ChartData BuildQualityTrendData(List<ValidationSession> sessions)
        {
            if (sessions == null || sessions.Count == 0)
            {
                return new Models.ChartData { Type = "line" };
            }

            var sortedSessions = sessions.OrderBy(s => s.StartTime).ToList();

            return new Models.ChartData
            {
                Type = "line",
                Data = new ChartDataSet
                {
                    Labels = sortedSessions.Select(s => s.StartTime.ToString("MM/dd HH:mm")).ToList(),
                    Datasets = new List<Dataset>
                    {
                        new Dataset
                        {
                            Label = "품질 점수",
                            Data = sortedSessions.Select(s => s.OverallQualityScore).ToList(),
                            BackgroundColor = "rgba(75, 192, 192, 0.2)",
                            BorderColor = "rgba(75, 192, 192, 1)",
                            BorderWidth = 2,
                            Tension = 0.4,
                            Fill = true
                        }
                    }
                },
                Options = new Dictionary<string, object>
                {
                    { "responsive", true },
                    { "plugins", new Dictionary<string, object>
                        {
                            { "title", new { display = true, text = "품질 점수 추이" } },
                            { "legend", new { display = true, position = "top" } }
                        }
                    },
                    { "scales", new Dictionary<string, object>
                        {
                            { "y", new { beginAtZero = true, max = 100 } }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 헌장 원칙별 준수율 Radar 차트 데이터 생성
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>Chart.js Radar 차트 데이터</returns>
        public Models.ChartData BuildConstitutionRadarData(ValidationSession session)
        {
            if (session == null)
            {
                return new Models.ChartData { Type = "radar" };
            }

            // 8가지 헌장 원칙
            var principles = new[]
            {
                "명확성", "일관성", "단순성", "모듈화",
                "안전성", "성능", "유지보수성", "표준준수"
            };

            // 각 원칙별 위반 개수 계산
            var violationCounts = new Dictionary<string, int>();
            foreach (var principle in principles)
            {
                violationCounts[principle] = 0;
            }

            foreach (var violation in session.Violations)
            {
                var category = MapRuleToPrinciple(violation.RuleId);
                if (violationCounts.ContainsKey(category))
                {
                    violationCounts[category]++;
                }
            }

            // 준수율 계산 (100 - 위반비율)
            var maxViolations = violationCounts.Values.Max();
            if (maxViolations == 0) maxViolations = 1; // 0으로 나누기 방지

            var complianceScores = principles.Select(p =>
            {
                var violationRatio = (double)violationCounts[p] / maxViolations;
                return Math.Max(0, 100 - (violationRatio * 100));
            }).ToList();

            return new Models.ChartData
            {
                Type = "radar",
                Data = new ChartDataSet
                {
                    Labels = principles.ToList(),
                    Datasets = new List<Dataset>
                    {
                        new Dataset
                        {
                            Label = "준수율 (%)",
                            Data = complianceScores,
                            BackgroundColor = "rgba(54, 162, 235, 0.2)",
                            BorderColor = "rgba(54, 162, 235, 1)",
                            BorderWidth = 2
                        }
                    }
                },
                Options = new Dictionary<string, object>
                {
                    { "responsive", true },
                    { "plugins", new Dictionary<string, object>
                        {
                            { "title", new { display = true, text = "헌장 원칙 준수율" } }
                        }
                    },
                    { "scales", new Dictionary<string, object>
                        {
                            { "r", new { beginAtZero = true, max = 100 } }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 심각도별 위반 분포 Pie 차트 데이터 생성
        /// </summary>
        /// <param name="session">검증 세션</param>
        /// <returns>Chart.js Pie 차트 데이터</returns>
        public Models.ChartData BuildViolationPieData(ValidationSession session)
        {
            if (session == null)
            {
                return new Models.ChartData { Type = "pie" };
            }

            // 심각도별 위반 개수 집계
            var severityCounts = session.Violations
                .GroupBy(v => v.Severity)
                .ToDictionary(g => g.Key, g => g.Count());

            var labels = new List<string>();
            var data = new List<double>();
            var colors = new List<string>();

            // 심각도 순서대로 추가
            AddSeverityData(ViolationSeverity.Critical, "Critical", "#dc3545", severityCounts, labels, data, colors);
            AddSeverityData(ViolationSeverity.High, "High", "#fd7e14", severityCounts, labels, data, colors);
            AddSeverityData(ViolationSeverity.Medium, "Medium", "#ffc107", severityCounts, labels, data, colors);
            AddSeverityData(ViolationSeverity.Low, "Low", "#0dcaf0", severityCounts, labels, data, colors);

            return new Models.ChartData
            {
                Type = "pie",
                Data = new ChartDataSet
                {
                    Labels = labels,
                    Datasets = new List<Dataset>
                    {
                        new Dataset
                        {
                            Label = "위반 개수",
                            Data = data,
                            BackgroundColor = colors,
                            BorderColor = colors.Select(c => c).ToList(),
                            BorderWidth = 1
                        }
                    }
                },
                Options = new Dictionary<string, object>
                {
                    { "responsive", true },
                    { "plugins", new Dictionary<string, object>
                        {
                            { "title", new { display = true, text = "심각도별 위반 분포" } },
                            { "legend", new { display = true, position = "right" } }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// 심각도 데이터를 차트 데이터에 추가
        /// </summary>
        private void AddSeverityData(
            ViolationSeverity severity,
            string label,
            string color,
            Dictionary<ViolationSeverity, int> severityCounts,
            List<string> labels,
            List<double> data,
            List<string> colors)
        {
            if (severityCounts.TryGetValue(severity, out int count) && count > 0)
            {
                labels.Add(label);
                data.Add(count);
                colors.Add(color);
            }
        }

        /// <summary>
        /// 규칙 ID를 헌장 원칙으로 매핑
        /// </summary>
        private string MapRuleToPrinciple(string ruleId)
        {
            // 규칙 ID 접두사 기반 매핑
            if (ruleId.StartsWith("NAM") || ruleId.StartsWith("COM")) return "명확성";
            if (ruleId.StartsWith("STY") || ruleId.StartsWith("FMT")) return "일관성";
            if (ruleId.StartsWith("CPX") || ruleId.StartsWith("DES")) return "단순성";
            if (ruleId.StartsWith("MOD") || ruleId.StartsWith("COH")) return "모듈화";
            if (ruleId.StartsWith("SAF") || ruleId.StartsWith("ERR")) return "안전성";
            if (ruleId.StartsWith("PERF") || ruleId.StartsWith("OPT")) return "성능";
            if (ruleId.StartsWith("MAINT") || ruleId.StartsWith("DOC")) return "유지보수성";
            if (ruleId.StartsWith("STD") || ruleId.StartsWith("IEC")) return "표준준수";

            return "일반"; // 기본값
        }
    }
}
