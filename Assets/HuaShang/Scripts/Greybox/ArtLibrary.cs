using System.Collections.Generic;
using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>
    /// 正式美术的接入口（docs/17 §7）。所有资产按 docs/17 的英文键放在
    /// <c>Assets/HuaShang/Resources/HuaShang/Art/</c> 下；有就用，没有就退回灰盒，不报错。
    /// 美术交付只需按名字放文件，不改代码。
    /// </summary>
    public static class ArtLibrary
    {
        public const string Root = "HuaShang/Art/";

        static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();

        static T Load<T>(string path) where T : Object
        {
            if (cache.TryGetValue(path, out var o)) return o as T;
            var a = Resources.Load<T>(Root + path);
            cache[path] = a;
            return a;
        }

        /// <summary>角色外形：Characters/SK_{id}（预制体，脚底在原点、面朝 −Z、身高约 1.6 米）。</summary>
        public static GameObject Character(string characterId) => string.IsNullOrEmpty(characterId) ? null : Load<GameObject>("Characters/SK_" + characterId);

        /// <summary>衣片网格：Garments/SK_part_{slot}_{pattern}（uv.v ≥ 0.999 固定，≤ 0.9 可动，同灰盒约定）。</summary>
        public static Mesh GarmentMesh(string slot, string pattern) => Load<Mesh>("Garments/SK_part_" + slot + "_" + (pattern ?? "ruQun"));

        /// <summary>面料结构法线：Fabrics/T_fabric_{varietyId}_n。</summary>
        public static Texture2D FabricNormal(string varietyId) => Load<Texture2D>("Fabrics/T_fabric_" + varietyId + "_n");

        /// <summary>工位或布景：Props/SM_{key}（预制体，放在工位原点）。</summary>
        public static GameObject Prop(string key) => Load<GameObject>("Props/SM_" + key);

        /// <summary>操作声：Audio/A_sfx_{name}。</summary>
        public static AudioClip Sfx(string name) => Load<AudioClip>("Audio/A_sfx_" + name);

        /// <summary>编辑器里换了资产后清缓存。</summary>
        public static void ClearCache() => cache.Clear();

        /// <summary>把一个美术预制体挂到 parent 下，并隐藏灰盒渲染器（keep 里的除外）。返回实例；没有美术返回 null。</summary>
        public static GameObject Swap(GameObject prefab, Transform parent, IEnumerable<Renderer> greybox, ICollection<Renderer> keep = null)
        {
            if (prefab == null || parent == null) return null;
            var go = Object.Instantiate(prefab, parent, false);
            go.name = prefab.name;
            foreach (var r in go.GetComponentsInChildren<Collider>()) Object.Destroy(r); // 美术不带碰撞；交互与布料碰撞仍用灰盒
            if (greybox != null)
                foreach (var r in greybox)
                    if (r != null && (keep == null || !keep.Contains(r)) && !r.transform.IsChildOf(go.transform)) r.enabled = false;
            return go;
        }
    }
}
