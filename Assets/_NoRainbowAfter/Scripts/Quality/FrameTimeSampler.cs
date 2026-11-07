using System;
using UnityEngine;

namespace NoRainbowAfter.Quality
{
    [Serializable]
    public struct FrameTimeSummary
    {
        public int sampleCount;
        public float durationSeconds;
        public float averageFrameTimeMs;
        public float averageFps;
        public float medianFrameTimeMs;
        public float medianFps;
        public float p95FrameTimeMs;
        public float p99FrameTimeMs;
        public float onePercentLowFps;
        public float minFrameTimeMs;
        public float maxFrameTimeMs;
    }

    /// <summary>
    /// Lightweight pre-allocated frame-time recorder for T07 standalone and Editor FPS benchmarking.
    /// Avoids per-frame GC allocations during sampling.
    /// </summary>
    public sealed class FrameTimeSampler
    {
        private readonly float[] samplesMs;
        private readonly float[] scratchSort;
        private int count;
        private double totalSeconds;

        public FrameTimeSampler(int capacity = 16384)
        {
            int cap = Mathf.Max(64, capacity);
            samplesMs = new float[cap];
            scratchSort = new float[cap];
        }

        public int Count => count;

        public void Reset()
        {
            count = 0;
            totalSeconds = 0.0;
        }

        public void RecordFrame(float unscaledDeltaTimeSeconds)
        {
            if (unscaledDeltaTimeSeconds <= 0.00001f || unscaledDeltaTimeSeconds > 2.0f)
                return;

            totalSeconds += unscaledDeltaTimeSeconds;
            if (count < samplesMs.Length)
            {
                samplesMs[count++] = unscaledDeltaTimeSeconds * 1000f;
            }
        }

        public FrameTimeSummary Summarize()
        {
            if (count == 0)
                return default;

            Array.Copy(samplesMs, 0, scratchSort, 0, count);
            Array.Sort(scratchSort, 0, count);

            double sumMs = 0.0;
            for (int i = 0; i < count; i++)
                sumMs += scratchSort[i];

            float avgMs = (float)(sumMs / count);
            float medianMs = PercentileSorted(scratchSort, count, 0.50f);
            float p95Ms = PercentileSorted(scratchSort, count, 0.95f);
            float p99Ms = PercentileSorted(scratchSort, count, 0.99f);

            int worstCount = Mathf.Max(1, Mathf.CeilToInt(count * 0.01f));
            double worstSumMs = 0.0;
            for (int i = count - worstCount; i < count; i++)
                worstSumMs += scratchSort[i];
            float worstMeanMs = (float)(worstSumMs / worstCount);

            return new FrameTimeSummary
            {
                sampleCount = count,
                durationSeconds = (float)totalSeconds,
                averageFrameTimeMs = Round2(avgMs),
                averageFps = avgMs > 0.0001f ? Round2(1000f / avgMs) : 0f,
                medianFrameTimeMs = Round2(medianMs),
                medianFps = medianMs > 0.0001f ? Round2(1000f / medianMs) : 0f,
                p95FrameTimeMs = Round2(p95Ms),
                p99FrameTimeMs = Round2(p99Ms),
                onePercentLowFps = worstMeanMs > 0.0001f ? Round2(1000f / worstMeanMs) : 0f,
                minFrameTimeMs = Round2(scratchSort[0]),
                maxFrameTimeMs = Round2(scratchSort[count - 1])
            };
        }

        private static float PercentileSorted(float[] sorted, int length, float fraction)
        {
            if (length <= 0) return 0f;
            if (length == 1) return sorted[0];
            float pos = Mathf.Clamp01(fraction) * (length - 1);
            int lower = Mathf.FloorToInt(pos);
            int upper = Mathf.CeilToInt(pos);
            if (lower == upper) return sorted[lower];
            return Mathf.Lerp(sorted[lower], sorted[upper], pos - lower);
        }

        private static float Round2(float value) => Mathf.Round(value * 100f) * 0.01f;
    }
}
