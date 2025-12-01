using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using TwinCatQA.Core.Models;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// 마우스 호버 시 코드 품질 위반 사항의 상세 정보를 표시하는 QuickInfo 소스
    /// </summary>
    public class QuickInfoSource : IAsyncQuickInfoSource
    {
        private readonly ITextBuffer _textBuffer;
        private bool _disposed = false;

        /// <summary>
        /// QuickInfoSource 생성자
        /// </summary>
        /// <param name="textBuffer">텍스트 버퍼</param>
        public QuickInfoSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        /// <summary>
        /// 지정된 위치에 대한 QuickInfo 항목을 비동기로 가져오기
        /// </summary>
        /// <param name="session">QuickInfo 세션</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>QuickInfo 항목 또는 null</returns>
        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    return null;
                }

                // 트리거 포인트 가져오기
                var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
                if (!triggerPoint.HasValue)
                {
                    return null;
                }

                // 해당 위치의 위반 사항 찾기
                var violations = GetViolationsAtPosition(triggerPoint.Value);
                if (!violations.Any())
                {
                    return null;
                }

                // QuickInfo 내용 생성
                var content = CreateQuickInfoContent(violations);
                if (content == null)
                {
                    return null;
                }

                // 적용 범위 계산
                var applicableToSpan = GetApplicableSpan(triggerPoint.Value, violations.First());

                return new QuickInfoItem(applicableToSpan, content);
            }, cancellationToken);
        }

        /// <summary>
        /// 지정된 위치의 위반 사항 가져오기
        /// </summary>
        /// <param name="point">텍스트 위치</param>
        /// <returns>위반 사항 목록</returns>
        private List<Violation> GetViolationsAtPosition(SnapshotPoint point)
        {
            var violations = new List<Violation>();

            // QATagger에서 위반 사항 가져오기
            var tagger = QATaggerProvider.GetTagger(_textBuffer);
            if (tagger == null)
            {
                return violations;
            }

            // 현재 위치를 포함하는 스팬 생성
            var span = new SnapshotSpan(point, 0);
            var spans = new NormalizedSnapshotSpanCollection(span);

            // 태그에서 위반 사항 추출
            var tags = tagger.GetTags(spans);
            foreach (var tagSpan in tags)
            {
                if (tagSpan.Tag is ErrorTag errorTag && tagSpan.Span.Contains(point))
                {
                    violations.Add(errorTag.Violation);
                }
            }

            return violations;
        }

        /// <summary>
        /// QuickInfo 내용 생성
        /// </summary>
        /// <param name="violations">위반 사항 목록</param>
        /// <returns>QuickInfo 내용</returns>
        private object CreateQuickInfoContent(List<Violation> violations)
        {
            if (!violations.Any())
            {
                return null;
            }

            var elements = new List<object>();

            foreach (var violation in violations)
            {
                // 제목
                var title = $"[{violation.RuleId}] {GetSeverityText(violation.Severity)}";
                elements.Add(new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, title, ClassifiedTextRunStyle.Bold)
                    )
                ));

                // 메시지
                elements.Add(new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, violation.Message)
                    )
                ));

                // 카테고리
                if (!string.IsNullOrEmpty(violation.Category))
                {
                    elements.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, $"카테고리: {violation.Category}")
                        )
                    ));
                }

                // 수정 제안
                if (!string.IsNullOrEmpty(violation.FixDescription))
                {
                    elements.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, "수정 제안:", ClassifiedTextRunStyle.Bold),
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Text, "\n" + violation.FixDescription)
                        )
                    ));
                }

                // 도움말 링크
                if (!string.IsNullOrEmpty(violation.HelpUrl))
                {
                    elements.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, $"자세한 정보: "),
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.String, violation.HelpUrl,
                                ClassifiedTextRunStyle.UseClassificationFont,
                                () => { System.Diagnostics.Process.Start(violation.HelpUrl); })
                        )
                    ));
                }

                // 위반 사항 간 구분선
                if (violations.Count > 1 && violation != violations.Last())
                {
                    elements.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(
                            new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, "─────────────────────────")
                        )
                    ));
                }
            }

            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        /// <summary>
        /// 적용 범위 계산
        /// </summary>
        /// <param name="point">트리거 포인트</param>
        /// <param name="violation">위반 사항</param>
        /// <returns>적용 범위</returns>
        private ITrackingSpan GetApplicableSpan(SnapshotPoint point, Violation violation)
        {
            var snapshot = point.Snapshot;
            var line = point.GetContainingLine();

            // 위반 범위 계산
            int startPosition, length;

            if (violation.Column > 0 && violation.Length > 0)
            {
                // 정확한 위치와 길이가 지정된 경우
                startPosition = line.Start.Position + violation.Column - 1;
                length = Math.Min(violation.Length, line.End.Position - startPosition);
            }
            else if (violation.Column > 0)
            {
                // 열만 지정된 경우
                startPosition = line.Start.Position + violation.Column - 1;
                length = FindWordEnd(snapshot, startPosition) - startPosition;
            }
            else
            {
                // 위치가 지정되지 않은 경우 현재 단어 범위 사용
                startPosition = point.Position;
                var wordStart = FindWordStart(snapshot, startPosition);
                var wordEnd = FindWordEnd(snapshot, startPosition);
                startPosition = wordStart;
                length = wordEnd - wordStart;
            }

            // 범위 유효성 검사
            startPosition = Math.Max(startPosition, line.Start.Position);
            length = Math.Min(length, line.End.Position - startPosition);

            if (length <= 0)
            {
                length = 1;
            }

            var span = new SnapshotSpan(snapshot, startPosition, length);
            return snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
        }

        /// <summary>
        /// 단어의 시작 위치 찾기
        /// </summary>
        /// <param name="snapshot">스냅샷</param>
        /// <param name="position">현재 위치</param>
        /// <returns>단어 시작 위치</returns>
        private int FindWordStart(ITextSnapshot snapshot, int position)
        {
            if (position <= 0)
            {
                return 0;
            }

            int start = position;
            while (start > 0)
            {
                char c = snapshot[start - 1];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                start--;
            }

            return start;
        }

        /// <summary>
        /// 단어의 끝 위치 찾기
        /// </summary>
        /// <param name="snapshot">스냅샷</param>
        /// <param name="position">현재 위치</param>
        /// <returns>단어 끝 위치</returns>
        private int FindWordEnd(ITextSnapshot snapshot, int position)
        {
            if (position >= snapshot.Length)
            {
                return snapshot.Length;
            }

            int end = position;
            while (end < snapshot.Length)
            {
                char c = snapshot[end];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                end++;
            }

            return end > position ? end : position + 1;
        }

        /// <summary>
        /// 심각도 텍스트 반환
        /// </summary>
        /// <param name="severity">심각도</param>
        /// <returns>심각도 텍스트</returns>
        private string GetSeverityText(ViolationSeverity severity)
        {
            switch (severity)
            {
                case ViolationSeverity.Error:
                    return "오류";
                case ViolationSeverity.Warning:
                    return "경고";
                case ViolationSeverity.Information:
                    return "정보";
                default:
                    return "알림";
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
