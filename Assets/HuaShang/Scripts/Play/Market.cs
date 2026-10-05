using System;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>市集（docs/02 §1、docs/04 §7）。不能用钱买成品锦、成衣或好感（docs/25 §2）。</summary>
    public static class Market
    {
        /// <summary>出售普通布：每米定价；精良和传世不可售（N11）。</summary>
        public static Result SellBolt(SaveRoot s, ConfigSnapshot c, string boltId)
        {
            var b = Find.Bolt(s, boltId);
            if (b == null) return Result.Fail("没有这匹布");
            var q = ItemQuality.BoltQ(b, c);
            if (!q.HasValue) return Result.Fail("这匹布分数不全，不能定价");
            string tier = QualityCalc.TierOf(q.Value, c.balance.quality);
            if (tier != QualityCalc.Common) return Result.Fail("精良和传世的布不可售");
            int coins = (int)Math.Floor(b.length * c.balance.economy.sellCommonPerMeter);
            s.silkCoin += coins;
            s.bolts.Remove(b);
            return Result.Ok();
        }

        public static Result BuyDryDye(SaveRoot s, ConfigSnapshot c, string dyeId)
        {
            var row = c.dyes.Find(x => x.id == dyeId);
            if (row == null || !row.launch || !Unlocks.DyeOpen(s, c, dyeId)) return Result.Fail("市集没有这种染料");
            int price = c.balance.economy.dryDyePrice;
            if (s.silkCoin < price) return Result.Fail("丝钱不够");
            s.silkCoin -= price;
            Add(s, dyeId, 1);
            return Result.Ok();
        }

        public static void Add(SaveRoot s, string dyeId, int count)
        {
            var stock = Find.Dye(s, dyeId);
            if (stock == null) s.dyes.Add(new DyeStock { dyeId = dyeId, count = count, state = "dry" });
            else stock.count += count;
        }
    }
}
