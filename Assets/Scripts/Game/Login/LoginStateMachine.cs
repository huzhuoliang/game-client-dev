using System.Collections.Generic;
using Game.StateMachine;
using System;
using System.Collections;
using Sirenix.OdinInspector;

namespace Game.Login {
    [Serializable]
    public class LoginStateMachine {
        [ShowInInspector]
        [ReadOnly]
        [HideReferenceObjectPicker]
        [HideLabel]
        private readonly StateMachineController _stateMachineController = new();

        private readonly Dictionary<Type, LoginStateBase> _states = new();

        public LoginStateMachineContext Context { get; private set; }

        public void Init() {
            Context = new LoginStateMachineContext();
            List<Type> subTypes = GetAllSubClass(typeof(LoginStateBase));
            foreach (Type type in subTypes) {
                LoginStateBase state = (LoginStateBase)Activator.CreateInstance(type);
                state.Init(this);
                _states.TryAdd(type, state);
            }
        }

        public LoginStateBase GetState<T>() where T : LoginStateBase {
            return _states.GetValueOrDefault(typeof(T)) as T;
        }

        private static List<Type> GetAllSubClass(Type parentType) {
            List<Type> list = new();
            Type[] allTypes = parentType.Assembly.GetTypes();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Type itemType in allTypes) {
                if (itemType.BaseType == null) {
                    continue;
                }

                if (itemType.BaseType.Name.Equals(parentType.Name)) {
                    list.Add(itemType);
                }
            }

            return list;
        }

        public IEnumerator StartStateMachine() {
            yield return _stateMachineController.Start(GetState<AppStartState>());
        }
    }
}
