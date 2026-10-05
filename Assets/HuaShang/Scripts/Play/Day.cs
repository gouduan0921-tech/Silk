using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>工坊日（docs/02 §2）。首发季节恒为春（docs/02 §2、docs/21 §2）。</summary>
    public static class Day
    {
        /// <summary>结束今天：养蚕离屏日结、委托到期、清工时、日序加一、刷新委托。</summary>
        public static Result End(SaveRoot s, ConfigSnapshot c)
        {
            // 养蚕日结：蚕箔有蚕而今天没做控温以外的照料，按热损扣茧层（春为 0，docs/04 §3）
            var t = s.tray;
            if (t.stage != TrayStage.Empty && t.stage != TrayStage.Mounted && t.lastStepDay != s.dayIndex && s.season == "summer")
                t.shellScore = (t.shellScore ?? 0) + c.balance.season.summerHeatPerDay;
            s.hoursUsed = 0;
            s.dayIndex += 1;
            Quests.Expire(s);
            Quests.Refresh(s, c);
            return Result.Ok();
        }
    }
}
