using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Login {
    public class LoginStateMachineMono : MonoBehaviour {
        [ShowInInspector]
        [HideReferenceObjectPicker]
        [HideLabel]
        public readonly LoginStateMachine StateMachine = new();
    }
}
