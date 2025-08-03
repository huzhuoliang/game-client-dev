using System;
using System.Collections;
using UnityEngine;

namespace Game.CoroutineExtensions {
    public static class CoroutineExtension {
        public static Coroutine StartCoroutineSafe(this MonoBehaviour mono, IEnumerator routine, Action<Exception> onException = null) {
            return mono.StartCoroutine(WrapCoroutine(routine, onException));
        }

        public static IEnumerator WrapCoroutine(IEnumerator routine, Action<Exception> onException) {
            while (true) {
                object current;
                try {
                    if (!routine.MoveNext()) {
                        yield break;
                    }

                    current = routine.Current;
                } catch (Exception ex) {
                    onException?.Invoke(ex);
                    yield break;
                }

                yield return current;
            }
        }
    }
}
