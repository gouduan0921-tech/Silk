using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;

namespace HuaShang.Solve
{
    /// <summary>布料出现的场合，决定质量档上限（docs/03 §3）。</summary>
    public enum ClothContext
    {
        Form = 0,
        Stage = 1,
        Exhibit = 2,
        Probe = 3,
    }

    /// <summary>
    /// 一个服装部件：源网格 + 一个 MeshCloth。只有解算模块改它的参数（构架文档 §2.1）。
    /// 每个服装部件一个组件（docs/03 §2）。
    /// </summary>
    [DisallowMultipleComponent]
    public class ClothPart : MonoBehaviour
    {
        static readonly List<ClothPart> registry = new List<ClothPart>();
        public static IReadOnlyList<ClothPart> All => registry;

        public ClothLayer layer = ClothLayer.Outer;
        public ClothContext context = ClothContext.Form;
        /// <summary>舞台上所属角色；同时只有一位角色用高或极致。</summary>
        public string ownerId;
        /// <summary>成衣带「层叠」词条。</summary>
        public bool layered;

        public MeshRenderer sourceRenderer;
        public MagicaCloth cloth;
        public Material material;
        public ClothDescriptor descriptor;

        bool built;

        /// <summary>透明布的绘制次序：里层先画，披帛最后。</summary>
        int DrawOrder => layer == ClothLayer.Inner ? 0 : layer == ClothLayer.Middle ? 1 : layer == ClothLayer.Outer ? 2 : 3;

        void OnEnable() { if (!registry.Contains(this)) registry.Add(this); }
        void OnDisable() { registry.Remove(this); }

        /// <summary>按 cloth 描述建立 MeshCloth 并写外观。paintMap：红=固定，绿=可动（MagicaCloth 2 约定）。</summary>
        public void Build(ClothDescriptor d, Color dyed, ClothFixed f, Texture2D paintMap,
                          IList<ColliderComponent> colliders, bool selfCollision)
        {
            descriptor = d;
            if (sourceRenderer == null) sourceRenderer = GetComponent<MeshRenderer>();
            if (material == null)
            {
                material = ClothLook.CreateMaterial();
                sourceRenderer.sharedMaterial = material;
            }
            ClothLook.Write(material, dyed, ClothLook.Alpha(d, f), ClothLook.Smoothness(d, f));
            ClothLook.Bind(sourceRenderer, material, DrawOrder);

            if (built) { ClothApplier.Apply(cloth, d, f); return; }

            var go = new GameObject(name + "_MeshCloth");
            go.transform.SetParent(transform, false);
            cloth = go.AddComponent<MagicaCloth>();
            var sdata = cloth.SerializeData;
            sdata.clothType = ClothProcess.ClothType.MeshCloth;
            sdata.sourceRenderers.Add(sourceRenderer);
            sdata.paintMode = ClothSerializeData.PaintMode.Texture_Fixed_Move;
            sdata.paintMaps.Add(paintMap);
            sdata.colliderCollisionConstraint.mode = ColliderCollisionConstraint.Mode.Point;
            if (colliders != null)
                foreach (var c in colliders) sdata.colliderCollisionConstraint.colliderList.Add(c);
            sdata.selfCollisionConstraint.selfMode = selfCollision
                ? SelfCollisionConstraint.SelfCollisionMode.FullMesh
                : SelfCollisionConstraint.SelfCollisionMode.None;
            ClothApplier.WriteSerializeData(sdata, d, f);
            cloth.BuildAndRun();
            built = true;
        }

        /// <summary>重新写外观与参数（染色、呈现模式拷贝等改变之后）。</summary>
        public void Refresh(ClothDescriptor d, Color dyed, ClothFixed f)
        {
            descriptor = d;
            ClothLook.Write(material, dyed, ClothLook.Alpha(d, f), ClothLook.Smoothness(d, f));
            ClothLook.Bind(sourceRenderer, material, DrawOrder);
            if (built) ClothApplier.Apply(cloth, d, f);
        }

        /// <summary>低档关掉 MagicaCloth，只留蒙皮或静态网格；颜色与透明度不变（docs/03 §3）。</summary>
        public void SetSimulated(bool on)
        {
            if (cloth != null && cloth.enabled != on) cloth.enabled = on;
        }

        public bool IsSimulated => cloth != null && cloth.enabled;

        bool selfCollision;

        /// <summary>自碰撞只在质量档「极致」打开（docs/03 §2）。</summary>
        public void SetSelfCollision(bool on)
        {
            if (cloth == null || selfCollision == on) return;
            selfCollision = on;
            cloth.SerializeData.selfCollisionConstraint.selfMode = on
                ? SelfCollisionConstraint.SelfCollisionMode.FullMesh
                : SelfCollisionConstraint.SelfCollisionMode.None;
            cloth.SetParameterChange();
        }
    }
}
