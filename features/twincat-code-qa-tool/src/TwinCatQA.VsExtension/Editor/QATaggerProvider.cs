using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace TwinCatQA.VsExtension.Editor
{
    /// <summary>
    /// QA 태거 프로바이더 - 에디터에 Squiggles를 표시하기 위한 태거를 생성
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")] // 모든 텍스트 타입에 적용
    [TagType(typeof(IErrorTag))]
    public class QATaggerProvider : ITaggerProvider
    {
        /// <summary>
        /// 버퍼별 태거 캐시
        /// </summary>
        private static readonly object _taggerKey = new object();

        /// <summary>
        /// 지정된 버퍼에 대한 태거 생성
        /// </summary>
        /// <typeparam name="T">태그 타입</typeparam>
        /// <param name="buffer">텍스트 버퍼</param>
        /// <returns>태거 인스턴스</returns>
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // IErrorTag 타입만 처리
            if (typeof(T) != typeof(IErrorTag))
            {
                return null;
            }

            // 버퍼별로 하나의 태거만 생성 (캐싱)
            return buffer.Properties.GetOrCreateSingletonProperty(_taggerKey, () => new QATagger(buffer)) as ITagger<T>;
        }

        /// <summary>
        /// 특정 버퍼의 태거 가져오기
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        /// <returns>QATagger 인스턴스 또는 null</returns>
        public static QATagger GetTagger(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return null;
            }

            buffer.Properties.TryGetProperty(_taggerKey, out QATagger tagger);
            return tagger;
        }

        /// <summary>
        /// 특정 버퍼에 태거가 존재하는지 확인
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        /// <returns>태거 존재 여부</returns>
        public static bool HasTagger(ITextBuffer buffer)
        {
            if (buffer == null)
            {
                return false;
            }

            return buffer.Properties.ContainsProperty(_taggerKey);
        }

        /// <summary>
        /// 특정 버퍼의 태거 제거
        /// </summary>
        /// <param name="buffer">텍스트 버퍼</param>
        public static void RemoveTagger(ITextBuffer buffer)
        {
            if (buffer != null && buffer.Properties.ContainsProperty(_taggerKey))
            {
                buffer.Properties.RemoveProperty(_taggerKey);
            }
        }
    }
}
