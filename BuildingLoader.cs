using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public enum BuildingType
{//'City Center' 'Farm' 'Mine' 'Hunting Camp' 'Fishing Boat' 'Whaling Ship'
  CityCenter = "City Center",
  Farm = "Farm",
  Mine = "Mine",
  HuntingCamp = "Hunting Camp",
  FishingBoat = "Fishing Boat",
  WhalingShip = "Whaling Ship",
  Granary = "Granary",
  StoneCutter = "Stone Cutter",
  Market = "Market",
}

public struct BuildingInfo
{
    public int ProductionCost { get; set; }
    public int GoldCost { get; set; }
    public float FoodYield { get; set; }
    public float ProductionYield { get; set; }
    public float GoldYield { get; set; }
    public float ScienceYield { get; set; }
    public float CultureYield { get; set; }
    public float HappinessYield { get; set; }
    public float MaintenanceCost { get; set; }
    public List<String> Effects { get; set; }
}

public static class BuildingLoader
{
    public Dictionary<BuildingType, BuildingInfo> buildingsDict;
    
    public BuildingLoader()
    {
        string xmlPath = "Buildings.xml";
        buildingsDict = LoadBuildingData(xmlPath);
    }
    
    public static Dictionary<BuildingType, BuildingInfo> LoadBuildingData(string xmlPath)
    {
        XDocument xmlDoc = XDocument.Load(xmlPath);
        var BuildingData = xmlDoc.Descendants("Building")
            .ToDictionary(
                r => (BuildingType)Enum.Parse(typeof(BuildingType), r.Attribute("Name").Value),
                r => new BuildingInfo
                {
                    ProductionCost = int.Parse(r.Attribute("ProductionCost").Value),
                    GoldCost = int.Parse(r.Attribute("GoldCost").Value),
                    FoodYield = int.Parse(r.Attribute("FoodYield").Value),
                    ProductionYield = int.Parse(r.Attribute("ProductionYield").Value),
                    GoldYield = float.Parse(r.Attribute("GoldYield").Value),
                    ScienceYield = float.Parse(r.Attribute("ScienceYield").Value),
                    CultureYield = float.Parse(r.Attribute("CultureYield").Value),
                    HappinessYield = int.Parse(r.Attribute("HappinessYield").Value),
                    MaintenanceCost = float.Parse(r.Attribute("MaintenanceCost").Value),
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList(),
                }
            );
        return BuildingData;
    }
}
