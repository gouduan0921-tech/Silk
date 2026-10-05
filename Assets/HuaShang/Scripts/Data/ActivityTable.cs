using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §8 活动，默认全部关闭。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class ActivityTable : ScriptableObject
    {
        public List<ActivityRow> rows = new List<ActivityRow>();
    }
}
