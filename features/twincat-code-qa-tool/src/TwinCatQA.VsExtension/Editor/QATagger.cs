using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using TwinCatQA.Core.Models;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// 코드 품질 위반 사항을 에디터에 표시하는 태거
    /// Squiggles(물결 밑줄)로 위반 사항을 표시
    /// </summary>
    public class QATagger : ITagger<IErrorTag>
    {
        private readonly ITextBuffer _buffer;
        private readonly Dictionary<int, List<Violation>> _violations = new Dictionary<int, List<Violation>>();
        private readonly object _lock = new object();

        /// <summary>
        /// 태그 변경 이벤트
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// QATagger 생성자
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        public QATagger(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        /// <summary>
        /// 위반 사항 업데이트
        /// </summary>
        /// <param name="violations">위반 사항 목록</param>
        public void UpdateViolations(IEnumerable<Violation> violations)
        {
            lock (_lock)
            {
                // 기존 위반 사항 제거
                _violations.Clear();

                // 라인 번호별로 위반 사항 그룹화
                foreach (var violation in violations)
                {
                    if (!_violations.ContainsKey(violation.LineNumber))
                    {
                        _violations[violation.LineNumber] = new List<Violation>();
                    }
                    _violations[violation.LineNumber].Add(violation);
                }
            }

            // 전체 버퍼에 대해 태그 변경 알림
            RaiseTagsChanged(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length));
        }

        /// <summary>
        /// 특정 라인의 위반 사항 추가
        /// </summary>
        /// <param name="violation">위반 사항</param>
        public void AddViolation(Violation violation)
        {
            lock (_lock)
            {
                if (!_violations.ContainsKey(violation.LineNumber))
                {
                    _violations[violation.LineNumber] = new List<Violation>();
                }
                _violations[violation.LineNumber].Add(violation);
            }

            // 해당 라인에 대해서만 태그 변경 알림
            try
            {
                var snapshot = _buffer.CurrentSnapshot;
                if (violation.LineNumber > 0 && violation.LineNumber <= snapshot.LineCount)
                {
                    var line = snapshot.GetLineFromLineNumber(violation.LineNumber - 1);
                    RaiseTagsChanged(new SnapshotSpan(snapshot, line.Start, line.Length));
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // 라인 번호가 범위를 벗어난 경우 무시
            }
        }

        /// <summary>
        /// 모든 위반 사항 제거
        /// </summary>
        public void ClearViolations()
        {
            lock (_lock)
            {
                _violations.Clear();
            }

            RaiseTagsChanged(new SnapshotSpan(_buffer.CurrentSnapshot, 0, _buffer.CurrentSnapshot.Length));
        }

        /// <summary>
        /// 지정된 범위에 대한 태그 반환
        /// </summary>
        /// <param name="spans">검사할 범위 목록</param>
        /// <returns>태그 목록</returns>
        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                yield break;
            }

            var snapshot = spans[0].Snapshot;

            lock (_lock)
            {
                foreach (var span in spans)
                {
                    // 범위 내의 모든 라인 검사
                    var startLine = span.Start.GetContainingLine().LineNumber + 1; // 1-based
                    var endLine = span.End.GetContainingLine().LineNumber + 1; // 1-based

                    for (int lineNumber = startLine; lineNumber <= endLine; lineNumber++)
                    {
                        if (!_violations.ContainsKey(lineNumber))
                        {
                            continue;
                        }

                        // 해당 라인의 위반 사항 처리
                        foreach (var violation in _violations[lineNumber])
                        {
                            var tagSpan = CreateTagSpan(snapshot, violation);
                            if (tagSpan != null)
                            {
                                yield return tagSpan;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 위반 사항에 대한 태그 스팬 생성
        /// </summary>
        /// <param name="snapshot">스냅샷</param>
        /// <param name="violation">위반 사항</param>
        /// <returns>태그 스팬 또는 null</returns>
        private ITagSpan<IErrorTag> CreateTagSpan(ITextSnapshot snapshot, Violation violation)
        {
            try
            {
                // 라인 번호 유효성 검사 (1-based -> 0-based)
                if (violation.LineNumber < 1 || violation.LineNumber > snapshot.LineCount)
                {
                    return null;
                }

                var line = snapshot.GetLineFromLineNumber(violation.LineNumber - 1);

                // 위반 범위 계산
                int startPosition, length;

                if (violation.Column > 0 && violation.Length > 0)
                {
                    // 정확한 위치와 길이가 지정된 경우
                    startPosition = line.Start.Position + violation.Column - 1; // 1-based -> 0-based
                    length = Math.Min(violation.Length, line.End.Position - startPosition);
                }
                else if (violation.Column > 0)
                {
                    // 열만 지정된 경우 - 단어 끝까지 표시
                    startPosition = line.Start.Position + violation.Column - 1;
                    length = FindWordEnd(snapshot, startPosition) - startPosition;
                }
                else
                {
                    // 위치가 지정되지 않은 경우 - 전체 라인 표시
                    startPosition = line.Start.Position;
                    length = line.Length;
                }

                // 범위 유효성 검사
                if (startPosition < line.Start.Position || startPosition + length > line.End.Position)
                {
                    // 범위가 라인을 벗어나는 경우 라인 끝까지만 표시
                    startPosition = Math.Max(startPosition, line.Start.Position);
                    length = Math.Min(length, line.End.Position - startPosition);
                }

                if (length <= 0)
                {
                    return null;
                }

                var span = new SnapshotSpan(snapshot, startPosition, length);
                var tag = new ErrorTag(violation);

                return new TagSpan<IErrorTag>(span, tag);
            }
            catch (ArgumentOutOfRangeException)
            {
                // 범위를 벗어난 경우 null 반환
                return null;
            }
        }

        /// <summary>
        /// 단어의 끝 위치 찾기
        /// </summary>
        /// <param name="snapshot">스냅샷</param>
        /// <param name="startPosition">시작 위치</param>
        /// <returns>단어 끝 위치</returns>
        private int FindWordEnd(ITextSnapshot snapshot, int startPosition)
        {
            if (startPosition >= snapshot.Length)
            {
                return startPosition;
            }

            int position = startPosition;
            while (position < snapshot.Length)
            {
                char c = snapshot[position];
                if (!char.IsLetterOrDigit(c) && c != '_')
                {
                    break;
                }
                position++;
            }

            // 최소 1글자는 표시
            return position > startPosition ? position : startPosition + 1;
        }

        /// <summary>
        /// 태그 변경 이벤트 발생
        /// </summary>
        /// <param name="span">변경된 범위</param>
        private void RaiseTagsChanged(SnapshotSpan span)
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }
    }
}
