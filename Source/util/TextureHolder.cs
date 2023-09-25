using UnityEngine;
using Verse;

namespace DigitalStorageUnit.util;

[StaticConstructorOnStartup]
public static class TextureHolder
{
    public static readonly Texture2D StoragePawnAccessSwitchIcon = ContentFinder<Texture2D>.Get("UI/dsu");
    public static readonly Texture2D CargoPlatform = ContentFinder<Texture2D>.Get("Storage/CargoPlatform");
    public static readonly Texture2D IoIcon = ContentFinder<Texture2D>.Get("UI/IoIcon");

    public static readonly Texture2D ForbidOn = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOn");
    public static readonly Texture2D ForbidOff = ContentFinder<Texture2D>.Get("UI/Designators/ForbidOff");
    public static readonly Texture2D Arrow = ContentFinder<Texture2D>.Get("UI/Overlays/Arrow");
    public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");
    public static readonly Texture2D SetTargetFuelLevel = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel");

    // public static readonly Material MatLostLink = MaterialPool.MatFrom("UI/danger", ShaderDatabase.MetaOverlay);
    public static readonly Material MatLostLink = MaterialPool.MatFrom("UI/too-far-from-roboport-icon", ShaderDatabase.MetaOverlay);

    public static readonly Color Yellow25 = Color.yellow.WithAlpha(0.25f);
}