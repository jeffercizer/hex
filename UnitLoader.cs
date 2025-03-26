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
    public List<string> Effects { get; set; 
}

public class UnitLoader
{
    Dictionary<UnitType, UnitInfo> Units;
    Dictionary<UnitType, UnitEffect> UnitEffects;
    public delegate void UnitEffect(UnitInfo UnitInfo);
    
    public UnitLoader()
    {
        UnitEffects = new Dictionary<UnitType, UnitEffect>
        {
            // { UnitType.Silk, ApplySilkEffect },
            // { UnitType.Jade, ApplyJadeEffect },
            // { UnitType.Iron, ApplyIronEffect },
            // { UnitType.Horses, ApplyHorsesEffect },
            // { UnitType.Oil, ApplyOilEffect },
        };
        string xmlPath = "Units.xml";
        Units = LoadUnitData(xmlPath);
        //if (Units.TryGetValue(Unit, out UnitInfo info))
        //ExecuteUnitEffect(Unit);
    }
    public static Dictionary<UnitType, UnitInfo> LoadUnitData(string xmlPath)
    {
        // Load the XML file
        XDocument xmlDoc = XDocument.Load(xmlPath);

        // Parse the Unit information into a dictionary
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
                    Effects = r.Element("Effects").Elements("Effect").Select(e => e.Value).ToList()
                }
            );
}

        return UnitData;
    }
    public void ExecuteUnitEffect(UnitType UnitType)
    {
        if (Units.TryGetValue(UnitType, out UnitInfo info) &&
            UnitEffects.TryGetValue(UnitType, out UnitEffect effect))
        {
            effect.Invoke(info);
        }
    }
}
