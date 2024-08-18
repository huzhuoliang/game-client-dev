using System;
using UnityEngine.Playables;

namespace CustomTimeline.TextNumberRoll {
    [Serializable]
    public class TextNumberRollBehaviour : PlayableBehaviour {
        public int numberFrom;
        public int numberTo;
        [NonSerialized]
        public double Start;
        [NonSerialized]
        public double End;
    }
}
