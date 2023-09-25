using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnassignedField.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ModExtensionPortColor : DefModExtension
{
    public Color inColor;
    public Color outColor;
}