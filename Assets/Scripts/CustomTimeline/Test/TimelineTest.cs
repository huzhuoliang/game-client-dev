using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace DefaultNamespace.Example {
    public class TimelineTest : MonoBehaviour {
        [Button]
        private void Test() {
            PlayableGraph playableGraph = PlayableGraph.Create();
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        }
    }
}
