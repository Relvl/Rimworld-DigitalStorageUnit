using Verse;

namespace DigitalStorageUnit.ui;

public class RenameDsuDialog : Dialog_Rename<DigitalStorageUnitBuilding>
{
    public RenameDsuDialog(DigitalStorageUnitBuilding building) : base(building)
    {
        curName = building.RenamableLabel;
    }
}