using System.Linq;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.def;

public class RenderLinkComp : ThingComp
{
    private const float LineWidth = 0.1f;
    private const float CircleRadius = 0.8f;

    public override void PostDrawExtraSelectionOverlays()
    {
        if (parent is Building_StorageUnitIOBase { NoConnectionAlert: false } iobase)
        {
            GenDraw.DrawCircleOutline(iobase.TrueCenter(), CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawCircleOutline(iobase.boundStorageUnit.TrueCenter(), CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawLineBetween(iobase.TrueCenter(), iobase.boundStorageUnit.TrueCenter(), SimpleColor.Yellow, LineWidth);
        }

        if (parent is DigitalStorageUnitBuilding { Powered: true } dsu)
        {
            var linkedPorts = dsu.Ports.Where(p => p.NoConnectionAlert).ToList();
            if (linkedPorts.Any())
            {
                GenDraw.DrawCircleOutline(dsu.TrueCenter(), CircleRadius, SimpleColor.Yellow);
                foreach (var port in linkedPorts)
                {
                    GenDraw.DrawCircleOutline(port.TrueCenter(), CircleRadius, SimpleColor.Yellow);
                    GenDraw.DrawLineBetween(dsu.TrueCenter(), port.TrueCenter(), SimpleColor.Yellow, LineWidth);
                }
            }
        }
    }
}