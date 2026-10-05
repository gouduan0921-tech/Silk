using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using HuaShang.Game;
using HuaShang.Play;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 工位链的导航：全景 → 走近（一个进入提示）→ 进入（同一场景推近）。
    /// Esc 或取消退回一个画面层级；离开工位即写盘（docs/19 §3）。
    /// </summary>
    public class WorkshopController : MonoBehaviour
    {
        public enum Layer { Overview, Approach, Inside }

        public CameraDirector cameraDirector;
        public Transform overviewPose;
        public List<StationBase> stations = new List<StationBase>();
        public Layer layer = Layer.Overview;
        public StationBase current;
        public bool performing;

        GameSession G => GameSession.I;

        void Start()
        {
            foreach (var s in stations) s.Workshop = this;
            G.Changed += RefreshChrome;
            Settings.Changed += ApplyQuality;
            ApplyQuality();
            GoOverview();
        }

        void OnDestroy()
        {
            if (G != null) G.Changed -= RefreshChrome;
            Settings.Changed -= ApplyQuality;
        }

        void ApplyQuality()
        {
            if (ClothQualityDirector.Instance != null)
                ClothQualityDirector.Instance.globalQuality = (ClothQuality)Settings.Current.clothQuality;
        }

        void Update()
        {
            if (performing) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) Back();
                if (kb.spaceKey.wasPressedThisFrame && layer == Layer.Inside && current != null) current.BeatKey();
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && layer != Layer.Inside
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                var ray = cameraDirector.cam.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 200f))
                {
                    var st = hit.collider.GetComponentInParent<StationBase>();
                    if (st != null) Approach(st);
                }
            }
        }

        public void GoOverview()
        {
            LeaveInside();
            layer = Layer.Overview;
            current = null;
            cameraDirector.GoTo(overviewPose, 45f);
            Hud.I.SetHeader("全景", "工位链", "织、染、裁、缝、人台，一路到戏台与展柜。");
            RefreshChrome();
        }

        void ShowOverviewPlaque()
        {
            var m = new PlaqueModel { kicker = "今天", title = TodoTitle(), body = TodoBody(), onCancel = null };
            m.secondary.Add(new KeyValuePair<string, System.Action>("走近下一站", () => Approach(NextStation())));
            Hud.I.ShowPlaque(m);
        }

        public void Approach(StationBase s, bool enterNow = false)
        {
            if (s == null) return;
            LeaveInside();
            current = s;
            layer = Layer.Approach;
            cameraDirector.GoTo(s.approachPose, 42f);
            Hud.I.SetHeader(s.number + " / " + s.englishKicker, s.title, s.subtitle);
            s.OnApproach();
            var m = new PlaqueModel
            {
                kicker = "走近",
                title = s.title,
                body = s.subtitle,
                primaryLabel = "进入" + s.title,
                onPrimary = () => Enter(s),
                onCancel = () => Back(),
            };
            Hud.I.ShowPlaque(m);
            RefreshChrome();
            if (enterNow) Enter(s);
        }

        public void Enter(StationBase s)
        {
            current = s;
            layer = Layer.Inside;
            cameraDirector.GoTo(s.enterPose, s.enterFov);
            Hud.I.SetHeader(s.number + " / " + s.englishKicker, s.title, s.subtitle);
            s.OnEnter();
            RefreshChrome();
        }

        /// <summary>退一个画面层级。工位内部先退自己的层级。</summary>
        public void Back()
        {
            if (layer == Layer.Inside && current != null)
            {
                if (current.Back()) return;
                var s = current;
                Approach(s);
                return;
            }
            if (layer == Layer.Approach) { GoOverview(); return; }
        }

        void LeaveInside()
        {
            if (layer == Layer.Inside && current != null)
            {
                current.OnExit();
                G.WriteNow(); // 离开工位即写盘
            }
        }

        StationBase NextStation()
        {
            foreach (var s in stations) if (!s.Done) return s;
            return stations.Count > 0 ? stations[0] : null;
        }

        string TodoTitle()
        {
            var t1 = Play.Find.Quest(G.Save, Craft.Tutorial1);
            var t2 = Play.Find.Quest(G.Save, Craft.Tutorial2);
            var t3 = Play.Find.Quest(G.Save, Craft.Tutorial3);
            if (t1 != null && !t1.done) return "确认一匹绢";
            if (t2 != null && !t2.done) return "染靛蓝，做成襦裙";
            if (t3 != null && !t3.done) return "西施穿上，演完 0 档，放入展柜";
            return "自由制作";
        }

        string TodoBody()
        {
            int left = Progress.HoursLeft(G.Save, G.Config);
            return "今日工时还剩 " + left + "。走近工位只有一个提示，进入后在原场景操作。";
        }

        public void RefreshChrome()
        {
            var s = G.Save;
            int? sday = Seasons.DayOfSeason(s, G.Config);
            Hud.I.SetTopInfo(Names.Season(s.season) + (sday.HasValue ? "季第 " + sday.Value + " 日" : "") + " · " + Names.Day(s.dayIndex)
                             + "      丝钱 " + s.silkCoin + "      等级 " + s.level
                             + "      工时 " + Progress.HoursLeft(s, G.Config) + "/" + G.Config.balance.day.HoursLimit(s.dayIndex)
                             + "      " + TodoTitle());
            var stops = new List<CorridorStop>
            {
                new CorridorStop { id = "overview", number = "全景", label = "工位链", current = layer == Layer.Overview, onClick = GoOverview },
            };
            foreach (var st in stations)
            {
                var x = st;
                stops.Add(new CorridorStop { id = st.stationId, number = st.number, label = st.title, current = current == st && layer != Layer.Overview, done = st.Done, onClick = () => Approach(x) });
            }
            Hud.I.SetCorridor(stops);
            if (layer == Layer.Overview) ShowOverviewPlaque(); // 新开一局或读档后，今日待办跟着存档变
            if (layer == Layer.Inside && current != null) current.Refresh();
            else Hud.I.SetRack(RackView.Build(s, G.Config, null, null, null));
        }
    }
}
