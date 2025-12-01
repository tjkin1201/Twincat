using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using TwinCatQA.Core.Models;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// Visual Studio Error List에 코드 품질 위반 사항을 표시하는 프로바이더
    /// </summary>
    public class ErrorListProvider : ITableDataSource, IDisposable
    {
        private readonly ITableManager _tableManager;
        private readonly List<SinkManager> _managers = new List<SinkManager>();
        private readonly Dictionary<string, ErrorListItem> _items = new Dictionary<string, ErrorListItem>();
        private readonly object _lock = new object();

        /// <summary>
        /// 데이터 소스 식별자
        /// </summary>
        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        /// <summary>
        /// 데이터 소스 이름
        /// </summary>
        public string Identifier => "TwinCatQA.ErrorListProvider";

        /// <summary>
        /// 데이터 소스 표시 이름
        /// </summary>
        public string DisplayName => "TwinCAT Code QA";

        /// <summary>
        /// ErrorListProvider 생성자
        /// </summary>
        /// <param name="serviceProvider">서비스 프로바이더</param>
        public ErrorListProvider(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _tableManager = serviceProvider.GetService(typeof(SVsErrorList)) as ITableManager;
            if (_tableManager != null)
            {
                _tableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                    StandardTableColumnDefinitions.ErrorSeverity,
                    StandardTableColumnDefinitions.ErrorCode,
                    StandardTableColumnDefinitions.ErrorSource,
                    StandardTableColumnDefinitions.BuildTool,
                    StandardTableColumnDefinitions.ErrorCategory,
                    StandardTableColumnDefinitions.Text,
                    StandardTableColumnDefinitions.DocumentName,
                    StandardTableColumnDefinitions.Line,
                    StandardTableColumnDefinitions.Column);
            }
        }

        /// <summary>
        /// 위반 사항 추가
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <param name="violations">위반 사항 목록</param>
        public void AddViolations(string filePath, IEnumerable<Violation> violations)
        {
            lock (_lock)
            {
                foreach (var violation in violations)
                {
                    var key = $"{filePath}:{violation.LineNumber}:{violation.Column}:{violation.RuleId}";

                    if (_items.ContainsKey(key))
                    {
                        continue;
                    }

                    var item = new ErrorListItem
                    {
                        FilePath = filePath,
                        Violation = violation
                    };

                    _items[key] = item;
                }

                // 모든 sink에 업데이트 알림
                UpdateAllSinks();
            }
        }

        /// <summary>
        /// 특정 파일의 위반 사항 제거
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        public void ClearViolations(string filePath)
        {
            lock (_lock)
            {
                var keysToRemove = _items.Keys
                    .Where(k => k.StartsWith(filePath + ":"))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _items.Remove(key);
                }

                // 모든 sink에 업데이트 알림
                UpdateAllSinks();
            }
        }

        /// <summary>
        /// 모든 위반 사항 제거
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _items.Clear();
                UpdateAllSinks();
            }
        }

        /// <summary>
        /// 모든 sink 업데이트
        /// </summary>
        private void UpdateAllSinks()
        {
            foreach (var manager in _managers)
            {
                manager.NotifyChange();
            }
        }

        #region ITableDataSource 구현

        /// <summary>
        /// 테이블 데이터 sink 구독
        /// </summary>
        public IDisposable Subscribe(ITableDataSink sink)
        {
            lock (_lock)
            {
                var manager = new SinkManager(this, sink);
                _managers.Add(manager);

                // 초기 스냅샷 전달
                manager.NotifyChange();

                return manager;
            }
        }

        #endregion

        #region IDisposable 구현

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    if (_tableManager != null)
                    {
                        _tableManager.RemoveSource(this);
                    }

                    lock (_lock)
                    {
                        _managers.Clear();
                        _items.Clear();
                    }
                }

                _disposed = true;
            }
        }

        #endregion

        #region SinkManager 클래스

        /// <summary>
        /// Sink 관리자 - 테이블 데이터를 관리하고 변경 사항을 알림
        /// </summary>
        private class SinkManager : IDisposable
        {
            private readonly ErrorListProvider _provider;
            private readonly ITableDataSink _sink;

            public SinkManager(ErrorListProvider provider, ITableDataSink sink)
            {
                _provider = provider;
                _sink = sink;
            }

            /// <summary>
            /// 변경 사항 알림
            /// </summary>
            public void NotifyChange()
            {
                _sink.FactorySnapshotChanged(null);
            }

            public void Dispose()
            {
                _provider._managers.Remove(this);
            }
        }

        #endregion

        #region ErrorListItem 클래스

        /// <summary>
        /// Error List 항목 데이터
        /// </summary>
        private class ErrorListItem : ITableEntry
        {
            public string FilePath { get; set; }
            public Violation Violation { get; set; }

            /// <summary>
            /// 열 값 가져오기
            /// </summary>
            public bool TryGetValue(string keyName, out object content)
            {
                content = null;

                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        content = FilePath;
                        return true;

                    case StandardTableKeyNames.Line:
                        content = Violation.LineNumber - 1; // 0-based
                        return true;

                    case StandardTableKeyNames.Column:
                        content = Violation.Column - 1; // 0-based
                        return true;

                    case StandardTableKeyNames.Text:
                        content = Violation.Message;
                        return true;

                    case StandardTableKeyNames.ErrorSeverity:
                        content = GetSeverity(Violation.Severity);
                        return true;

                    case StandardTableKeyNames.ErrorCode:
                        content = Violation.RuleId;
                        return true;

                    case StandardTableKeyNames.ErrorSource:
                        content = ErrorSource.Build;
                        return true;

                    case StandardTableKeyNames.BuildTool:
                        content = "TwinCAT QA";
                        return true;

                    case StandardTableKeyNames.ErrorCategory:
                        content = Violation.Category;
                        return true;

                    case StandardTableKeyNames.Priority:
                        content = GetPriority(Violation.Severity);
                        return true;

                    case StandardTableKeyNames.HelpLink:
                        if (!string.IsNullOrEmpty(Violation.HelpUrl))
                        {
                            content = Violation.HelpUrl;
                            return true;
                        }
                        return false;

                    default:
                        return false;
                }
            }

            /// <summary>
            /// 심각도를 Visual Studio 심각도로 변환
            /// </summary>
            private __VSERRORCATEGORY GetSeverity(ViolationSeverity severity)
            {
                switch (severity)
                {
                    case ViolationSeverity.Error:
                        return __VSERRORCATEGORY.EC_ERROR;
                    case ViolationSeverity.Warning:
                        return __VSERRORCATEGORY.EC_WARNING;
                    case ViolationSeverity.Information:
                        return __VSERRORCATEGORY.EC_MESSAGE;
                    default:
                        return __VSERRORCATEGORY.EC_MESSAGE;
                }
            }

            /// <summary>
            /// 심각도를 우선순위로 변환
            /// </summary>
            private vsTaskPriority GetPriority(ViolationSeverity severity)
            {
                switch (severity)
                {
                    case ViolationSeverity.Error:
                        return vsTaskPriority.vsTaskPriorityHigh;
                    case ViolationSeverity.Warning:
                        return vsTaskPriority.vsTaskPriorityMedium;
                    case ViolationSeverity.Information:
                        return vsTaskPriority.vsTaskPriorityLow;
                    default:
                        return vsTaskPriority.vsTaskPriorityLow;
                }
            }

            /// <summary>
            /// 항목 ID (고유 식별자)
            /// </summary>
            public bool Identity(out object identity)
            {
                identity = $"{FilePath}:{Violation.LineNumber}:{Violation.Column}:{Violation.RuleId}";
                return true;
            }

            /// <summary>
            /// 다른 항목과 동일한지 확인
            /// </summary>
            public bool CanSetValue(string keyName)
            {
                return false; // 읽기 전용
            }

            /// <summary>
            /// 값 설정 (지원하지 않음)
            /// </summary>
            public bool TrySetValue(string keyName, object content)
            {
                return false;
            }
        }

        #endregion

        #region TableEntriesSnapshot 클래스

        /// <summary>
        /// Error List 항목의 스냅샷
        /// </summary>
        private class TableEntriesSnapshot : ITableEntriesSnapshot
        {
            private readonly List<ErrorListItem> _items;
            private readonly int _versionNumber;

            public int Count => _items.Count;
            public int VersionNumber => _versionNumber;

            public TableEntriesSnapshot(IEnumerable<ErrorListItem> items, int versionNumber)
            {
                _items = items.ToList();
                _versionNumber = versionNumber;
            }

            public bool TryGetValue(int index, string keyName, out object content)
            {
                if (index >= 0 && index < _items.Count)
                {
                    return _items[index].TryGetValue(keyName, out content);
                }

                content = null;
                return false;
            }

            public int IndexOf(int currentIndex, ITableEntriesSnapshot newSnapshot)
            {
                // 항목 매칭 로직 (단순화)
                if (currentIndex >= 0 && currentIndex < _items.Count &&
                    newSnapshot is TableEntriesSnapshot other &&
                    currentIndex < other._items.Count)
                {
                    if (_items[currentIndex].Identity(out object id1) &&
                        other._items[currentIndex].Identity(out object id2) &&
                        id1.Equals(id2))
                    {
                        return currentIndex;
                    }
                }

                return -1;
            }

            public void Dispose()
            {
                // 정리 작업 없음
            }
        }

        #endregion
    }
}
