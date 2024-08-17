using UnityEngine;
using UnityEngine.Playables;

namespace Timeline.Samples {
    // A track mixer behaviour that modifies the timeScale. This affects how fast the game plays back
    public class TimeDilationMixerBehaviour : PlayableBehaviour {
        private float _defaultTimeScale = 1f;

        // Called every frame that the timeline is Evaluated.
        public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
            int inputCount = playable.GetInputCount();
            float timeScale = 0f;
            float totalWeight = 0f;

            // blend clips together
            for (int i = 0; i < inputCount; i++) {
                float inputWeight = playable.GetInputWeight(i);

                ScriptPlayable<TimeDilationBehaviour> playableInput = (ScriptPlayable<TimeDilationBehaviour>)playable.GetInput(i);
                TimeDilationBehaviour input = playableInput.GetBehaviour();

                timeScale += inputWeight * input.timeScale;
                totalWeight += inputWeight;
            }

            // blend to/from the default timeline
            //Time.timeScale = Mathf.Max(0.0001f, Mathf.Lerp(_defaultTimeScale, timeScale, Mathf.Clamp01(totalWeight)));
            double currTimeScale = Mathf.Max(0.0001f, Mathf.Lerp(_defaultTimeScale, timeScale, Mathf.Clamp01(totalWeight)));
            // playable.SetSpeed(currTimeScale);
            // PlayableHandle playableHandle = playable.GetHandle();
            // Playable playable2 = playableDirector.playableGraph.GetRootPlayable(0);
            playable.GetGraph().GetRootPlayable(0).SetSpeed(currTimeScale);
            // Debug.LogError($"\t [DEBUG]\t timeScale={playable.GetSpeed()}");
        }

        // Called when the playable graph is created, typically when the timeline is played.
        public override void OnPlayableCreate(Playable playable) {
            double speed = playable.GetSpeed();
            _defaultTimeScale = (float)speed;
            Debug.LogError($"\t [DEBUG]\t OnPlayableCreate={_defaultTimeScale}");
        }

        // Called when the playable is destroyed, typically when the timeline stops.
        public override void OnPlayableDestroy(Playable playable) {
            // Time.timeScale = _defaultTimeScale;
            playable.SetSpeed(_defaultTimeScale);
            Debug.LogError($"\t [DEBUG]\t OnPlayableDestroy={_defaultTimeScale}");
        }
    }
}
