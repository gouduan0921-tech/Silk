using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §4 形制。用量来自 docs/04 §2。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class PatternTable : ScriptableObject
    {
        public List<PatternRow> rows = new List<PatternRow>();
    }
}
