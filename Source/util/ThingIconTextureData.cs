using UnityEngine;

namespace DigitalStorageUnit.util;

public struct ThingIconTextureData
{
    public readonly Texture Texture;
    public readonly Color Color;

    public ThingIconTextureData(Texture texture, Color color)
    {
        Texture = texture;
        Color = color;
    }
}