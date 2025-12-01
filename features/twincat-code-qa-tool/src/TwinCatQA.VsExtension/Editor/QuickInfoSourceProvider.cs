using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// QuickInfo 소스 프로바이더 - 마우스 호버 시 위반 사항 정보를 표시하는 소스를 생성
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("TwinCAT QA QuickInfo Provider")]
    [ContentType("text")] // 모든 텍스트 타입에 적용
    [Order(Before = "Default Quick Info Presenter")]
    public class QuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        /// <summary>
        /// 버퍼별 QuickInfo 소스 캐시 키
        /// </summary>
        private static readonly object _quickInfoSourceKey = new object();

        /// <summary>
        /// 지정된 버퍼에 대한 QuickInfo 소스 생성
        /// </summary>
        /// <param name="textBuffer">텍스트 버퍼</param>
        /// <returns>QuickInfo 소스 인스턴스</returns>
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // 버퍼별로 하나의 QuickInfo 소스만 생성 (캐싱)
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                _quickInfoSourceKey,
                () => new QuickInfoSource(textBuffer));
        }

        /// <summary>
        /// 특정 버퍼의 QuickInfo 소스 가져오기
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        /// <returns>QuickInfoSource 인스턴스 또는 null</returns>
        public static QuickInfoSource GetQuickInfoSource(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            buffer.Properties.TryGetProperty(_quickInfoSourceKey, out QuickInfoSource source);
            return source;
        }

        /// <summary>
        /// 특정 버퍼에 QuickInfo 소스가 존재하는지 확인
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        /// <returns>QuickInfo 소스 존재 여부</returns>
        public static bool HasQuickInfoSource(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return false;
            }

            return buffer.Properties.ContainsProperty(_quickInfoSourceKey);
        }

        /// <summary>
        /// 특정 버퍼의 QuickInfo 소스 제거
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        public static void RemoveQuickInfoSource(ITextBuffer buffer)
        {
            if (buffer != null && buffer.Properties.ContainsProperty(_quickInfoSourceKey))
            {
                // 소스 제거 전 Dispose 호출
                if (buffer.Properties.TryGetProperty(_quickInfoSourceKey, out QuickInfoSource source))
                {
                    source?.Dispose();
                }

                buffer.Properties.RemoveProperty(_quickInfoSourceKey);
            }
        }
    }
}
