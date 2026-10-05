using System.Collections.Generic;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 委托牌与市集（docs/02 §1、docs/10 §2–§3、docs/04 §7）：接委托、买卖、结束今天。
    /// 提示是一次木牌声，不循环叫卖（docs/18 §4）。不能用钱买成衣或好感（docs/25 §2）。
    /// </summary>
    public class MarketStation : StationBase
    {
        string pickBoltId, pickGarmentId;

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Board", new Vector3(0, 1.4f, 0.3f), new Vector3(1.6f, 1.1f, 0.06f), Props.Wood);
            for (int i = 0; i < 3; i++)
                Props.Box(root, "Note", new Vector3(-0.5f + i * 0.5f, 1.45f, 0.26f), new Vector3(0.36f, 0.5f, 0.01f), new Color(0.9f, 0.87f, 0.79f));
            Props.Box(root, "Post", new Vector3(0, 0.45f, 0.3f), new Vector3(0.1f, 0.9f, 0.1f), Props.Wood);
            Props.Box(root, "Stall", new Vector3(1.4f, 0.4f, -0.2f), new Vector3(0.9f, 0.8f, 0.6f), Props.WoodLight);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0.5f, 1f, 0); col.size = new Vector3(2.6f, 2f, 1f);
        }

        public override void OnEnter() { base.OnEnter(); Sfx.Play(Sfx.Cue.Wood); }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Bolt || k == RackView.Kind.Garment || k == RackView.Kind.Dye,
                pickBoltId ?? pickGarmentId,
                (k, id) => { if (k == RackView.Kind.Bolt) { pickBoltId = id; pickGarmentId = null; } else if (k == RackView.Kind.Garment) { pickGarmentId = id; pickBoltId = null; } Refresh(); },
                (k, id) => k != RackView.Kind.Dye));
            var m = Plaque("委托牌与市集", "市集");
            var lines = new List<string>();
            foreach (var q in S.quests)
            {
                if (q.tutorial) lines.Add((q.done ? "✓ " : "· ") + TutorialText(q.id));
                else if (!q.expired && !q.done)
                    lines.Add("· 委托：" + Names.Variety(C, q.needVariety) + "襦裙，" + Names.Dye(C, q.needDye) + "，至少" + Names.Tier(q.needMinTier)
                              + "（第 " + (q.expireDay + 1) + " 日前" + (q.characterId != null ? "，" + Names.Character(C, q.characterId) : "") + "）");
            }
            m.body = string.Join("\n", lines);
            foreach (var q in S.quests)
            {
                if (q.tutorial || q.done || q.expired || pickGarmentId == null) continue;
                var quest = q;
                if (Quests.Matches(S, q, Play.Find.Garment(S, pickGarmentId)))
                    m.secondary.Add(new KeyValuePair<string, System.Action>("交付这件成衣", () => { if (Run((s, c) => Quests.Deliver(s, c, quest.id, pickGarmentId), "委托完成")) { pickGarmentId = null; Refresh(); } }));
            }
            foreach (var dye in Unlocks.OpenDyes(S, C))
            {
                var d = dye;
                m.secondary.Add(new KeyValuePair<string, System.Action>("买一份干" + Names.Dye(C, d) + "（" + C.balance.economy.dryDyePrice + " 丝钱）",
                    () => { Run((s, c) => Market.BuyDryDye(s, c, d), "买到了"); Refresh(); }));
            }
            if (pickBoltId != null)
                m.secondary.Add(new KeyValuePair<string, System.Action>("出售这匹普通布", () => { if (Run((s, c) => Market.SellBolt(s, c, pickBoltId), "卖出了")) { pickBoltId = null; Refresh(); } }));
            m.primaryLabel = "结束今天";
            m.onPrimary = () =>
            {
                if (Run(Day.End, "新的一天：" + Names.Day(S.dayIndex + 1)))
                {
                    G.WriteNow();
                    Workshop.RefreshChrome();
                    Refresh();
                }
            };
            Show(m);
        }

        static string TutorialText(string id)
        {
            switch (id)
            {
                case Craft.Tutorial1: return "教学一：确认库存或织一匹绢";
                case Craft.Tutorial2: return "教学二：把布 A 染靛蓝，做成襦裙";
                case Craft.Tutorial3: return "教学三：西施穿上，完成 0 档，放入展柜";
                default: return id;
            }
        }
    }
}
