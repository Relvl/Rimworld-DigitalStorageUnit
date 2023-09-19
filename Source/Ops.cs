using UnityEngine;

namespace DigitalStorageUnit.AutoMachineTool;

public static class Ops
{
    public static Color A(this Color color, float a)
    {
        var c = color;
        c.a = a;
        return c;
    }
}