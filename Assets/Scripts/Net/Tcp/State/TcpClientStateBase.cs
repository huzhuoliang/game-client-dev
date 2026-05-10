using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Net.Tcp.State {
    public abstract class TcpClientStateBase {
        /// <summary>
        /// 派生状态的 ctor 应当声明为 <c>private</c>，仅允许通过 <see cref="GetInstance{T}"/> 获取实例。
        /// 这里显式写出 protected ctor 是为了让"派生类不能 public ctor"这条约束在阅读时一目了然。
        /// </summary>
        protected TcpClientStateBase() { }

        private static readonly Dictionary<Type, TcpClientStateBase> instanceDic = new();

        /// <summary>
        /// 获取（或惰性创建）状态单例。状态对象不持有任何与状态机实例相关的可变字段，
        /// 因此可以跨状态机共享。
        /// </summary>
        public static T GetInstance<T>() where T : TcpClientStateBase {
            Type t = typeof(T);
            if (!instanceDic.TryGetValue(t, out TcpClientStateBase instance)) {
                instance = (T)Activator.CreateInstance(t, nonPublic: true);
                instanceDic[t] = instance;
            }
            return (T)instance;
        }

        public UniTask<TcpClientStateBase> RunAsync(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            return RunAsyncInternal(ctx, ct);
        }

        /// <summary>
        /// 状态主体逻辑。返回值即下一个要执行的状态实例（用 <see cref="GetInstance{T}"/> 取），
        /// 返回 null 表示终止状态机。状态对象不维护任何转移用的可变字段。
        /// </summary>
        protected abstract UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default);

        /// <summary>
        /// 进入状态时由状态机调用。base 实现负责打印进入日志
        /// </summary>
        public void OnEnterWrap(ITcpClientFSMCtx ctx) {
            Debug.LogFormat("[TcpClientStateBase] Enter state \"{0}\"", GetType().Name);
            OnEnter(ctx);
        }

        public void OnExitWrap(ITcpClientFSMCtx ctx) {
            Debug.LogFormat("[TcpClientStateBase] Exit state \"{0}\"", GetType().Name);
            OnExit(ctx);
        }

        protected virtual void OnEnter(ITcpClientFSMCtx ctx) { }


        protected virtual void OnExit(ITcpClientFSMCtx ctx) { }
    }
}
