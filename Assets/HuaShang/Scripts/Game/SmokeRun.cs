using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using HuaShang.Performance;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.Stations;
using HuaShang.UI;

namespace HuaShang.Game
{
    /// <summary>
    /// 打包冒烟：命令行带 <c>-hsSmoke 目录</c> 启动时，用单独的存档走一遍首日（织、染、裁、缝、人台、戏台 0 档、展柜），
    /// 每站截图，记演出期间帧时间，写报告后退出。不带参数时什么都不做，也不碰玩家存档。
    /// </summary>
    public class SmokeRun : MonoBehaviour
    {
        const string Flag = "-hsSmoke";
        string outDir;
        readonly StringBuilder report = new StringBuilder();
        int errors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            string dir = Arg(Flag);
            if (dir == null) return;
            GameSession.SaveFileOverride = "huashang_smoke.json";
            var go = new GameObject("SmokeRun");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeRun>().outDir = dir;
        }

        static string Arg(string name)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }

        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors++;
                report.AppendLine("[" + type + "] " + msg);
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(outDir);
            report.AppendLine("华裳 打包冒烟 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("分辨率 " + Screen.width + "×" + Screen.height + "，图形 " + SystemInfo.graphicsDeviceName + "，布料质量 " + (ClothQuality)Settings.Current.clothQuality);
            yield return Wait(2f);
            var g = GameSession.I;
            g.StartNewGame();
            var w = FindFirstObjectByType<WorkshopController>();
            yield return Wait(1f);
            yield return Shot("01_全景");

            string bolt = NewGameFactory.OpeningBoltId;
            Step("确认开局绢", g.Run((s, c) => Craft.ConfirmBolt(s, c, bolt)));
            Step("染靛蓝", g.Run((s, c) => Craft.Dye(s, c, new Craft.DyeInput
            {
                boltId = bolt, dyeId = "indigo", concentration = DyeCalc.Medium, temperature = Craft.TempWarm,
                liftSeconds = c.balance.dye.LiftCenterOf(DyeCalc.Medium), stirSteady = 1, stirOff = 0, stirTotal = 1,
            })));
            var ids = new List<string>();
            foreach (var slot in new[] { "upper", "skirt", "drape" })
            {
                var r = g.Run((s, c) => Craft.Cut(s, c, new Craft.CutInput { boltId = bolt, slot = slot }));
                Step("裁" + slot, r);
                ids.Add(r.createdId);
            }
            var steady = new List<Beat>();
            for (int i = 0; i < g.Config.balance.weave.sewNeedlesPerPart; i++) steady.Add(Beat.Steady);
            Step("缝上襦", g.Run((s, c) => Craft.Sew(s, c, ids[0], steady)));
            Step("缝裙", g.Run((s, c) => Craft.Sew(s, c, ids[1], steady)));

            w.Approach(w.stations.Find(x => x.stationId == "station_dye"), true);
            yield return Wait(1.5f);
            yield return Shot("02_染缸");

            var form = w.stations.Find(x => x.stationId == "station_form");
            w.Approach(form, true);
            yield return Wait(2f);
            yield return Shot("03_人台");
            Hud.I.CurrentPlaque.onPrimary?.Invoke(); // 收成襦裙
            yield return Wait(1f);
            Step("收成襦裙", g.Save.garments.Count == 1 ? Result.Ok() : Result.Fail("人台没收成成衣"));
            Hud.I.onTransmittance?.Invoke();
            yield return Wait(3f);
            yield return Shot("04_材料透光");

            w.Approach(w.stations.Find(x => x.stationId == "station_stage"), true);
            yield return Wait(1.5f);
            Hud.I.CurrentPlaque.onPrimary?.Invoke(); // 开演
            var pd = FindFirstObjectByType<PerformanceDirector>();
            var frames = new List<float>();
            float t0 = Time.unscaledTime;
            bool shot = false;
            while (pd != null && pd.Playing && Time.unscaledTime - t0 < 120f)
            {
                frames.Add(Time.unscaledDeltaTime);
                if (!shot && Time.unscaledTime - t0 > 6f) { shot = true; yield return Shot("05_戏台0档"); }
                yield return null;
            }
            Step("0 档演完", g.Save.performances.Count == 1 ? Result.Ok() : Result.Fail("没有演出记录"));
            ReportFrames(frames);

            w.Approach(w.stations.Find(x => x.stationId == "station_museum"), true);
            yield return Wait(1.5f);
            Step("放入展柜", g.Run((s, c) => Exhibits.Place(s, c, 0, "garment", s.garments[0].id)));
            Exhibits.CheckTutorial3(g.Save);
            w.Approach(w.stations.Find(x => x.stationId == "station_museum"), true);
            yield return Wait(2f);
            yield return Shot("06_展柜");
            Step("教学三步完成", Find.Quest(g.Save, Craft.Tutorial3).done ? Result.Ok() : Result.Fail("教学三未完成"));

            report.AppendLine("错误日志 " + errors + " 条");
            report.AppendLine(errors == 0 ? "结果：通过" : "结果：有错误");
            File.WriteAllText(Path.Combine(outDir, "report.txt"), report.ToString(), Encoding.UTF8);
            yield return Wait(0.5f);
            Application.Quit(errors == 0 ? 0 : 1);
        }

        void Step(string what, Result r)
        {
            report.AppendLine((r.ok ? "✓ " : "✗ ") + what + (r.ok ? "" : "：" + r.error));
            if (!r.ok) errors++;
        }

        void ReportFrames(List<float> f)
        {
            if (f.Count == 0) { report.AppendLine("演出帧时间：无"); return; }
            f.Sort();
            float sum = 0; foreach (var x in f) sum += x;
            float avg = sum / f.Count;
            float p95 = f[Mathf.Min(f.Count - 1, Mathf.FloorToInt(f.Count * 0.95f))];
            report.AppendLine("演出 " + f.Count + " 帧：平均 " + (avg * 1000f).ToString("0.0") + " 毫秒（" + (1f / avg).ToString("0") + " 帧/秒），95% 分位 " + (p95 * 1000f).ToString("0.0") + " 毫秒，最慢 " + (f[f.Count - 1] * 1000f).ToString("0.0") + " 毫秒");
        }

        static IEnumerator Wait(float s)
        {
            float t = Time.unscaledTime + s;
            while (Time.unscaledTime < t) yield return null;
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            report.AppendLine("截图 " + name);
        }
    }
}
