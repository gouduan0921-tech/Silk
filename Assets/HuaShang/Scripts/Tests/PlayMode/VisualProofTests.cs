using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HuaShang.Game;
using HuaShang.Probe;
using HuaShang.Rules.Config;
using HuaShang.Solve;

namespace HuaShang.Tests
{
    /// <summary>
    /// docs/03 §5 的四条视觉证明，在灰盒探针上用实时 MeshCloth 跑（P1 退出条件）。
    /// 判定只做相对比较，不设手册以外的阈值；实测值写进日志供评审。
    /// </summary>
    public class VisualProofTests
    {
        ConfigSnapshot c;
        ProbeRig rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            c = GameConfig.LoadInEditor();
#endif
            Time.captureFramerate = 60;
            rig = ProbeRig.Build(c);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            if (rig != null) Object.Destroy(rig.gameObject);
            yield return null;
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator VP1_素纱透出内层靛蓝()
        {
            yield return Frames(90);
            var cam = rig.vp1Camera;
            var rt = new RenderTexture(320, 200, 24);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;

            Color Sample(Transform t)
            {
                var vp = cam.WorldToViewportPoint(t.position);
                return tex.GetPixel(Mathf.RoundToInt(vp.x * (rt.width - 1)), Mathf.RoundToInt(vp.y * (rt.height - 1)));
            }
            var gauzeOverIndigo = Sample(rig.vp1SampleGauzeOverIndigo);
            var silkOverIndigo = Sample(rig.vp1SampleSilkOverIndigo);
            var gauzeOverBackdrop = Sample(rig.vp1SampleGauzeOverBackdrop);
            float Blue(Color x) => x.b - x.r;
            Debug.Log("[VP1] 素纱盖靛蓝 " + gauzeOverIndigo + "，绸盖靛蓝 " + silkOverIndigo + "，素纱盖白底 " + gauzeOverBackdrop);

            Assert.Greater(Blue(gauzeOverIndigo), Blue(gauzeOverBackdrop), "素纱下应看得出靛蓝");
            Assert.Greater(Blue(gauzeOverIndigo), Blue(silkOverIndigo), "同样盖在靛蓝上，素纱应比绸透出更多内层颜色");
            Object.Destroy(rt);
            Object.Destroy(tex);
        }

        [UnityTest]
        public IEnumerator VP2_素纱袖与绸袖同风延迟不同()
        {
            yield return Frames(120);
            Vector3 restA = ClothSampler.FreeEdgeCenter(rig.vp2GauzeSleeve);
            Vector3 restB = ClothSampler.FreeEdgeCenter(rig.vp2SilkSleeve);
            rig.vp2SideWind.enabled = true;

            var a = new List<float>();
            var b = new List<float>();
            for (int i = 0; i < 240; i++)
            {
                yield return null;
                a.Add(ClothSampler.FreeEdgeCenter(rig.vp2GauzeSleeve).x - restA.x);
                b.Add(ClothSampler.FreeEdgeCenter(rig.vp2SilkSleeve).x - restB.x);
            }
            float finalA = Tail(a), finalB = Tail(b);
            int lagA = HalfRise(a, finalA), lagB = HalfRise(b, finalB);
            Debug.Log("[VP2] 素纱袖：终位移 " + finalA.ToString("0.000") + " 米，半程 " + lagA + " 帧；绸袖：终位移 " + finalB.ToString("0.000") + " 米，半程 " + lagB + " 帧");

            Assert.Greater(Mathf.Abs(finalA), 0.005f, "素纱袖应被侧风吹动");
            Assert.Greater(Mathf.Abs(finalB), 0.005f, "绸袖应被侧风吹动");
            Assert.AreNotEqual(lagA, lagB, "同一阵侧风下两袖的延迟应不同");
            Assert.Greater(Mathf.Abs(finalA), Mathf.Abs(finalB), "素纱对风更敏感（docs/15 §2）");
        }

        static float Tail(List<float> xs)
        {
            float sum = 0; int n = 0;
            for (int i = xs.Count - 60; i < xs.Count; i++) { sum += xs[i]; n++; }
            return sum / n;
        }

        static int HalfRise(List<float> xs, float final)
        {
            for (int i = 0; i < xs.Count; i++)
                if (Mathf.Abs(xs[i]) >= Mathf.Abs(final) * 0.5f) return i;
            return xs.Count;
        }

        [UnityTest]
        public IEnumerator VP3_高低画质颜色一致()
        {
            yield return Frames(30);
            var parts = new[] { rig.vp4Skirt, rig.vp4Upper };
            var before = new Color[parts.Length];
            for (int i = 0; i < parts.Length; i++) before[i] = ClothLook.ReadColor(parts[i].material);

            rig.director.globalQuality = ClothQuality.Low;
            yield return Frames(10);
            for (int i = 0; i < parts.Length; i++)
            {
                Assert.IsFalse(parts[i].IsSimulated, "低档应关掉 MagicaCloth");
                Assert.IsTrue(parts[i].sourceRenderer.enabled, "低档仍要显示衣服");
                Assert.AreEqual(before[i], ClothLook.ReadColor(parts[i].material), "低档颜色与透明度不变");
            }
            rig.director.globalQuality = ClothQuality.High;
            yield return Frames(10);
            for (int i = 0; i < parts.Length; i++)
            {
                Assert.IsTrue(parts[i].IsSimulated);
                Assert.AreEqual(before[i], ClothLook.ReadColor(parts[i].material));
            }
        }

        [UnityTest]
        public IEnumerator VP4_零档三十秒不穿身不炸布()
        {
            var body = rig.vp4Body;
            var verts = new List<Vector3>();
            float tolerance = (float)c.clothFixed.radius;
            float worst = float.MaxValue;
            const int frames = 30 * 60;
            for (int f = 0; f < frames; f++)
            {
                yield return null;
                if (f % 10 != 0) continue;
                foreach (var part in new[] { rig.vp4Skirt, rig.vp4Upper })
                {
                    ClothSampler.WorldVertices(part, verts);
                    foreach (var v in verts)
                    {
                        Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x), "炸布：顶点无效");
                        Assert.Less(Vector3.Distance(v, body.transform.position + Vector3.up), 2.5f, "炸布：顶点飞离身体");
                        float d = body.SignedDistance(v);
                        if (d < worst) worst = d;
                    }
                }
            }
            Debug.Log("[VP4] 30 秒最深穿入 " + (-worst).ToString("0.0000") + " 米（允许不超过粒子半径 " + tolerance + "）");
            Assert.GreaterOrEqual(worst, -tolerance, "穿身：布料进入身体超过粒子半径");
        }
    }
}
