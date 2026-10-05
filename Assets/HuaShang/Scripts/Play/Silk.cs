using System;
using System.Collections.Generic;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>
    /// 蚕箔与缫丝（docs/06、docs/04 §3）。四步：收蚁、喂叶、控温、上蔟，相邻两步至少隔 1 个工坊日。
    /// 养蚕的日结发生在工坊日结束，是首发唯一的离屏结算（docs/19 §3）。
    /// </summary>
    public static class Silk
    {
        /// <summary>当季蚕种品质基数（docs/04 §3）。</summary>
        static int BaseScore(SaveRoot s, ConfigSnapshot c) => Seasons.Current(s, c)?.cocoonBase ?? c.balance.season.springBase;

        static Result StepCheck(SaveRoot s, ConfigSnapshot c, string expectedStage)
        {
            if (s.tray.stage != expectedStage) return Result.Fail("蚕箔现在不能做这一步");
            if (s.tray.lastStepDay.HasValue && s.dayIndex - s.tray.lastStepDay.Value < 1) return Result.Fail("这一步要隔一个工坊日再做");
            if (!Progress.CanSpend(s, c, c.balance.day.HoursOf("feed"))) return Result.Fail("今天的工时不够");
            return null;
        }

        static void Step(SaveRoot s, ConfigSnapshot c, string next)
        {
            int h = c.balance.day.HoursOf("feed");
            Progress.Spend(s, h);
            Progress.AwardProcessXp(s, c, h, false);
            s.tray.stage = next;
            s.tray.lastStepDay = s.dayIndex;
        }

        /// <summary>收蚁：耗蚕种（docs/04 §3），没有蚕种库存时按 docs/04 §7 的价格从丝钱买。</summary>
        public static Result Hatch(SaveRoot s, ConfigSnapshot c)
        {
            if (s.tray.stage != TrayStage.Empty) return Result.Fail("蚕箔上已经有一批蚕");
            var season = Seasons.Current(s, c);
            if (season != null && !season.CanHatch) return Result.Fail(season.label + "天不结新茧，不能收蚁");
            if (!Progress.CanSpend(s, c, c.balance.day.HoursOf("feed"))) return Result.Fail("今天的工时不够");
            int price = c.balance.economy.seedPrice * c.balance.season.seedPerTray;
            if (s.silkCoin < price) return Result.Fail("丝钱不够买蚕种（" + price + "）");
            s.silkCoin -= price;
            int b = BaseScore(s, c);
            s.tray = new Tray { stage = TrayStage.Hatched, startDay = s.dayIndex, shellScore = b, uniformity = b, shapeScore = b };
            int h = c.balance.day.HoursOf("feed");
            Progress.Spend(s, h);
            Progress.AwardProcessXp(s, c, h, false);
            s.tray.lastStepDay = s.dayIndex;
            return Result.Ok();
        }

        public static Result Feed(SaveRoot s, ConfigSnapshot c, bool overfeed)
        {
            var fail = StepCheck(s, c, TrayStage.Hatched);
            if (fail != null) return fail;
            s.tray.uniformity += overfeed ? c.balance.season.overfeedPenalty : c.balance.season.feedGoodUniformity;
            Step(s, c, TrayStage.Fed);
            return Result.Ok();
        }

        /// <summary>控温：当天在标记带内不扣；离带按热损扣茧层（春为 0，docs/04 §3）。</summary>
        public static Result Temper(SaveRoot s, ConfigSnapshot c, bool inBand)
        {
            var fail = StepCheck(s, c, TrayStage.Fed);
            if (fail != null) return fail;
            if (!inBand) s.tray.shellScore += SeasonHeatLoss(s, c);
            Step(s, c, TrayStage.Tempered);
            return Result.Ok();
        }

        static int SeasonHeatLoss(SaveRoot s, ConfigSnapshot c) => Seasons.Current(s, c)?.heatPerDay ?? 0;

        /// <summary>上蔟：得茧篮一只（docs/04 §3）。</summary>
        public static Result Mount(SaveRoot s, ConfigSnapshot c, bool crowded)
        {
            var fail = StepCheck(s, c, TrayStage.Tempered);
            if (fail != null) return fail;
            if (crowded) s.tray.shapeScore += c.balance.season.crowdingShapePenalty;
            Step(s, c, TrayStage.Mounted);
            var t = s.tray;
            s.cocoons.Add(new CocoonBasket
            {
                id = Ids.Next(s, "cocoon"),
                shellScore = Clamp(t.shellScore ?? 0),
                uniformity = Clamp(t.uniformity ?? 0),
                shapeScore = Clamp(t.shapeScore ?? 0),
                batch = c.balance.season.trayBatch,
                defect = t.sick,
            });
            s.tray = new Tray { stage = TrayStage.Empty };
            return Result.Ok(s.cocoons[s.cocoons.Count - 1].id);
        }

        static int Clamp(int v) => Math.Max(0, Math.Min(100, v));

        /// <summary>茧篮原料分 = 三项平均（docs/04 §3）。</summary>
        public static int MaterialScore(CocoonBasket b) => (int)Math.Round((b.shellScore + b.uniformity + b.shapeScore) / 3.0, MidpointRounding.AwayFromZero);

        /// <summary>缫丝一束：按拍抽丝，乱拍记一次断头（docs/04 §3）。</summary>
        public static Result Reel(SaveRoot s, ConfigSnapshot c, string basketId, string fineness, IList<Beat> beats)
        {
            var basket = s.cocoons.Find(x => x.id == basketId);
            if (basket == null || basket.batch <= 0) return Result.Fail("没有可缫的茧");
            if (fineness != "fine" && fineness != "medium" && fineness != "coarse") return Result.Fail("细度只有细、中、粗");
            if (beats == null || beats.Count != c.balance.season.reelBeats) return Result.Fail("缫一束需要 " + c.balance.season.reelBeats + " 拍");
            int hours = c.balance.day.HoursOf("reel");
            if (!Progress.CanSpend(s, c, hours)) return Result.Fail("今天的工时不够缫一束（需要 " + hours + "）");
            int breaks = 0;
            foreach (var b in beats) if (b == Beat.Chaos) breaks++;
            var yarn = new Yarn
            {
                id = Ids.Next(s, "yarn"),
                fiber = "jiaCan",
                fineness = fineness,
                materialScore = MaterialScore(basket),
                processScore = Math.Max(0, 100 + breaks * c.balance.penalties.reelBreak),
                joints = breaks,
                length = c.balance.day.reelYieldMeters,
            };
            if (basket.defect) yarn.processScore = Math.Min(yarn.processScore.Value, (int)c.balance.weave.defectLine);
            basket.batch -= 1;
            if (basket.batch <= 0) s.cocoons.Remove(basket);
            s.yarns.Add(yarn);
            Progress.Spend(s, hours);
            Progress.AwardProcessXp(s, c, hours, basket.defect);
            return Result.Ok(yarn.id);
        }
    }
}
