using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Greybox;

namespace HuaShang.EditorTools
{
    /// <summary>美术接入清单（docs/17 §7）：列出每个接入口已有 / 缺的资产，写到 Assets/HuaShang/Art/美术接入清单.md。</summary>
    public static class ArtReport
    {
        [MenuItem("HuaShang/美术/生成美术接入清单")]
        public static string Write()
        {
            ArtLibrary.ClearCache();
            var c = GameConfig.LoadInEditor();
            var sb = new StringBuilder();
            sb.AppendLine("# 美术接入清单");
            sb.AppendLine();
            sb.AppendLine("资产放在 `Assets/HuaShang/Resources/HuaShang/Art/` 下，按下表的名字放进去就会替换灰盒（docs/17 §7）。✓ 已有，· 缺。");
            int have = 0, total = 0;
            void Row(string group, string name, bool ok) { total++; if (ok) have++; sb.AppendLine("| " + group + " | `" + name + "` | " + (ok ? "✓" : "·") + " |"); }
            sb.AppendLine();
            sb.AppendLine("| 类 | 名字 | 状态 |");
            sb.AppendLine("|---|---|---|");
            foreach (var ch in c.characters) if (ch.enabled) Row("角色", "Characters/SK_" + ch.id, ArtLibrary.Character(ch.id) != null);
            foreach (var p in c.patterns)
                foreach (var slot in p.parts) Row("衣片", "Garments/SK_part_" + slot + "_" + p.id + "（或 _any）", ArtLibrary.GarmentMesh(slot, p.id) != null);
            foreach (var v in c.varieties) if (v.launch && !v.liningOnly) Row("面料", "Fabrics/T_fabric_" + v.id + "_n", ArtLibrary.FabricNormal(v.id) != null);
            foreach (var id in new[] { "workshop", "station_silk", "station_loom", "station_loom_satin", "station_loom_draw", "station_loom_leno", "station_dye", "station_cut", "station_sew", "station_market", "stage_classic", "stage_ink" })
                Row("场景", "Props/SM_" + id, ArtLibrary.Prop(id) != null);
            foreach (var s in new[] { "shuttle_steady", "shuttle_rough", "thread_break", "needle", "needle_dry", "water", "water_broken", "wood", "cloth_gauze", "cloth_silk", "confirm" })
                Row("音效", "Audio/A_sfx_" + s, ArtLibrary.Sfx(s) != null);
            sb.Insert(sb.ToString().IndexOf('\n') + 1, "\n已有 " + have + " / " + total + "。\n");
            Directory.CreateDirectory("Assets/HuaShang/Art");
            File.WriteAllText("Assets/HuaShang/Art/美术接入清单.md", sb.ToString());
            AssetDatabase.Refresh();
            return "已有 " + have + " / " + total;
        }
    }
}
