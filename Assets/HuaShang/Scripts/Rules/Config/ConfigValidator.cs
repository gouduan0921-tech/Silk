using System.Collections.Generic;

namespace HuaShang.Rules.Config
{
    /// <summary>
    /// docs/05 §9 加载校验，外加本工程要求的几条：首发四个品种 id、开局物品可解析、
    /// 解算固定项已读到。返回错误列表，空表示通过。
    /// </summary>
    public static class ConfigValidator
    {
        /// <summary>docs/05 §2 写明的首发品种 id。</summary>
        public static readonly string[] LaunchVarietyIds = { "suSha", "juan", "chou", "zhuMa" };

        /// <summary>docs/05 §4 的槽位。</summary>
        public static readonly string[] Slots = { "inner", "upper", "skirt", "drape", "robe", "wrap" };

        /// <summary>docs/05 §5：除此角色外，首发角色的 launchTierMax 不超过 0。</summary>
        public const string TierExceptionCharacter = "xiShi";

        public static List<string> Validate(ConfigSnapshot c)
        {
            var errors = new List<string>();
            if (c == null) { errors.Add("配置为空"); return errors; }

            // 1 每个首发品种在 docs/04 §8 有行
            foreach (var id in LaunchVarietyIds)
            {
                var v = c.varieties.Find(x => x.id == id);
                if (v == null) errors.Add("首发品种 " + id + " 在 docs/04 §8 没有行");
                else if (!v.launch) errors.Add("首发品种 " + id + " 的 launch 为 false");
            }
            foreach (var v in c.varieties)
            {
                if (c.stretchGroups.Find(g => g.id == v.stretchGroup) == null)
                    errors.Add("品种 " + v.id + " 的 stretchGroup " + v.stretchGroup + " 不存在");
                if (v.densityMin > v.densityMax || v.bendMin > v.bendMax || v.windMin > v.windMax)
                    errors.Add("品种 " + v.id + " 的区间低端大于高端");
            }

            // 2 角色偏好的品种 id 存在
            foreach (var ch in c.characters)
                foreach (var pv in ch.preferVariety)
                    if (c.varieties.Find(v => v.id == pv) == null)
                        errors.Add("角色 " + ch.id + " 偏好的品种 " + pv + " 不存在");

            // 3 首发形制的部件都有槽位
            foreach (var p in c.patterns)
            {
                if (!p.launch) continue;
                foreach (var part in p.parts)
                    if (System.Array.IndexOf(Slots, part) < 0)
                        errors.Add("形制 " + p.id + " 的部件 " + part + " 没有槽位");
            }

            // 4 首发角色的 launchTierMax 不超过 30，除西施外不超过 0
            foreach (var ch in c.characters)
            {
                if (!ch.enabled) continue;
                int cap = ch.id == TierExceptionCharacter ? 30 : 0;
                if (ch.launchTierMax > cap)
                    errors.Add("角色 " + ch.id + " 的 launchTierMax " + ch.launchTierMax + " 超过 " + cap);
            }

            // 5 非首发角色关闭：首发名单之外的角色必须 enabled 为 false（docs/16 §3、docs/11）
            foreach (var ch in c.characters)
                if (ch.enabled && ch.id != "xiShi" && ch.id != "wangZhaoJun" && ch.id != "zhaoFeiYan")
                    errors.Add("非首发角色 " + ch.id + " 未关闭");

            // 其他：开局与固定项
            if (c.opening.bolt == null || string.IsNullOrEmpty(c.opening.bolt.variety))
                errors.Add("docs/04 §1 开局布 A 未读出");
            else if (c.varieties.Find(v => v.id == c.opening.bolt.variety) == null)
                errors.Add("开局布 A 的品种 " + c.opening.bolt.variety + " 不存在");
            foreach (var d in c.opening.dyes)
                if (c.dyes.Find(x => x.id == d.dyeId) == null)
                    errors.Add("开局染料 " + d.dyeId + " 不存在");
            if (c.balance.quality.bands.Count == 0) errors.Add("docs/04 §4 档位表未读出");
            if (c.balance.quality.dynasties.Find(d => d.launch && d.parsed) == null)
                errors.Add("docs/04 §4 没有可用的首发朝代偏移");
            if (c.clothFixed.triangleBendingStiffness <= 0 || c.clothFixed.radius <= 0)
                errors.Add("docs/03 §2 固定项未读出");

            return errors;
        }
    }
}
