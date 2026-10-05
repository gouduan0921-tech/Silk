using System.Collections.Generic;
using UnityEngine;
using HuaShang.Greybox;

namespace HuaShang.Performance
{
    /// <summary>
    /// 篇章特效（docs/11 §3）：水波、月华。只是演出临时参数，不写回布匹，不改品质、契合、热度。
    /// 30 档起出现；用 Time.deltaTime 推进，演出暂停（timeScale 0）时一起停。
    /// </summary>
    public class StageEffects : MonoBehaviour
    {
        const int RingCount = 4;
        const float RingPeriod = 3.2f;
        const int MoteCount = 36;

        string kind;
        float t;
        readonly List<Transform> rings = new List<Transform>();
        readonly List<Material> ringMats = new List<Material>();
        readonly List<Transform> motes = new List<Transform>();
        Light extra;
        GameObject root;

        public void Begin(string effect, int tier)
        {
            End();
            if (string.IsNullOrEmpty(effect) || tier < 30) return;
            kind = effect;
            t = 0f;
            root = new GameObject("FX_" + effect);
            root.transform.SetParent(transform, false);
            if (effect == "water") BuildWater();
            else if (effect == "moon") BuildMoon();
        }

        public void End()
        {
            if (root != null) Destroy(root);
            root = null; kind = null;
            rings.Clear(); ringMats.Clear(); motes.Clear(); extra = null;
        }

        void BuildWater()
        {
            for (int i = 0; i < RingCount; i++)
            {
                var g = Props.Prim(PrimitiveType.Cylinder, root.transform, "Ripple", new Vector3(0, 0.005f, 0), new Vector3(0.5f, 0.002f, 0.5f), Color.white, false);
                var m = LitMaterials.New(LitMaterials.Kind.Transparent, new Color(0.55f, 0.78f, 0.80f, 0.35f));
                g.GetComponent<Renderer>().sharedMaterial = m;
                g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rings.Add(g.transform); ringMats.Add(m);
            }
            extra = MakeLight("WaterRim", new Vector3(0, 0.3f, 1.6f), new Vector3(0, 1.0f, 0), new Color(0.55f, 0.85f, 0.85f), 1.4f, 60f);
        }

        void BuildMoon()
        {
            extra = MakeLight("MoonGlow", new Vector3(0, 4.2f, -0.4f), new Vector3(0, 1.0f, 0), Color.white, 2.2f, 28f);
            extra.useColorTemperature = true;
            extra.colorTemperature = 7500f;
            var mat = LitMaterials.New(LitMaterials.Kind.Unlit, new Color(0.92f, 0.95f, 1f));
            var rnd = new System.Random(7);
            for (int i = 0; i < MoteCount; i++)
            {
                var g = Props.Prim(PrimitiveType.Sphere, root.transform, "Mote",
                    new Vector3((float)rnd.NextDouble() * 2.4f - 1.2f, (float)rnd.NextDouble() * 3.0f, (float)rnd.NextDouble() * 1.6f - 1.0f),
                    Vector3.one * 0.018f, Color.white, false);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                motes.Add(g.transform);
            }
        }

        Light MakeLight(string name, Vector3 pos, Vector3 look, Color c, float intensity, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(transform.TransformPoint(look));
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = c;
            l.intensity = intensity;
            l.spotAngle = angle;
            l.range = 8f;
            l.shadows = LightShadows.None;
            return l;
        }

        void Update()
        {
            if (kind == null) return;
            float dt = Time.deltaTime;
            t += dt;
            if (kind == "water")
            {
                for (int i = 0; i < rings.Count; i++)
                {
                    float p = Mathf.Repeat(t / RingPeriod + i / (float)RingCount, 1f);
                    float d = Mathf.Lerp(0.4f, 3.0f, p);
                    rings[i].localScale = new Vector3(d, 0.002f, d);
                    var col = ringMats[i].color; col.a = 0.35f * (1f - p);
                    ringMats[i].color = col;
                }
            }
            else if (kind == "moon")
            {
                for (int i = 0; i < motes.Count; i++)
                {
                    var p = motes[i].localPosition;
                    p.y -= dt * (0.18f + (i % 5) * 0.03f);
                    p.x += Mathf.Sin(t * 0.7f + i) * dt * 0.05f;
                    if (p.y < 0f) p.y += 3.0f;
                    motes[i].localPosition = p;
                }
            }
        }
    }
}
