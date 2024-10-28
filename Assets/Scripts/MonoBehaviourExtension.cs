using System;
using System.Collections;
using UnityEngine;

namespace Game {
    public static class MonoBehaviourExtension {
        public static Coroutine<T> StartCoroutine<T>(this MonoBehaviour obj, IEnumerator coroutine) {
            Coroutine<T> coroutineObject = new();
            coroutineObject.unityCoroutine = obj.StartCoroutine(coroutineObject.InternalRoutine(coroutine));
            return coroutineObject;
        }
    }

    public class Coroutine<T> {
        public T Result {
            get {
                if (_exception != null) {
                    throw _exception;
                }

                return _result;
            }
        }

        private T _result;

        private Exception _exception;

        // ReSharper disable once InconsistentNaming
        public Coroutine unityCoroutine;

        public IEnumerator InternalRoutine(IEnumerator coroutine) {
            while (true) {
                try {
                    if (!coroutine.MoveNext()) {
                        yield break;
                    }
                } catch (Exception e) {
                    _exception = e;
                    yield break;
                }

                object yielded = coroutine.Current;
                if (yielded != null && yielded.GetType() == typeof(T)) {
                    _result = (T)yielded;
                    yield break;
                } else {
                    yield return coroutine.Current;
                }
            }
        }
    }
}
