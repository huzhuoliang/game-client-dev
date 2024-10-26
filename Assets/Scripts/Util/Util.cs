using UnityEngine;

namespace Game.Util {
    public class Util {
        public static void OpenFolder(string path) {
            if (string.IsNullOrEmpty(path))
                return;
            path = path.Replace("/", "\\");
            if (!System.IO.Directory.Exists(path)) {
                Debug.LogError($"No Directory: \"{path}\"");
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }
}
