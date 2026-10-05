using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;
using HuaShang.Greybox;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Solve;

namespace HuaShang.Probe
{
    /// <summary>
    /// P1 灰盒探针：在一个场景里摆出 docs/03 §5 的四条视觉证明。
    /// 场景生成器与 PlayMode 测试共用这一个搭建函数，保证测的就是看的。
    /// </summary>
    public class ProbeRig : MonoBehaviour
    {
        // VP1：素纱 / 绸 盖在靛蓝绸上
        public ClothPart vp1InnerA, vp1InnerB, vp1GauzeOuter, vp1SilkOuter;
        public Camera vp1Camera;
        public Transform vp1SampleGauzeOverIndigo, vp1SampleSilkOverIndigo, vp1SampleGauzeOverBackdrop;

        // VP2：同版型素纱袖与绸袖，同一阵侧风
        public ClothPart vp2GauzeSleeve, vp2SilkSleeve;
        public MagicaWindZone vp2SideWind;

        // VP3 / VP4：灰盒人体上的襦裙
        public GreyboxBody vp4Body;
        public ClothPart vp4Upper, vp4Skirt;
        public MagicaWindZone baseWind;
        public ClothQualityDirector director;

        public const string GauzeId = "suSha";
        public const string SilkId = "chou";

        public static ProbeRig Build(ConfigSnapshot c, Transform parent = null)
        {
            var root = new GameObject("HS_ProbeRig");
            if (parent != null) root.transform.SetParent(parent, false);
            var rig = root.AddComponent<ProbeRig>();
            rig.BuildAll(c);
            return rig;
        }

        void BuildAll(ConfigSnapshot c)
        {
            var fixedSettings = c.clothFixed;
            string dynasty = ClothFactory.LaunchDynasty(c);
            // 灰盒统一取精良档中点，只为比较品种之间的差别。
            var fine = c.balance.quality.bands.Find(b => b.tier == QualityCalc.Fine);
            double t = QualityCalc.T((fine.qMin + fine.qMax) * 0.5, c.balance.quality);

            var indigo = new List<DyeLayer>
            {
                new DyeLayer
                {
                    dyeId = "indigo",
                    concentration = DyeCalc.Strong,
                    strength = DyeCalc.Strength(DyeCalc.Strong, 1, 1, c.balance.dye),
                    uneven = 0,
                    maskId = null,
                }
            };
            var undyed = new List<DyeLayer>();
            var paintTop = GreyboxMeshes.PaintMap(0.06f);

            director = gameObject.AddComponent<ClothQualityDirector>();
            director.fixedSettings = fixedSettings;
            director.globalQuality = ClothQuality.High;

            Environment();

            // 基础风（docs/04 §9：0 档只用基础风），取区间中点
            baseWind = new GameObject("BaseWind").AddComponent<MagicaWindZone>();
            baseWind.transform.SetParent(transform, false);
            baseWind.mode = MagicaWindZone.Mode.GlobalDirection;
            baseWind.main = (float)((c.balance.windLight.baseWindMin + c.balance.windLight.baseWindMax) * 0.5);
            baseWind.transform.rotation = Quaternion.Euler(0, 90, 0);

            // ---- VP1 ----
            var vp1 = new GameObject("VP1_透光").transform;
            vp1.SetParent(transform, false);
            vp1.localPosition = new Vector3(-3f, 1.6f, 0f);
            Backdrop(vp1, new Vector3(0, -0.6f, -0.08f), new Vector2(1.8f, 1.4f));
            vp1InnerA = Part(vp1, "Inner_Indigo_A", GreyboxMeshes.Panel("P_inner", 0.6f, 0.6f), new Vector3(-0.4f, 0, 0), SilkId, indigo, c, t, dynasty, paintTop, null, ClothLayer.Inner, ClothContext.Probe);
            vp1InnerB = Part(vp1, "Inner_Indigo_B", GreyboxMeshes.Panel("P_inner", 0.6f, 0.6f), new Vector3(0.4f, 0, 0), SilkId, indigo, c, t, dynasty, paintTop, null, ClothLayer.Inner, ClothContext.Probe);
            vp1GauzeOuter = Part(vp1, "Outer_SuSha", GreyboxMeshes.Panel("P_outer", 0.6f, 1.0f), new Vector3(-0.4f, 0, 0.03f), GauzeId, undyed, c, t, dynasty, paintTop, null, ClothLayer.Outer, ClothContext.Probe);
            vp1SilkOuter = Part(vp1, "Outer_Chou", GreyboxMeshes.Panel("P_outer", 0.6f, 1.0f), new Vector3(0.4f, 0, 0.03f), SilkId, undyed, c, t, dynasty, paintTop, null, ClothLayer.Outer, ClothContext.Probe);
            vp1SampleGauzeOverIndigo = Marker(vp1, "Sample_GauzeOverIndigo", new Vector3(-0.4f, -0.35f, 0.05f));
            vp1SampleSilkOverIndigo = Marker(vp1, "Sample_SilkOverIndigo", new Vector3(0.4f, -0.35f, 0.05f));
            vp1SampleGauzeOverBackdrop = Marker(vp1, "Sample_GauzeOverBackdrop", new Vector3(-0.4f, -0.85f, 0.05f));
            vp1Camera = new GameObject("VP1_Camera").AddComponent<Camera>();
            vp1Camera.transform.SetParent(vp1, false);
            vp1Camera.transform.localPosition = new Vector3(0, -0.5f, 2.2f);
            vp1Camera.transform.localRotation = Quaternion.Euler(0, 180, 0);
            vp1Camera.fieldOfView = 40f;
            vp1Camera.enabled = false;

            // ---- VP2 ----
            var vp2 = new GameObject("VP2_同风延迟").transform;
            vp2.SetParent(transform, false);
            vp2.localPosition = new Vector3(0f, 1.6f, 0f);
            var sleeve = GreyboxMeshes.Tube("P_sleeve", 0.12f, 0.16f, 0.6f);
            vp2GauzeSleeve = Part(vp2, "Sleeve_SuSha", sleeve, new Vector3(-0.4f, 0, 0), GauzeId, undyed, c, t, dynasty, paintTop, null, ClothLayer.Outer, ClothContext.Probe);
            vp2SilkSleeve = Part(vp2, "Sleeve_Chou", GreyboxMeshes.Tube("P_sleeve", 0.12f, 0.16f, 0.6f), new Vector3(0.4f, 0, 0), SilkId, undyed, c, t, dynasty, paintTop, null, ClothLayer.Outer, ClothContext.Probe);
            vp2SideWind = new GameObject("SideWind").AddComponent<MagicaWindZone>();
            vp2SideWind.transform.SetParent(vp2, false);
            vp2SideWind.transform.localPosition = new Vector3(0, -0.3f, 0);
            vp2SideWind.mode = MagicaWindZone.Mode.BoxDirection;
            vp2SideWind.size = new Vector3(2f, 1.5f, 1.5f);
            // 侧风只出现在 30 档转身（docs/08 §3），风速取 30 档上限（docs/04 §9）
            vp2SideWind.main = (float)c.balance.windLight.tier30WindMax;
            vp2SideWind.SetWindDirection(Vector3.right);
            vp2SideWind.enabled = false;

            // ---- VP3 / VP4 ----
            var vp4 = new GameObject("VP4_零档三十秒").transform;
            vp4.SetParent(transform, false);
            vp4.localPosition = new Vector3(3f, 0f, 0f);
            var skin = LitMaterials.New(LitMaterials.Kind.Opaque, new Color(0.55f, 0.53f, 0.5f));
            vp4Body = GreyboxBody.Create("Body_西施灰盒", vp4, skin, false);
            vp4Body.gameObject.AddComponent<ProbeMotion>();
            vp4Skirt = Part(vp4Body.hips, "Skirt_Indigo", GreyboxMeshes.Tube("P_skirt", 0.17f, 0.42f, 0.9f), new Vector3(0, 0.12f, 0), SilkId, indigo, c, t, dynasty, paintTop, vp4Body.colliders, ClothLayer.Outer, ClothContext.Probe);
            vp4Upper = Part(vp4Body.torso, "Upper_SuSha", GreyboxMeshes.Tube("P_upper", 0.16f, 0.24f, 0.42f), new Vector3(0, 0.18f, 0), GauzeId, undyed, c, t, dynasty, paintTop, vp4Body.colliders, ClothLayer.Outer, ClothContext.Probe);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.transform.SetParent(transform, false);
            cam.transform.localPosition = new Vector3(0, 1.4f, 6.5f);
            cam.transform.localRotation = Quaternion.Euler(5, 180, 0);
            director.viewer = cam.transform;
        }

        void Environment()
        {
            var lightGo = new GameObject("Key Light");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.rotation = Quaternion.Euler(40, 160, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 0.9f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.32f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(2, 1, 1);
            ground.GetComponent<MeshRenderer>().sharedMaterial = LitMaterials.New(LitMaterials.Kind.Opaque, new Color(0.42f, 0.41f, 0.39f));
        }

        static void Backdrop(Transform parent, Vector3 pos, Vector2 size)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Backdrop_White";
            DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos;
            q.transform.localRotation = Quaternion.Euler(0, 180, 0);
            q.transform.localScale = new Vector3(size.x, size.y, 1);
            var m = LitMaterials.New(LitMaterials.Kind.Unlit, new Color(0.82f, 0.82f, 0.80f));
            q.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        static Transform Marker(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static ClothPart Part(Transform parent, string name, Mesh mesh, Vector3 pos, string varietyId, List<DyeLayer> layers,
                              ConfigSnapshot c, double t, string dynasty, Texture2D paint, IList<ColliderComponent> colliders,
                              ClothLayer layer, ClothContext context)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var part = go.AddComponent<ClothPart>();
            part.sourceRenderer = mr;
            part.layer = layer;
            part.context = context;
            part.ownerId = name;
            var d = ClothFactory.Describe(c, varietyId, t, dynasty, layers, null, 0, null);
            part.Build(d, ClothFactory.Color(c, layers), c.clothFixed, paint, colliders, false);
            return part;
        }
    }
}
