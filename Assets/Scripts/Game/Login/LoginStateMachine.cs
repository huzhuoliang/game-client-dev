using System.Collections.Generic;
using Game.StateMachine;
using System;
using System.Collections;
using Game.UI;
using Net.Mono;
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
        public IReadOnlyDictionary<Type, LoginStateBase> States => _states;

        public UIManager UIManager { get; private set; }
        public GameClient GameClient { get; private set; }

        public LoginStateMachine Init() {
            List<Type> subTypes = GetAllSubClass(typeof(LoginStateBase));
            foreach (Type type in subTypes) {
                LoginStateBase state = (LoginStateBase)Activator.CreateInstance(type, this);
                _states.TryAdd(type, state);
            }

            return this;
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
            yield return _stateMachineController.Start(new AppStartState(this));
        }

        public LoginStateMachine SetUIManager(UIManager uiManager) {
            UIManager = uiManager;
            return this;
        }

        public LoginStateMachine SetGameClient(GameClient gameClient) {
            GameClient = gameClient;
            return this;
        }
    }
}
