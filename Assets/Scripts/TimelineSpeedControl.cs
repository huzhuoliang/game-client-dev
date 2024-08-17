using UnityEngine;
using UnityEngine.Playables;

namespace DefaultNamespace {
    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineSpeedControl : MonoBehaviour {

        private PlayableDirector _playableDirector;

        public float speed = 1f;

        private void Awake() {
            _playableDirector = GetComponent<PlayableDirector>();
            _playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }
    }
}
