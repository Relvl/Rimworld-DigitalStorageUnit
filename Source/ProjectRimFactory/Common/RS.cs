using UnityEngine;
using Verse;

namespace ProjectRimFactory;

[StaticConstructorOnStartup]
public static class RS
{
    static RS()
    {
        ForbidOn = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn", true);
        ForbidOff = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff", true);
        Arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow", true);
    }

    public static readonly Texture2D ForbidOn;
    public static readonly Texture2D ForbidOff;

    public static readonly Texture2D Arrow;
}