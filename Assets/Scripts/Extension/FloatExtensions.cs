using System;

public static class FloatExtensions {
    private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };


    public static string FormatByte(this ulong byteCount) {
        if (byteCount == 0) {
            return "0 bytes";
        }

        int sizeIndex = (int)Math.Floor(Math.Log(byteCount, 1024));
        double sizeValue = byteCount / Math.Pow(1024, sizeIndex);
        return $"{sizeValue:0.##} {SizeSuffixes[sizeIndex]}";
    }
}
