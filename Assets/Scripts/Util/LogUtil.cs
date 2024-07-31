using System.Diagnostics;
public static class LogUtil {

    [Conditional("ENABLE_LOG")]
    public static void Log(object message) {
        UnityEngine.Debug.Log(message);
    }
}