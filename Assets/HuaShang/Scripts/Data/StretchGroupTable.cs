using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/04 §8 距离刚度与阻尼分组。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class StretchGroupTable : ScriptableObject
    {
        public List<StretchGroupRow> rows = new List<StretchGroupRow>();
    }
}
