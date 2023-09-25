using Verse;

namespace DigitalStorageUnit.ui;

public class RenameDsuDialog : Dialog_Rename
{
    private readonly DigitalStorageUnitBuilding _building;

    public RenameDsuDialog(DigitalStorageUnitBuilding building)
    {
        _building = building;
        curName = building.UniqueName ?? building.LabelNoCount;
    }

    protected override void SetName(string name)
    {
        _building.UniqueName = curName;
    }
}