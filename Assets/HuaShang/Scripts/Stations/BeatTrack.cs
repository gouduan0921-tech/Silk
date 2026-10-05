using System;
using System.Collections.Generic;
using UnityEngine;
using HuaShang.Play;
using HuaShang.Rules.Config;

namespace HuaShang.Stations
{
    /// <summary>
    /// 节拍操作：每隔 docs/04 §5 的节拍间隔有一个目标点，玩家按空格；按偏差毫秒判稳、偏、乱。
    /// 错过一拍超过偏的窗口记乱。判定发生在节拍上，不在结算按钮（docs/12 §3）。
    /// </summary>
    public class BeatTrack
    {
        readonly WeaveData w;
        readonly int total;
        readonly float interval;
        float startTime;
        int nextTarget;
        public readonly List<Beat> results = new List<Beat>();
        public Action<Beat> onBeat;
        public string caption;

        public BeatTrack(WeaveData weave, int totalBeats, string caption)
        {
            w = weave;
            total = totalBeats;
            interval = (float)weave.beatInterval;
            this.caption = caption;
            startTime = Time.time + interval; // 第一拍留一拍准备
        }

        /// <summary>不定拍数：持续到外部结束（染缸搅拌持续到起布，docs/04 §5）。</summary>
        public const int OpenEnded = int.MaxValue;

        public bool Done => results.Count >= total;
        public bool IsOpenEnded => total == OpenEnded;
        /// <summary>不定拍数时为已经过的拍数。</summary>
        public int Total => IsOpenEnded ? results.Count : total;

        float TargetTime(int i) => startTime + i * interval;

        /// <summary>每帧调用：把错过太久的拍记为乱。</summary>
        public void Tick()
        {
            if (Done) return;
            float late = (float)w.offWindowMs / 1000f;
            while (!Done && Time.time > TargetTime(nextTarget) + late)
            {
                Record(Beat.Chaos);
            }
        }

        public void Press()
        {
            if (Done) return;
            double offsetMs = (Time.time - TargetTime(nextTarget)) * 1000.0;
            // 太早按（不到上一拍与本拍的中点）不算这一拍
            if (offsetMs < -interval * 500.0) return;
            Record(Beats.Judge(offsetMs, w));
        }

        void Record(Beat b)
        {
            results.Add(b);
            nextTarget++;
            onBeat?.Invoke(b);
        }

        public UI.BeatView View()
        {
            float phase = Done ? 1f : Mathf.Clamp01(1f - (TargetTime(nextTarget) - Time.time) / interval);
            return new UI.BeatView { cursor = phase, target = 1f, caption = caption, done = results.Count, total = IsOpenEnded ? 0 : total };
        }
    }
}
