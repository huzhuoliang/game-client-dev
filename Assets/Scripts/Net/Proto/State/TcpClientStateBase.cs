using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Net.Proto.State {
    public abstract class TcpClientStateBase {
        public TcpClientStateBase NextState { get; private set; }

        private static readonly Dictionary<Type, TcpClientStateBase> instanceDic = new();

        protected void SetNext<TNext>() where TNext : TcpClientStateBase {
            NextState = GetInstance<TNext>();
        }

        private static TTarget GetInstance<TTarget>() where TTarget : TcpClientStateBase {
            Type t = typeof(TTarget);
            if (!instanceDic.TryGetValue(t, out TcpClientStateBase instance)) {
                instance = (TTarget)Activator.CreateInstance(t, nonPublic: true);
                instanceDic[t] = instance;
            }
            return (TTarget)instance;
        }

        public UniTask RunAsync(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            Debug.LogErrorFormat("============ RunAsync \"{0}\"", GetType());
            return RunAsyncInternal(ctx, ct);
        }

        protected abstract UniTask RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default);

        public virtual void OnEnter(ITcpClientFSMCtx ctx) { }

        public virtual void OnExit(ITcpClientFSMCtx ctx) { }
    }
}
