using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace DigitalStorageUnit.Common;

public class ModExtension_Graphic : DefModExtension
{
    [XmlInheritanceAllowDuplicateNodes] List<GraphicDataListItem> graphicDataList = new();

    public IEnumerable<Graphic> Graphics => graphicDataList.Select(i => i.Graphic);

    public Graphic FirstGraphic => Graphics.FirstOrDefault();

    private Dictionary<string, int> getByNameCache = new();

    public Graphic GetByName(string name)
    {
        int index;
        if (name is null) return null;
        if (!getByNameCache.TryGetValue(name, out index))
        {
            var value = graphicDataList.Where(g => g.name == name).FirstOrDefault();
            index = graphicDataList.IndexOf(value);
            getByNameCache.Add(name, index);
        }

        return graphicDataList[index].Graphic;
    }
}

public class GraphicDataListItem
{
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        name = xmlRoot.Name;
        graphicData = DirectXmlToObject.ObjectFromXml<GraphicData>(xmlRoot.FirstChild, false);
    }

    public string name;

    public GraphicData graphicData;

    public Graphic Graphic => graphicData?.Graphic ?? null;
}