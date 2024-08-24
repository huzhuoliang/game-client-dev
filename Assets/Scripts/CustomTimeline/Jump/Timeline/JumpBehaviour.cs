using System;
using UnityEngine;
using UnityEngine.Playables;

namespace CustomTimeline.Jump.Timeline {
    [Serializable]
    public class JumpBehaviour : PlayableBehaviour {
        [NonSerialized]
        public double Start;

        [NonSerialized]
        public double End;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            base.ProcessFrame(playable, info, playerData);
        }
    }
}
