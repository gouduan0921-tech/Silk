using System.Collections.Generic;
using UnityEngine;

namespace HuaShang.Solve
{
    /// <summary>读取部件当前显示的网格顶点（世界坐标），供测试与调试判定穿身、炸布、延迟。</summary>
    public static class ClothSampler
    {
        static readonly List<Vector3> buffer = new List<Vector3>();

        public static List<Vector3> WorldVertices(ClothPart part, List<Vector3> into = null)
        {
            into = into ?? new List<Vector3>();
            into.Clear();
            var mf = part.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return into;
            mf.sharedMesh.GetVertices(buffer);
            var m = part.transform.localToWorldMatrix;
            foreach (var v in buffer) into.Add(m.MultiplyPoint3x4(v));
            return into;
        }

        /// <summary>v 最小（自由边）一圈顶点的平均位置：袖口、裙摆、布的下沿。</summary>
        public static Vector3 FreeEdgeCenter(ClothPart part)
        {
            var mf = part.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            if (mesh == null) return part.transform.position;
            var uv = new List<Vector2>();
            mesh.GetUVs(0, uv);
            mesh.GetVertices(buffer);
            Vector3 sum = Vector3.zero;
            int n = 0;
            for (int i = 0; i < buffer.Count && i < uv.Count; i++)
                if (uv[i].y < 0.001f) { sum += buffer[i]; n++; }
            if (n == 0) return part.transform.position;
            return part.transform.localToWorldMatrix.MultiplyPoint3x4(sum / n);
        }
    }
}
