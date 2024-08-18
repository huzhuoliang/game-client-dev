using UnityEngine;
using UnityEngine.Playables;

namespace CustomTimeline.TextNumberRoll {
    public class TextNumberRollMixerBehaviour : PlayableBehaviour {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            if (playerData is not TextNumberRoll textNumberRoll) {
                return;
            }

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++) {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f)
                    continue;
                var scriptPlayable = (ScriptPlayable<TextNumberRollBehaviour>)playable.GetInput(i);
                TextNumberRollBehaviour behaviour = scriptPlayable.GetBehaviour();
                int numberFrom = behaviour.numberFrom;
                int numberTo = Mathf.Max(behaviour.numberTo, behaviour.numberFrom);
                double duration = behaviour.End - behaviour.Start;
                // double playableTime = playable.GetTime(); // 当 Timeline 循环播放时这个时间会累加
                double playableTime = playable.GetGraph().GetRootPlayable(0).GetTime(); // 这个时间更可靠
                double localTime = playableTime - behaviour.Start;
                float p = (float)(localTime / duration);
                p = Mathf.Clamp(p, 0f, 1f);
                int number = numberFrom + Mathf.RoundToInt((numberTo - numberFrom) * p);
                textNumberRoll.SetString(number);
            }
        }
    }
}
