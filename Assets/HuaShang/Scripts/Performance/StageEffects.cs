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
        readonly List<Material> moteMats = new List<Material>();
        readonly List<Vector3> moteVel = new List<Vector3>();
        Light extra;
        GameObject root;
        int spawnCursor;
        float spawnTimer;

        /// <summary>舞台上的成衣显示；白蛇特效跟披帛末端（docs/11 §3）。</summary>
        public HuaShang.Solve.GarmentVisual visual;

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
            else if (effect == "snake") BuildSnake();
            else if (effect == "petal") BuildPetals();
            else if (effect == "foam") BuildFoam();
        }

        public void End()
        {
            if (root != null) Destroy(root);
            foreach (var m in ringMats) if (m != null) Destroy(m);
            foreach (var m in moteMats) if (m != null) Destroy(m);
            root = null; kind = null;
            rings.Clear(); ringMats.Clear(); motes.Clear(); moteMats.Clear(); moteVel.Clear(); extra = null;
            spawnCursor = 0; spawnTimer = 0f;
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

        const int ScaleCount = 48;
        const float ScaleLife = 1.2f;

        void BuildSnake()
        {
            // 鳞光碎点池：从披帛两端轮流放出，原地慢慢下沉、渐隐
            for (int i = 0; i < ScaleCount; i++)
            {
                var g = Props.Prim(PrimitiveType.Cube, root.transform, "Scale", Vector3.zero, new Vector3(0.022f, 0.004f, 0.016f), Color.white, false);
                var m = LitMaterials.New(LitMaterials.Kind.Transparent, new Color(0.88f, 0.93f, 0.95f, 0f));
                m.SetFloat("_Smoothness", 0.95f);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                g.SetActive(false);
                motes.Add(g.transform); moteMats.Add(m); moteVel.Add(new Vector3(-1f, 0, 0)); // x 记剩余寿命，<0 为空闲
            }
        }

        void BuildPetals()
        {
            var rnd = new System.Random(11);
            for (int i = 0; i < MoteCount; i++)
            {
                var g = Props.Prim(PrimitiveType.Cube, root.transform, "Petal", Vector3.zero, new Vector3(0.05f, 0.004f, 0.035f), Color.white, false);
                var m = LitMaterials.New(LitMaterials.Kind.Opaque, Color.Lerp(new Color(0.93f, 0.55f, 0.62f), new Color(0.97f, 0.75f, 0.78f), (float)rnd.NextDouble()));
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                motes.Add(g.transform); moteMats.Add(m);
                // 极坐标：半径、起始角、起始高度
                moteVel.Add(new Vector3(0.7f + (float)rnd.NextDouble() * 0.6f, (float)rnd.NextDouble() * Mathf.PI * 2f, (float)rnd.NextDouble() * 3.2f));
            }
        }

        void BuildFoam()
        {
            var mat = LitMaterials.New(LitMaterials.Kind.Opaque, new Color(0.95f, 0.97f, 0.97f));
            moteMats.Add(mat);
            var rnd = new System.Random(5);
            for (int i = 0; i < MoteCount; i++)
            {
                var g = Props.Prim(PrimitiveType.Sphere, root.transform, "Foam", Vector3.zero, Vector3.one * 0.03f, Color.white, false);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                motes.Add(g.transform);
                // 半径、角度、相位
                moteVel.Add(new Vector3(0.3f + (float)rnd.NextDouble() * 0.45f, (float)rnd.NextDouble() * Mathf.PI * 2f, (float)rnd.NextDouble()));
            }
        }

        /// <summary>披帛两端的世界坐标；没有披帛时取两手高度（docs/11 §3）。</summary>
        void SnakeEnds(out Vector3 a, out Vector3 b)
        {
            Renderer drape = null;
            if (visual != null)
                foreach (var p in visual.parts)
                    if (p != null && p.layer == HuaShang.Solve.ClothLayer.Drape && p.sourceRenderer != null) { drape = p.sourceRenderer; break; }
            if (drape != null)
            {
                var bb = drape.bounds;
                float y = bb.min.y + 0.06f;
                a = new Vector3(bb.min.x + 0.04f, y, bb.center.z);
                b = new Vector3(bb.max.x - 0.04f, y, bb.center.z);
                return;
            }
            // 跟着人体根节点（会随转身旋转）取两手位置
            var body = visual != null && visual.body != null ? visual.body.transform : transform;
            a = body.TransformPoint(GreyboxBody.ArmPoints(-1)[2]);
            b = body.TransformPoint(GreyboxBody.ArmPoints(1)[2]);
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
            else if (kind == "snake")
            {
                spawnTimer += dt;
                SnakeEnds(out var ea, out var eb);
                while (spawnTimer > 0.05f)
                {
                    spawnTimer -= 0.05f;
                    int i = spawnCursor; spawnCursor = (spawnCursor + 1) % motes.Count;
                    var at = (i % 2 == 0 ? ea : eb) + new Vector3(Mathf.Sin(t * 13f + i) * 0.03f, 0, Mathf.Cos(t * 11f + i) * 0.03f);
                    motes[i].position = at;
                    motes[i].rotation = Quaternion.Euler(Mathf.Sin(i) * 40f, i * 37f, 0);
                    motes[i].gameObject.SetActive(true);
                    moteVel[i] = new Vector3(ScaleLife, 0, 0);
                }
                for (int i = 0; i < motes.Count; i++)
                {
                    float life = moteVel[i].x;
                    if (life < 0) continue;
                    life -= dt;
                    moteVel[i] = new Vector3(life, 0, 0);
                    if (life < 0) { motes[i].gameObject.SetActive(false); continue; }
                    motes[i].position += Vector3.down * dt * 0.08f;
                    motes[i].Rotate(0, dt * 90f, 0, Space.Self);
                    var col = moteMats[i].color; col.a = 0.9f * (life / ScaleLife);
                    moteMats[i].color = col;
                }
            }
            else if (kind == "foam")
            {
                for (int i = 0; i < motes.Count; i++)
                {
                    var v = moteVel[i];
                    float ph = Mathf.Repeat(v.z + t * (0.35f + (i % 4) * 0.05f), 1f); // 0 生出，1 消散
                    float ang = v.y + ph * 0.6f;
                    motes[i].localPosition = new Vector3(Mathf.Cos(ang) * v.x, ph * 0.7f, Mathf.Sin(ang) * v.x * 0.8f);
                    motes[i].localScale = Vector3.one * Mathf.Lerp(0.035f, 0.004f, ph);
                }
            }
            else if (kind == "petal")
            {
                for (int i = 0; i < motes.Count; i++)
                {
                    var v = moteVel[i];
                    float ang = v.y + t * (0.6f + (i % 4) * 0.1f);
                    float h = v.z - t * (0.25f + (i % 3) * 0.05f);
                    h = Mathf.Repeat(h, 3.2f);
                    motes[i].localPosition = new Vector3(Mathf.Cos(ang) * v.x, h, Mathf.Sin(ang) * v.x * 0.8f);
                    motes[i].localRotation = Quaternion.Euler(t * 70f + i * 20f, t * 50f + i * 13f, t * 30f);
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
