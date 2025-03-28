using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public enum ResearchType
{
    None,
    Agriculture,
    Sailing,
    Pottery,
    AnimalHusbandry,
    Irrigation,
    Writing,
    Masonry,
    Wheel,
    BronzeWorking,
    Archery,
    Currency,
}

public struct ResearchInfo
{
    public float Cost;
    public int Tier;
    public List<BuildingType> BuildingUnlocks;
    public List<UnitType> UnitUnlocks;
    public List<String> Effects;
}

public static class ResearchLoader
{

    public static Dictionary<ResearchType, string> researchNames = new Dictionary<ResearchType, string>
    {
        { ResearchType.Agriculture, "Agriculture" },
        { ResearchType.Sailing, "Sailing" },
        { ResearchType.Pottery, "Pottery"},
        { ResearchType.AnimalHusbandry, "Animal Husbandry" },
        { ResearchType.Irrigation, "Irrigation" },
        { ResearchType.Writing, "Writing" },
        { ResearchType.Masonry, "Masonry" },
        { ResearchType.Wheel, "Wheel" },
        { ResearchType.BronzeWorking, "Bronze Working"},
        { ResearchType.Archery, "Archery" },
        { ResearchType.Currency, "Currency" },
    };
    public static Dictionary<ResearchType, ResearchInfo> researchsDict;
    
    static ResearchLoader()
    {
        string xmlPath = "Researches.xml";
        researchsDict = LoadResearchData(xmlPath);
    }
    
    public static Dictionary<ResearchType, ResearchInfo> LoadResearchData(string xmlPath)
    {
        XDocument xmlDoc = XDocument.Load(xmlPath);
        var ResearchData = xmlDoc.Descendants("Research")
            .ToDictionary(
                r => Enum.Parse<ResearchType>(r.Attribute("Name").Value),
                r => new ResearchInfo
                {
                    Cost = float.Parse(r.Attribute("Cost").Value),
                    Tier = int.Parse(r.Attribute("Tier").Value),
                    BuildingUnlocks = r.Element("BuildingUnlocks").Elements("BuildingType").Select(e => Enum.Parse<BuildingType>(e.Value)).ToList(),
                    UnitUnlocks = r.Element("UnitUnlocks").Elements("UnitUnlocks").Select(e => Enum.Parse<UnitType>(e.Value)).ToList(),
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList(),
                }
            );
        return ResearchData;
    }
}
