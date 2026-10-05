using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §7 委托。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class QuestTable : ScriptableObject
    {
        public List<QuestRow> rows = new List<QuestRow>();
    }
}
