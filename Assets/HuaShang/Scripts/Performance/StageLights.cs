using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    /// <summary>戏台的主光与边缘光。0 档一盏柔光；30 档主光加一条边缘光（docs/08 §2）。</summary>
    public class StageLights : MonoBehaviour
    {
        public Light key;
        public Light rim;
    }
}
