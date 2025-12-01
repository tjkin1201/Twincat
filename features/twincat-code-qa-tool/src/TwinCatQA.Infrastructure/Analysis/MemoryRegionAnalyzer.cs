using System.Text.RegularExpressions;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// 메모리 영역 분석기 구현
/// PLC I/O 주소 매핑의 중복, 충돌, 단편화를 검출
/// </summary>
public class MemoryRegionAnalyzer : IMemoryRegionAnalyzer
{
    // I/O 주소 패턴: %IX0.0, %QW10, %MD100 등
    private static readonly Regex AddressPattern = new(
        @"AT\s+%([IQM])([XBWDL]?)(\d+)(?:\.(\d+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MemoryRegionAnalysis Analyze(ValidationSession session)
    {
        var allocations = ExtractAllocations(session);
        var conflicts = DetectConflicts(allocations);
        var statistics = CalculateStatistics(allocations);

        return new MemoryRegionAnalysis
        {
            Allocations = allocations,
            Conflicts = conflicts,
            Statistics = statistics
        };
    }

    /// <summary>
    /// 모든 메모리 할당 추출
    /// </summary>
    private List<MemoryAllocation> ExtractAllocations(ValidationSession session)
    {
        var allocations = new List<MemoryAllocation>();

        foreach (var file in session.Files)
        {
            var lines = file.Content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var match = AddressPattern.Match(lines[i]);
                if (match.Success)
                {
                    var allocation = ParseAllocation(match, file.FilePath, i + 1, lines[i]);
                    if (allocation != null)
                    {
                        allocations.Add(allocation);
                    }
                }
            }
        }

        return allocations.OrderBy(a => a.RegionType)
                          .ThenBy(a => a.StartAddress)
                          .ThenBy(a => a.BitOffset)
                          .ToList();
    }

    /// <summary>
    /// 주소 문자열 파싱
    /// </summary>
    private MemoryAllocation? ParseAllocation(Match match, string filePath, int line, string rawLine)
    {
        var regionChar = match.Groups[1].Value.ToUpper();
        var sizeChar = match.Groups[2].Value.ToUpper();
        var address = int.Parse(match.Groups[3].Value);
        var bitOffset = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

        var regionType = regionChar switch
        {
            "I" => MemoryRegionType.Input,
            "Q" => MemoryRegionType.Output,
            "M" => MemoryRegionType.Memory,
            _ => MemoryRegionType.Unknown
        };

        var size = sizeChar switch
        {
            "X" or "" => MemorySize.Bit,
            "B" => MemorySize.Byte,
            "W" => MemorySize.Word,
            "D" => MemorySize.DWord,
            "L" => MemorySize.LWord,
            _ => MemorySize.Bit
        };

        // 변수명 추출 시도
        var varNameMatch = Regex.Match(rawLine, @"(\w+)\s*(?:AT|:)");
        var varName = varNameMatch.Success ? varNameMatch.Groups[1].Value : "Unknown";

        // 데이터 타입 추출 시도
        var typeMatch = Regex.Match(rawLine, @":\s*(\w+)");
        var dataType = typeMatch.Success ? typeMatch.Groups[1].Value : "Unknown";

        return new MemoryAllocation
        {
            VariableName = varName,
            FilePath = filePath,
            Line = line,
            RegionType = regionType,
            Size = size,
            StartAddress = address,
            BitOffset = bitOffset,
            RawAddress = match.Value,
            DataType = dataType
        };
    }

    /// <summary>
    /// 메모리 충돌 검출
    /// </summary>
    private List<MemoryConflict> DetectConflicts(List<MemoryAllocation> allocations)
    {
        var conflicts = new List<MemoryConflict>();

        // 영역별로 그룹화
        var byRegion = allocations.GroupBy(a => a.RegionType);

        foreach (var group in byRegion)
        {
            var regionAllocations = group.ToList();

            for (int i = 0; i < regionAllocations.Count; i++)
            {
                for (int j = i + 1; j < regionAllocations.Count; j++)
                {
                    var conflict = CheckConflict(regionAllocations[i], regionAllocations[j]);
                    if (conflict != null)
                    {
                        conflicts.Add(conflict);
                    }
                }
            }

            // 단편화 검사
            var fragmentationConflicts = CheckFragmentation(regionAllocations);
            conflicts.AddRange(fragmentationConflicts);
        }

        return conflicts;
    }

    /// <summary>
    /// 두 할당 간 충돌 검사 (모든 타입 적용)
    /// </summary>
    private MemoryConflict? CheckConflict(MemoryAllocation a, MemoryAllocation b)
    {
        // 1. 동일 주소 완전 중복 (타입 무관)
        if (a.StartAddress == b.StartAddress && a.BitOffset == b.BitOffset)
        {
            // 동일 크기 중복
            if (a.Size == b.Size)
            {
                return new MemoryConflict
                {
                    ConflictType = MemoryConflictType.Overlap,
                    First = a,
                    Second = b,
                    Severity = IssueSeverity.Critical,
                    Message = $"동일 주소 중복: {a.RawAddress} - {a.VariableName}({a.DataType})와 {b.VariableName}({b.DataType})"
                };
            }

            // 다른 크기로 동일 시작 주소 사용 (타입 불일치)
            return new MemoryConflict
            {
                ConflictType = MemoryConflictType.TypeMismatch,
                First = a,
                Second = b,
                Severity = IssueSeverity.Critical,
                Message = $"동일 주소 다른 크기: {a.RawAddress} - {a.VariableName}({a.Size})와 {b.VariableName}({b.Size})"
            };
        }

        // 2. 비트와 바이트/워드 혼용 검사
        if (a.Size == MemorySize.Bit && b.Size != MemorySize.Bit)
        {
            if (a.StartAddress >= b.StartAddress && a.StartAddress < b.EndAddress)
            {
                return new MemoryConflict
                {
                    ConflictType = MemoryConflictType.MixedAccess,
                    First = a,
                    Second = b,
                    Severity = IssueSeverity.Error,
                    Message = $"비트/바이트 혼용: {a.VariableName}({a.DataType}, 비트)와 {b.VariableName}({b.DataType}, {b.Size}) 영역 겹침"
                };
            }
        }
        else if (b.Size == MemorySize.Bit && a.Size != MemorySize.Bit)
        {
            if (b.StartAddress >= a.StartAddress && b.StartAddress < a.EndAddress)
            {
                return new MemoryConflict
                {
                    ConflictType = MemoryConflictType.MixedAccess,
                    First = a,
                    Second = b,
                    Severity = IssueSeverity.Error,
                    Message = $"비트/바이트 혼용: {b.VariableName}({b.DataType}, 비트)와 {a.VariableName}({a.DataType}, {a.Size}) 영역 겹침"
                };
            }
        }

        // 3. 부분 중첩 검사 (모든 크기 타입)
        if (IsOverlapping(a, b))
        {
            return new MemoryConflict
            {
                ConflictType = MemoryConflictType.PartialOverlap,
                First = a,
                Second = b,
                Severity = IssueSeverity.Critical,
                Message = $"메모리 영역 중첩: {a.VariableName}({a.DataType}, {a.RawAddress})와 {b.VariableName}({b.DataType}, {b.RawAddress})"
            };
        }

        return null;
    }

    /// <summary>
    /// 영역 중첩 여부 확인
    /// </summary>
    private bool IsOverlapping(MemoryAllocation a, MemoryAllocation b)
    {
        // 비트 주소는 별도 처리
        if (a.Size == MemorySize.Bit && b.Size == MemorySize.Bit)
        {
            return a.StartAddress == b.StartAddress && a.BitOffset == b.BitOffset;
        }

        // 바이트 이상 크기의 경우
        return a.StartAddress < b.EndAddress && b.StartAddress < a.EndAddress;
    }

    /// <summary>
    /// 메모리 단편화 검사
    /// </summary>
    private List<MemoryConflict> CheckFragmentation(List<MemoryAllocation> allocations)
    {
        var conflicts = new List<MemoryConflict>();

        // 바이트 이상 크기만 검사
        var byteAllocations = allocations
            .Where(a => a.Size != MemorySize.Bit)
            .OrderBy(a => a.StartAddress)
            .ToList();

        for (int i = 0; i < byteAllocations.Count - 1; i++)
        {
            var current = byteAllocations[i];
            var next = byteAllocations[i + 1];

            var gap = next.StartAddress - current.EndAddress;

            // 10바이트 이상 갭이 있으면 단편화 경고
            if (gap > 10)
            {
                conflicts.Add(new MemoryConflict
                {
                    ConflictType = MemoryConflictType.FragmentedAllocation,
                    First = current,
                    Second = next,
                    Severity = IssueSeverity.Warning,
                    Message = $"메모리 단편화: {current.VariableName}와 {next.VariableName} 사이 {gap}바이트 갭"
                });
            }
        }

        return conflicts;
    }

    /// <summary>
    /// 메모리 사용 통계 계산
    /// </summary>
    private List<MemoryUsageStatistics> CalculateStatistics(List<MemoryAllocation> allocations)
    {
        var statistics = new List<MemoryUsageStatistics>();

        foreach (var regionType in Enum.GetValues<MemoryRegionType>())
        {
            if (regionType == MemoryRegionType.Unknown) continue;

            var regionAllocations = allocations.Where(a => a.RegionType == regionType).ToList();
            if (!regionAllocations.Any()) continue;

            var bitAllocations = regionAllocations.Where(a => a.Size == MemorySize.Bit).ToList();
            var byteAllocations = regionAllocations.Where(a => a.Size != MemorySize.Bit).ToList();

            var minAddr = regionAllocations.Min(a => a.StartAddress);
            var maxAddr = byteAllocations.Any() ? byteAllocations.Max(a => a.EndAddress) : minAddr + 1;

            var usedBytes = byteAllocations.Sum(a => a.EndAddress - a.StartAddress);
            var totalRange = maxAddr - minAddr;
            var fragmentationRate = totalRange > 0 ? (1.0 - (double)usedBytes / totalRange) * 100 : 0;

            // 갭 계산
            var gaps = CalculateGaps(byteAllocations, minAddr, maxAddr);

            statistics.Add(new MemoryUsageStatistics
            {
                RegionType = regionType,
                TotalAllocations = regionAllocations.Count,
                UsedBytes = usedBytes,
                UsedBits = bitAllocations.Count,
                MinAddress = minAddr,
                MaxAddress = maxAddr,
                FragmentationRate = Math.Round(fragmentationRate, 2),
                Gaps = gaps
            });
        }

        return statistics;
    }

    /// <summary>
    /// 메모리 갭 계산
    /// </summary>
    private List<(int Start, int End)> CalculateGaps(List<MemoryAllocation> allocations, int minAddr, int maxAddr)
    {
        var gaps = new List<(int Start, int End)>();
        var sorted = allocations.OrderBy(a => a.StartAddress).ToList();

        int currentEnd = minAddr;
        foreach (var alloc in sorted)
        {
            if (alloc.StartAddress > currentEnd)
            {
                gaps.Add((currentEnd, alloc.StartAddress));
            }
            currentEnd = Math.Max(currentEnd, alloc.EndAddress);
        }

        return gaps;
    }
}
