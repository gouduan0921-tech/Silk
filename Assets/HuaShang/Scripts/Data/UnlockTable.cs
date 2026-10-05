using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/10 §1 等级解锁。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class UnlockTable : ScriptableObject
    {
        public List<UnlockRow> rows = new List<UnlockRow>();
    }
}
