using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

public enum UnitType
{
  Scout = "Scout",
  Settler = "Settler"
}

public struct UnitInfo
{
    public int ProductionCost { get; set; }
    public int GoldCost { get; set; }
    public float MovementSpeed { get; set; }
    public float SightRange { get; set; }
    public float CombatPower { get; set; }
    public int HealingFactor { get; set; }
    public List<String> Effects { get; set; }
    public List<String> Abilities { get; set; }
}

public class UnitLoader
{
    Dictionary<UnitType, UnitInfo> UnitsLoader;
    public delegate void UnitEffect(UnitInfo UnitInfo);
    
    public UnitLoader()
    {
        string xmlPath = "Units.xml";
        Units = LoadUnitData(xmlPath);
        //if (Units.TryGetValue(Unit, out UnitInfo info))
        //ExecuteUnitEffect(Unit);
    }
    public static Dictionary<UnitType, UnitInfo> LoadUnitData(string xmlPath)
    {
        XDocument xmlDoc = XDocument.Load(xmlPath);
        var UnitData = xmlDoc.Descendants("Unit")
            .ToDictionary(
                r => (UnitType)Enum.Parse(typeof(UnitType), r.Attribute("Name").Value),
                r => new UnitInfo
                {
                    ProductionCost = int.Parse(r.Attribute("ProductionCost").Value),
                    GoldCost = int.Parse(r.Attribute("GoldCost").Value),
                    MovementSpeed = float.Parse(r.Attribute("MovementSpeed").Value),
                    SightRange = float.Parse(r.Attribute("SightRange").Value),
                    CombatPower = float.Parse(r.Attribute("CombatPower").Value),
                    HealingFactor = int.Parse(r.Attribute("HealingFactor").Value),
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList(),
                    Abilities = r.Element("Abilities").Elements("Ability").Select(e => e.Value).ToList()
                }
            );
        return UnitData;
    }
    public void ExecuteUnitEffect(UnitType UnitType)
    {
        //pick a thing idk
    }
    public void SettlerAbility()
    {
        
    }
}
