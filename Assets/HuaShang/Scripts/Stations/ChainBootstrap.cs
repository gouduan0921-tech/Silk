using System.Collections.Generic;
using UnityEngine;
using HuaShang.Performance;

namespace HuaShang.Stations
{
    /// <summary>
    /// 首发是一条工位链，不是七张开放地图（docs/00 §6、docs/02 §1）。
    /// 灰盒：建筑退后，布与器物先被看见（docs/14）。正式美术交付后逐个替换道具。
    /// </summary>
    public class ChainBootstrap : MonoBehaviour
    {
        public List<TimelineEntry> timelines = new List<TimelineEntry>();

        void Start()
        {
            var rig = ChainRig.Build(transform);
            var stage = rig.stations.Find(s => s is StageStation) as StageStation;
            if (stage != null) stage.performance.timelines = timelines;
        }
    }
}
