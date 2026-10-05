using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §3 染料。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class DyeTable : ScriptableObject
    {
        public List<DyeRow> rows = new List<DyeRow>();
    }
}
