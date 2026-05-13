using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Init : TcpClientStateBase {
        public override ETcpState Kind => ETcpState.Init;
        private Init() { }

        protected override UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            RegisterAll();
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult<TcpClientStateBase>(GetInstance<Connecting>());
        }

        // MessageHandlerRegistry 是进程级单例；handler 注册是一次性的事，不能跟随 FSM 启停反复跑
        // ——否则 Mono disable/enable 重启 FSM 时会触发"Duplicate MessageType registration" 报错
        private static bool sRegistered = false;

        private static void RegisterAll() {
            if (sRegistered) {
                return;
            }
            sRegistered = true;   // 先置位再注册，防止反射途中抛异常时下次重入重复注册成功的那些
#if UNITY_EDITOR
            RegisterAllByReflection();
#else
            /* TODO 调用自动生成的类来注册 */
            // MessageHandlerRegistry.Instance.Register(new TcpHelloWorldHandler());
#endif
        }

        private static void RegisterAllByReflection() {
            Type handlerInterface = typeof(IMessageHandler<>);

            StringBuilder sb = new StringBuilder();
            sb.Append("AutoRegister: \n");
            int index = 1;
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                if (!type.IsClass || type.IsAbstract) {
                    continue;
                }

                Type[] interfaces = type.GetInterfaces();
                Type interfaceType = null;
                foreach (Type inter in interfaces) {
                    if (inter.IsGenericType && inter.GetGenericTypeDefinition() == handlerInterface) {
                        interfaceType = inter;
                    }
                }

                if (interfaceType != null) {
                    object handlerInstance = Activator.CreateInstance(type);
                    MethodInfo registerMethod = typeof(MessageHandlerRegistry).GetMethod("Register");
                    registerMethod = registerMethod?.MakeGenericMethod(interfaceType.GenericTypeArguments[0]);
                    registerMethod?.Invoke(MessageHandlerRegistry.Instance, new[] { handlerInstance });
                    sb.Append($"{index} {type.Name}\n");
                    index++;
                }
            }
            if (index > 1) {
                UnityEngine.Debug.Log(sb.ToString());
            }
        }
    }
}
