using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Tcp.State {
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Init : TcpClientStateBase {
        private Init() { }

        protected override UniTask<TcpClientStateBase> RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            RegisterAll();
            ct.ThrowIfCancellationRequested();
            return UniTask.FromResult<TcpClientStateBase>(GetInstance<Connecting>());
        }

        private static void RegisterAll() {
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
