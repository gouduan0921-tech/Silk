using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §2 品种。数值来自 docs/04 §8。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class VarietyTable : ScriptableObject
    {
        public List<VarietyRow> rows = new List<VarietyRow>();
    }
}
