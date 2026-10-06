using System.Collections.Generic;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Greybox;
using HuaShang.Performance;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    public class ChainRig : MonoBehaviour
    {
        public readonly List<StationBase> stations = new List<StationBase>();
        public WorkshopController workshop;

        public static ChainRig Build(Transform parent)
        {
            var go = new GameObject("HS_ChainRig");
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<ChainRig>();
            rig.BuildAll();
            return rig;
        }

        void BuildAll()
        {
            var c = GameSession.I.Config;
            go("Sfx").AddComponent<Sfx>();
            var hudGo = go("HudRoot");
            hudGo.AddComponent<Hud>();

            var director = gameObject.AddComponent<ClothQualityDirector>();
            director.fixedSettings = c.clothFixed;
            director.globalQuality = (ClothQuality)Settings.Current.clothQuality;

            // 环境：中性灰米墙、旧木地面、柔和日光
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.35f, 0.33f);
            // 反射也用室内的中性灰米色；默认天空盒偏蓝，会让有光泽的绸、缎发青
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = NeutralReflection(new Color(0.62f, 0.60f, 0.56f));
            var sun = go("Daylight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(42, -25, 0);
            sun.intensity = 1.0f;
            sun.useColorTemperature = true;
            sun.colorTemperature = 5600;
            sun.shadows = LightShadows.Soft;
            UnityEngine.Rendering.GraphicsSettings.lightsUseColorTemperature = true;
            Props.Box(transform, "Floor", new Vector3(41, -0.05f, 2), new Vector3(102, 0.1f, 14), Props.Floor);
            Props.Box(transform, "BackWall", new Vector3(41, 2.2f, 3.2f), new Vector3(102, 4.4f, 0.2f), Props.Wall);
            for (int i = 0; i < 12; i++) Props.Box(transform, "Beam", new Vector3(-3.5f + i * 8f, 2.2f, 2.6f), new Vector3(0.18f, 4.4f, 0.18f), Props.Wood);

            var workshopArt = ArtLibrary.Prop("workshop"); // 地面、墙、梁
            if (workshopArt != null)
            {
                var env = new List<Renderer>();
                foreach (Transform t in transform) if (t.name == "Floor" || t.name == "BackWall" || t.name == "Beam") env.Add(t.GetComponent<Renderer>());
                ArtLibrary.Swap(workshopArt, transform, env);
            }

            var camGo = go("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            // 室内工坊：不露蓝天，背景用暗一档的墙色（风格规范「色与材质」）
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Props.Wall * 0.55f;
            RenderSettings.skybox = null;
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            var camDir = camGo.AddComponent<CameraDirector>();
            camDir.cam = cam;
            director.viewer = camGo.transform;

            Add<SilkStation>("station_silk", "00", "蚕房", "SILK HOUSE", "叶、茧、丝绪。蚕箔与缫丝。", 0f, s => s.BuildProps());
            // 四台织机各是一个工位（docs/07 §1）
            Add<LoomStation>("station_loom", "01", "平纹机", "SILK WORKSHOP", "经线整齐，踏板和梭打在节拍上。素纱、绢、绸。", 7f, s => s.BuildProps());
            Add<LoomStation>("station_loom_satin", "02", "缎机", "SILK WORKSHOP", "多片综框，经浮长，织出缎面的光。", 14f, s => { s.kind = Play.Craft.Loom.Satin; s.BuildProps(); });
            Add<LoomStation>("station_loom_draw", "03", "花楼机", "SILK WORKSHOP", "楼上拽花，楼下投梭，两人合织提花。", 21f, s => { s.kind = Play.Craft.Loom.Draw; s.BuildProps(); });
            Add<LoomStation>("station_loom_leno", "04", "罗机", "SILK WORKSHOP", "绞综交错，经线绞转，织出罗的孔眼。", 28f, s => { s.kind = Play.Craft.Loom.Leno; s.BuildProps(); });
            Add<DyeStation>("station_dye", "05", "染缸", "SILK WORKSHOP", "液面平静，靛蓝慢慢入布。", 35f, s => s.BuildProps());
            Add<CutStation>("station_cut", "06", "裁桌", "SILK WORKSHOP", "布摊开，剪刀和针能看清。", 42f, s => s.BuildProps());
            Add<SewStation>("station_sew", "07", "针线", "SILK WORKSHOP", "沿着缝线，让衣片接在一起。", 49f, s => s.BuildProps());
            var form = Add<FormStation>("station_form", "08", "人台", "SILK WORKSHOP", "只用基础风与中性白光，看衣领与下摆。", 56f, s => s.BuildProps());
            var stage = Add<StageStation>("station_stage", "09", "戏台", "CLASSIC STAGE", "木色戏台，背景暗于衣服。", 65f, s => s.BuildProps());
            var museum = Add<MuseumStation>("station_museum", "10", "展柜", "MUSEUM", "玻璃与中性墙，灯可以暖。", 75f, s => s.BuildProps());
            Add<MarketStation>("station_market", "11", "委托牌", "MARKET", "接委托、买卖、结束今天。", 82f, s => s.BuildProps());

            // 人台加基础风（docs/07 §5）
            var formWind = new GameObject("FormBaseWind").AddComponent<MagicaCloth2.MagicaWindZone>();
            formWind.transform.SetParent(form.transform, false);
            formWind.mode = MagicaCloth2.MagicaWindZone.Mode.SphereDirection;
            formWind.radius = 3f;
            formWind.main = (float)((c.balance.windLight.baseWindMin + c.balance.windLight.baseWindMax) * 0.5);

            // 戏台机位更宽
            stage.approachPose.localPosition = new Vector3(0, 2.6f, -7.5f);
            stage.approachPose.rotation = Quaternion.LookRotation(stage.transform.TransformPoint(new Vector3(0, 1.4f, 0)) - stage.approachPose.position);
            stage.enterPose.localPosition = new Vector3(0, 1.9f, -4.6f);
            stage.enterPose.rotation = Quaternion.LookRotation(stage.transform.TransformPoint(new Vector3(0, 1.4f, 0)) - stage.enterPose.position);
            stage.performance.cam = cam;

            // 人台整身入画：从头到裙摆（袖与披帛也在框里）
            form.enterPose.localPosition = new Vector3(-0.2f, 1.3f, -3.1f);
            form.enterPose.rotation = Quaternion.LookRotation(form.transform.TransformPoint(new Vector3(-0.2f, 0.95f, 0)) - form.enterPose.position);

            // 展柜整柜两个展位都要入画
            museum.enterPose.localPosition = new Vector3(-0.2f, 1.6f, -3.7f);
            museum.enterPose.rotation = Quaternion.LookRotation(museum.transform.TransformPoint(new Vector3(-0.2f, 1.2f, 0)) - museum.enterPose.position);

            var overview = new GameObject("OverviewPose").transform;
            overview.SetParent(transform, false);
            // 全景：略高的四分之三视角，工位沿纵深排开（参考 素材/工位链全景.png 的机位）
            overview.position = new Vector3(-11f, 6.8f, -11.5f);
            overview.rotation = Quaternion.LookRotation(new Vector3(25f, 0.6f, 1.2f) - overview.position);
            camGo.transform.SetPositionAndRotation(overview.position, overview.rotation);

            workshop = gameObject.AddComponent<WorkshopController>();
            workshop.cameraDirector = camDir;
            workshop.overviewPose = overview;
            workshop.stations = stations;
            Hud.I.onTransmittance = () => { workshop.Approach(form); workshop.Enter(form); form.ShowTransmittance(); };
        }

        static Cubemap NeutralReflection(Color c)
        {
            const int size = 8;
            var cube = new Cubemap(size, TextureFormat.RGBA32, false) { name = "Cube_neutral_room" };
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            foreach (CubemapFace f in System.Enum.GetValues(typeof(CubemapFace)))
                if (f != CubemapFace.Unknown) cube.SetPixels(px, f);
            cube.Apply(false, true);
            return cube;
        }

        GameObject go(string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(transform, false);
            return g;
        }

        T Add<T>(string id, string number, string title, string kicker, string sub, float x, System.Action<T> build) where T : StationBase
        {
            var g = go(title);
            g.transform.localPosition = new Vector3(x, 0, 0);
            var s = g.AddComponent<T>();
            s.stationId = id; s.number = number; s.title = title; s.englishKicker = kicker; s.subtitle = sub;
            build(s);
            // 正式美术（docs/17 §7）：工坊工位整体换外形，会动的部件留着；人台、戏台、展柜里有人体与布，不整体替换
            if (id != "station_form" && id != "station_stage" && id != "station_museum")
            {
                var art = ArtLibrary.Prop(id);
                if (art != null) ArtLibrary.Swap(art, g.transform, g.GetComponentsInChildren<Renderer>(), new HashSet<Renderer>(s.LiveRenderers()));
            }
            s.approachPose = Props.Pose(g.transform, "ApproachPose", new Vector3(0, 2.3f, -4.6f), new Vector3(0, 0.9f, 0));
            // 进入机位整体左移一点：左侧木牌与右侧侧架之间的空处中心略偏右（素材/工位特写构图）
            s.enterPose = Props.Pose(g.transform, "EnterPose", new Vector3(-0.15f, 1.5f, -2.5f), new Vector3(-0.15f, 0.95f, 0.1f));
            stations.Add(s);
            return s;
        }
    }
}
