using UnityEngine;

namespace DigitalStorageUnit.util;

public static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float a)
    {
        var c = color;
        c.a = a;
        return c;
    }
}