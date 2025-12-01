using System.Collections.Generic;

namespace TwinCatQA.Application.Models
{
    /// <summary>
    /// Chart.js 호환 차트 데이터 모델
    /// </summary>
    public class ChartData
    {
        /// <summary>
        /// 차트 타입 (line, bar, pie, radar 등)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 차트 데이터
        /// </summary>
        public ChartDataSet Data { get; set; } = new();

        /// <summary>
        /// 차트 옵션
        /// </summary>
        public Dictionary<string, object> Options { get; set; } = new();
    }

    /// <summary>
    /// Chart.js 데이터셋 구조
    /// </summary>
    public class ChartDataSet
    {
        /// <summary>
        /// X축 레이블
        /// </summary>
        public List<string> Labels { get; set; } = new();

        /// <summary>
        /// 데이터셋 목록
        /// </summary>
        public List<Dataset> Datasets { get; set; } = new();
    }

    /// <summary>
    /// 개별 데이터셋
    /// </summary>
    public class Dataset
    {
        /// <summary>
        /// 데이터셋 레이블
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// 데이터 값
        /// </summary>
        public List<double> Data { get; set; } = new();

        /// <summary>
        /// 배경색 (단일 또는 배열)
        /// </summary>
        public object? BackgroundColor { get; set; }

        /// <summary>
        /// 테두리 색
        /// </summary>
        public object? BorderColor { get; set; }

        /// <summary>
        /// 테두리 두께
        /// </summary>
        public int BorderWidth { get; set; } = 1;

        /// <summary>
        /// 선 긴장도 (Line 차트용)
        /// </summary>
        public double? Tension { get; set; }

        /// <summary>
        /// 영역 채우기 여부
        /// </summary>
        public bool? Fill { get; set; }
    }
}
