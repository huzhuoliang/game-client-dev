using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Net.Proto.State {
    public sealed class Init : TcpClientStateBase {
        protected override UniTask RunAsyncInternal(ITcpClientFSMCtx ctx, CancellationToken ct = default) {
            RegisterAll();
            SetNext<Connecting>();
            ct.ThrowIfCancellationRequested();
            return UniTask.CompletedTask;
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
                    // Debug.Log($"[AutoRegister] Registered {type.Name}");
                }
            }
        }
    }
}
