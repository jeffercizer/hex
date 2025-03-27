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
    public int ProductionCost;
    public int GoldCost;
    public Yields yields;
    public float MaintenanceCost;
    public List<String> Effects;
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
                    yields.food = int.Parse(r.Attribute("FoodYield").Value),
                    yields.production = int.Parse(r.Attribute("ProductionYield").Value),
                    yields.gold = float.Parse(r.Attribute("GoldYield").Value),
                    yields.science = float.Parse(r.Attribute("ScienceYield").Value),
                    yields.culture = float.Parse(r.Attribute("CultureYield").Value),
                    yields.happiness = int.Parse(r.Attribute("HappinessYield").Value),
                    MaintenanceCost = float.Parse(r.Attribute("MaintenanceCost").Value),
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList(),
                }
            );
        return BuildingData;
    }
}
