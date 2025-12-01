using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using TwinCatQA.Domain.Models;

namespace TwinCatQA.VsExtension.Services;

/// <summary>
/// 분석 결과 캐시
/// 파일 해시 기반으로 이전 분석 결과 재사용
/// </summary>
public class AnalysisResultCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly TimeSpan _expirationTime;
    private readonly int _maxCacheSize;
    private readonly Timer _cleanupTimer;
    private readonly object _lockObject = new();

    /// <summary>
    /// 캐시 생성자
    /// </summary>
    /// <param name="expirationMinutes">캐시 만료 시간 (분, 기본 30분)</param>
    /// <param name="maxCacheSize">최대 캐시 엔트리 수 (기본 1000)</param>
    public AnalysisResultCache(
        int expirationMinutes = 30,
        int maxCacheSize = 1000)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _expirationTime = TimeSpan.FromMinutes(expirationMinutes);
        _maxCacheSize = maxCacheSize;

        // 정리 타이머 (5분마다 만료된 항목 제거)
        _cleanupTimer = new Timer(
            CleanupExpiredEntries,
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    #region 캐시 작업

    /// <summary>
    /// 캐시에서 결과 가져오기
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>캐시된 위반 사항 목록 (없거나 만료되면 null)</returns>
    public List<Violation>? Get(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (!File.Exists(filePath))
            return null;

        var key = GetCacheKey(filePath);

        if (!_cache.TryGetValue(key, out var entry))
            return null;

        // 만료 확인
        if (IsExpired(entry))
        {
            _cache.TryRemove(key, out _);
            return null;
        }

        // 파일 변경 확인 (해시 비교)
        var currentHash = ComputeFileHash(filePath);
        if (entry.FileHash != currentHash)
        {
            _cache.TryRemove(key, out _);
            return null;
        }

        // 캐시 히트
        entry.LastAccessed = DateTime.UtcNow;
        entry.HitCount++;

        return entry.Violations.ToList(); // 방어적 복사
    }

    /// <summary>
    /// 캐시에 결과 저장
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="violations">위반 사항 목록</param>
    public void Set(string filePath, List<Violation> violations)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (!File.Exists(filePath))
            return;

        // 캐시 크기 제한 확인
        if (_cache.Count >= _maxCacheSize)
        {
            EvictLeastRecentlyUsed();
        }

        var key = GetCacheKey(filePath);
        var fileHash = ComputeFileHash(filePath);

        var entry = new CacheEntry
        {
            FilePath = filePath,
            FileHash = fileHash,
            Violations = violations.ToList(), // 방어적 복사
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            HitCount = 0
        };

        _cache.AddOrUpdate(key, entry, (_, __) => entry);
    }

    /// <summary>
    /// 특정 파일의 캐시 제거
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    public void Remove(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var key = GetCacheKey(filePath);
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// 전체 캐시 삭제
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    #endregion

    #region 캐시 통계

    /// <summary>
    /// 캐시 통계 가져오기
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            TotalHits = entries.Sum(e => e.HitCount),
            AverageHits = entries.Any() ? entries.Average(e => e.HitCount) : 0,
            OldestEntry = entries.Any()
                ? entries.Min(e => e.CreatedAt)
                : (DateTime?)null,
            NewestEntry = entries.Any()
                ? entries.Max(e => e.CreatedAt)
                : (DateTime?)null,
            TotalViolationsCached = entries.Sum(e => e.Violations.Count)
        };
    }

    #endregion

    #region 내부 구현

    /// <summary>
    /// 파일 경로에서 캐시 키 생성
    /// </summary>
    private string GetCacheKey(string filePath)
    {
        // 대소문자 구분 없이, 경로 정규화
        return Path.GetFullPath(filePath).ToLowerInvariant();
    }

    /// <summary>
    /// 파일 내용의 SHA256 해시 계산
    /// </summary>
    private string ComputeFileHash(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hashBytes);
        }
        catch
        {
            // 파일 접근 실패 시 빈 해시 반환
            return string.Empty;
        }
    }

    /// <summary>
    /// 캐시 엔트리 만료 확인
    /// </summary>
    private bool IsExpired(CacheEntry entry)
    {
        return DateTime.UtcNow - entry.CreatedAt > _expirationTime;
    }

    /// <summary>
    /// LRU (Least Recently Used) 정책으로 캐시 축출
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        lock (_lockObject)
        {
            // 가장 오래 접근되지 않은 항목 제거 (최대 10% 제거)
            var entriesToRemove = _cache.Values
                .OrderBy(e => e.LastAccessed)
                .Take(_maxCacheSize / 10)
                .ToList();

            foreach (var entry in entriesToRemove)
            {
                var key = GetCacheKey(entry.FilePath);
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// 만료된 캐시 엔트리 정리 (타이머 콜백)
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var expiredKeys = _cache
            .Where(kvp => IsExpired(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache.Clear();
    }

    #endregion
}

#region 내부 클래스

/// <summary>
/// 캐시 엔트리
/// </summary>
internal class CacheEntry
{
    /// <summary>파일 경로</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>파일 해시 (SHA256)</summary>
    public string FileHash { get; init; } = string.Empty;

    /// <summary>캐시된 위반 사항</summary>
    public List<Violation> Violations { get; init; } = new();

    /// <summary>캐시 생성 시각</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>마지막 접근 시각</summary>
    public DateTime LastAccessed { get; set; }

    /// <summary>캐시 히트 횟수</summary>
    public long HitCount { get; set; }
}

#endregion

/// <summary>
/// 캐시 통계 정보
/// </summary>
public class CacheStatistics
{
    /// <summary>총 캐시 엔트리 수</summary>
    public int TotalEntries { get; init; }

    /// <summary>총 캐시 히트 횟수</summary>
    public long TotalHits { get; init; }

    /// <summary>평균 히트 횟수</summary>
    public double AverageHits { get; init; }

    /// <summary>가장 오래된 엔트리 생성 시각</summary>
    public DateTime? OldestEntry { get; init; }

    /// <summary>가장 최근 엔트리 생성 시각</summary>
    public DateTime? NewestEntry { get; init; }

    /// <summary>캐시된 총 위반 사항 수</summary>
    public int TotalViolationsCached { get; init; }

    /// <summary>
    /// 통계 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"캐시 엔트리: {TotalEntries}, " +
               $"총 히트: {TotalHits}, " +
               $"평균 히트: {AverageHits:F2}, " +
               $"캐시된 위반: {TotalViolationsCached}";
    }
}
