using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>docs/05 §5 角色。内容见 docs/16。关闭的角色保留，enabled 为 false。由「HuaShang/从 docs 导入配置表」生成，不在 Inspector 手改数值。</summary>
    public class CharacterTable : ScriptableObject
    {
        public List<CharacterRow> rows = new List<CharacterRow>();
    }
}
