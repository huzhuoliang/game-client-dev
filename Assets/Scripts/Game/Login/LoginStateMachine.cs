using System.Collections.Generic;
using UnityEngine;
using Game.StateMachine;
using System;
using Game.UI;
using Net.Mono;

namespace Game.Login {
    public class LoginStateMachine : MonoBehaviour {
        private readonly StateMachineController _stateMachineController = new();

        private readonly Dictionary<Type, LoginStateBase> _states = new();
        public IReadOnlyDictionary<Type, LoginStateBase> States => _states;

        public UIManager UIManager { get; private set; }
        public GameClient GameClient { get; private set; }

        private void Start() {
            List<Type> subTypes = GetAllSubClass(typeof(LoginStateBase));
            foreach (Type type in subTypes) {
                LoginStateBase state = (LoginStateBase)Activator.CreateInstance(type, new object[] { this });
                _states.TryAdd(type, state);
            }
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

        public void StartStateMachine() {
            StartCoroutine(_stateMachineController.Start(new AppStartState(this)));
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
