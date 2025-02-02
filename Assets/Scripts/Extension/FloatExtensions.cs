using System;

public static class FloatExtensions {
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };


    public static string FormatByte(this ulong byteCount) {
        if (byteCount == 0) {
            return "0B";
        }

        int sizeIndex = (int)Math.Floor(Math.Log(byteCount, 1024));
        double sizeValue = byteCount / Math.Pow(1024, sizeIndex);
        return $"{sizeValue:0.##}{SizeSuffixes[sizeIndex]}";
    }

    public static string FormatByte(this long byteCount) {
        bool negative = byteCount < 0;
        if (negative) {
            byteCount = -byteCount;
        }

        if (byteCount == 0) {
            return "0B";
        }

        int sizeIndex = (int)Math.Floor(Math.Log(byteCount, 1024));
        double sizeValue = byteCount / Math.Pow(1024, sizeIndex);
        string symbol = negative ? "-" : "";
        return $"{symbol}{sizeValue:0.##}{SizeSuffixes[sizeIndex]}";
    }
}
