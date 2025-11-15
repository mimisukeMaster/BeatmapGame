using UnityEngine;

public static class BeatmapUtility
{
    /// <summary>
    /// 1ステップ（グリッドの最小単位）あたりの秒数を計算
    /// </summary>
    /// <param name="bpm">BPM</param>
    /// <param name="beatsPerMeasure">拍子 (例: 4/4なら 4)</param>
    /// <param name="stepsPerMeasure">1小節あたりのステップ数 (例: 16分音符なら 16)</param>
    /// <returns>1ステップの秒数</returns>
    public static double GetSecondsPerStep(double bpm, int beatsPerMeasure, int stepsPerMeasure)
    {
        if (bpm <= 0 || beatsPerMeasure <= 0 || stepsPerMeasure <= 0)
        {
            Debug.LogError("BPMや拍子の設定が不正です。");
            return 0;
        }
        double secondsPerBeat = 60.0 / bpm;
        double secondsPerMeasure = secondsPerBeat * beatsPerMeasure;
        return secondsPerMeasure / stepsPerMeasure;
    }

    /// <summary>
    /// ステップ番号から、曲の開始時点からの正確な秒数を計算
    /// </summary>
    /// <param name="beatmap">譜面データ</param>
    /// <param name="step">ステップ番号</param>
    /// <returns>秒数</returns>
    public static double GetTimeFromStep(Beatmap beatmap, int step)
    {
        double secondsPerStep = GetSecondsPerStep(beatmap.bpm, beatmap.beatsPerMeasure, beatmap.stepsPerMeasure);
        return beatmap.firstBeatOffsetSec + (secondsPerStep * step);
    }

    /// <summary>
    /// 曲の秒数から、最も近いステップ番号を計算（グリッドにスナップする）
    /// </summary>
    /// <param name="beatmap">譜面データ</param>
    /// <param name="timeSec">秒数</param>
    /// <returns>ステップ番号</returns>
    public static int GetStepFromTime(Beatmap beatmap, double timeSec)
    {
        if (timeSec < beatmap.firstBeatOffsetSec) return 0;
        
        double secondsPerStep = GetSecondsPerStep(beatmap.bpm, beatmap.beatsPerMeasure, beatmap.stepsPerMeasure);
        if (secondsPerStep <= 0) return 0;

        double relativeTime = timeSec - beatmap.firstBeatOffsetSec;
        return (int)Mathf.Round((float)(relativeTime / secondsPerStep));
    }
}