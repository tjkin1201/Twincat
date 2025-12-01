using LibGit2Sharp;

namespace TwinCatQA.Infrastructure.Git;

/// <summary>
/// LibGit2Sharp Patch를 도메인 모델로 변환하는 파서
/// </summary>
public class DiffParser
{
    /// <summary>
    /// Patch를 LineChange 목록으로 변환
    /// </summary>
    /// <param name="patch">LibGit2Sharp Patch 객체</param>
    /// <param name="filePath">파일 경로</param>
    /// <returns>변경된 라인 목록</returns>
    public IReadOnlyList<LineChange> ParsePatch(Patch patch, string filePath)
    {
        var lineChanges = new List<LineChange>();

        if (patch == null)
        {
            return lineChanges;
        }

        // Patch의 각 변경사항 처리
        foreach (var change in patch)
        {
            if (change.Path != filePath && change.OldPath != filePath)
            {
                continue;
            }

            // PatchEntryChanges에서 라인 변경사항 추출
            var patchEntry = change.Patch;
            if (string.IsNullOrEmpty(patchEntry))
            {
                continue;
            }

            // Hunk별로 파싱
            var lines = patchEntry.Split('\n');
            int currentNewLine = 0;
            int currentOldLine = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Hunk 헤더 파싱: @@ -oldStart,oldCount +newStart,newCount @@
                if (line.StartsWith("@@"))
                {
                    var numbers = ExtractLineNumbers(line);
                    currentOldLine = numbers.oldStart;
                    currentNewLine = numbers.newStart;
                    continue;
                }

                // 변경사항 파싱
                if (line.StartsWith("+") && !line.StartsWith("+++"))
                {
                    // 추가된 라인
                    lineChanges.Add(new LineChange
                    {
                        FilePath = filePath,
                        LineNumber = currentNewLine,
                        ChangeType = LineChangeType.Added,
                        Content = line.Substring(1),
                        OldLineNumber = null
                    });
                    currentNewLine++;
                }
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                {
                    // 삭제된 라인
                    lineChanges.Add(new LineChange
                    {
                        FilePath = filePath,
                        LineNumber = currentNewLine,
                        ChangeType = LineChangeType.Deleted,
                        Content = line.Substring(1),
                        OldLineNumber = currentOldLine
                    });
                    currentOldLine++;
                }
                else if (line.StartsWith(" "))
                {
                    // 변경되지 않은 컨텍스트 라인
                    currentNewLine++;
                    currentOldLine++;
                }
            }
        }

        return lineChanges;
    }

    /// <summary>
    /// Hunk 헤더에서 라인 번호 추출
    /// </summary>
    /// <param name="hunkHeader">@@ -oldStart,oldCount +newStart,newCount @@ 형식</param>
    /// <returns>이전/새로운 시작 라인 번호</returns>
    private (int oldStart, int newStart) ExtractLineNumbers(string hunkHeader)
    {
        try
        {
            // @@ -oldStart,oldCount +newStart,newCount @@ 형식 파싱
            var parts = hunkHeader.Split(' ');

            var oldPart = parts.FirstOrDefault(p => p.StartsWith("-"));
            var newPart = parts.FirstOrDefault(p => p.StartsWith("+"));

            int oldStart = 0;
            int newStart = 0;

            if (oldPart != null)
            {
                var oldNumbers = oldPart.Substring(1).Split(',');
                int.TryParse(oldNumbers[0], out oldStart);
            }

            if (newPart != null)
            {
                var newNumbers = newPart.Substring(1).Split(',');
                int.TryParse(newNumbers[0], out newStart);
            }

            return (oldStart, newStart);
        }
        catch
        {
            // 파싱 실패 시 기본값 반환
            return (0, 0);
        }
    }

    /// <summary>
    /// 변경 유형 기호로 ChangeType 결정
    /// </summary>
    /// <param name="status">Git 상태 기호 (+, -, ~)</param>
    /// <returns>변경 유형</returns>
    public LineChangeType DetermineChangeType(char status)
    {
        return status switch
        {
            '+' => LineChangeType.Added,
            '-' => LineChangeType.Deleted,
            '~' => LineChangeType.Modified,
            _ => LineChangeType.Modified
        };
    }

    /// <summary>
    /// TreeEntryChanges 상태로 변경 유형 결정
    /// </summary>
    /// <param name="status">TreeEntryChanges 상태</param>
    /// <returns>변경 유형</returns>
    public LineChangeType DetermineChangeType(ChangeKind status)
    {
        return status switch
        {
            ChangeKind.Added => LineChangeType.Added,
            ChangeKind.Deleted => LineChangeType.Deleted,
            ChangeKind.Modified => LineChangeType.Modified,
            ChangeKind.Renamed => LineChangeType.Modified,
            _ => LineChangeType.Modified
        };
    }
}
