using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DefaultNamespace.Example {
    public class PlayQueueSample : MonoBehaviour {
        [SerializeField]
        private Animator animator;

        public AnimationClip[] clipsToPlay;

        private PlayableGraph _playableGraph;

        private void Start() {
            _playableGraph = PlayableGraph.Create();
            var playQueuePlayable = ScriptPlayable<PlayQueuePlayable>.Create(_playableGraph);
            var playQueue = playQueuePlayable.GetBehaviour();
            playQueue.Initialize(clipsToPlay, playQueuePlayable, _playableGraph);
            var playableOutput = AnimationPlayableOutput.Create(_playableGraph, "Animation", animator);
            playableOutput.SetSourcePlayable(playQueuePlayable, 0);
            _playableGraph.Play();
        }

        private void OnDisable() {
            // Destroys all Playables and Outputs created by the graph.
            _playableGraph.Destroy();
        }
    }

    public class PlayQueuePlayable : PlayableBehaviour {
        private int _currentClipIndex = -1;

        private float _timeToNextClip;

        private Playable _mixer;

        public void Initialize(AnimationClip[] clipsToPlay, Playable owner, PlayableGraph graph) {
            owner.SetInputCount(1);
            _mixer = AnimationMixerPlayable.Create(graph, clipsToPlay.Length);
            graph.Connect(_mixer, 0, owner, 0);
            owner.SetInputWeight(0, 1);
            for (int clipIndex = 0; clipIndex < _mixer.GetInputCount(); ++clipIndex) {
                AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, clipsToPlay[clipIndex]);
                graph.Connect(clipPlayable, 0, _mixer, clipIndex);
                _mixer.SetInputWeight(clipIndex, 1.0f);
            }
        }

        public override void PrepareFrame(Playable owner, FrameData info) {
            if (_mixer.GetInputCount() == 0) {
                return;
            }

            // Advance to next clip if necessary
            _timeToNextClip -= info.deltaTime;
            if (_timeToNextClip <= 0.0f) {
                _currentClipIndex++;
                if (_currentClipIndex >= _mixer.GetInputCount()) {
                    _currentClipIndex = 0;
                }

                AnimationClipPlayable currentClip = (AnimationClipPlayable)_mixer.GetInput(_currentClipIndex);
                // Reset the time so that the next clip starts at the correct position
                currentClip.SetTime(0);
                _timeToNextClip = currentClip.GetAnimationClip().length;
            }

            // Adjust the weight of the inputs
            for (int clipIndex = 0; clipIndex < _mixer.GetInputCount(); ++clipIndex) {
                if (clipIndex == _currentClipIndex) {
                    _mixer.SetInputWeight(clipIndex, 1.0f);
                } else {
                    _mixer.SetInputWeight(clipIndex, 0.0f);
                }
            }
        }
    }
}
