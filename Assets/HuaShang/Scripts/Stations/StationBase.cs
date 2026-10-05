using System.Collections.Generic;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Play;
using HuaShang.Rules.Config;
using HuaShang.Save;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 一个工位：走近只有一个进入提示，进入后同一场景推近（docs/12 §1）。
    /// Esc 或取消退回一个画面层级，不抹掉已经确认的产物；未确认的选择只存在这里，不进存档。
    /// </summary>
    public abstract class StationBase : MonoBehaviour
    {
        public string stationId;
        public string number;
        public string title;
        public string englishKicker = "SILK WORKSHOP";
        public string subtitle;
        public Transform approachPose, enterPose;
        public float enterFov = 38f;

        protected GameSession G => GameSession.I;
        protected SaveRoot S => G.Save;
        protected ConfigSnapshot C => G.Config;
        protected Hud H => Hud.I;
        public WorkshopController Workshop { get; set; }

        /// <summary>工位内部的画面层级；0 是刚进入。</summary>
        protected int depth;

        public virtual void OnApproach() { }

        public virtual void OnEnter() { depth = 0; Refresh(); }

        public virtual void OnExit() { }

        /// <summary>退一层；返回 false 表示已在最外层，由工作坊退回走近。</summary>
        public virtual bool Back()
        {
            if (depth > 0) { depth--; Refresh(); return true; }
            return false;
        }

        /// <summary>空格：节拍类工位用。</summary>
        public virtual void BeatKey() { }

        /// <summary>重画木牌与侧架。</summary>
        public abstract void Refresh();

        public virtual bool Done => false;

        protected PlaqueModel Plaque(string title, string kicker = "下一步")
        {
            return new PlaqueModel { title = title, kicker = kicker, onCancel = () => Workshop.Back() };
        }

        protected void Show(PlaqueModel m) => H.ShowPlaque(m);

        protected bool Run(System.Func<SaveRoot, ConfigSnapshot, Result> cmd, string okToast = null)
        {
            var r = G.Run(cmd);
            if (!r.ok) H.Toast(r.error);
            else
            {
                if (r.notes.Count > 0) H.Toast(string.Join("；", r.notes));
                else if (!string.IsNullOrEmpty(okToast)) H.Toast(okToast);
                Sfx.Play(Sfx.Cue.Confirm);
            }
            return r.ok;
        }

        protected virtual void Update() { }
    }
}
