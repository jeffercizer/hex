using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

[Flags]
public enum UnitClass
{
    None = 0,
    Civilian = 1 << 0,
    Combat = 1 << 1,
    Land = 1 << 2,
    Naval = 1 << 3,
    Air = 1 << 4,
    Infantry = 1 << 5,
    Ranged = 1 << 6,
    Cavalry = 1 << 7,
    Siege = 1 << 8,
    Recon = 1 << 9,
}


public enum UnitType
{
    None,
    Founder,
    Scout,
    Settler,
    Galley,
    Slinger,
    Warrior
}

public struct UnitInfo
{
    public UnitClass Class { get; set; }
    public int ProductionCost { get; set; }
    public int GoldCost { get; set; }
    public float MovementSpeed { get; set; }
    public float SightRange { get; set; }
    public float CombatPower { get; set; }
    public int HealingFactor { get; set; }
    public int MaintenanceCost { get; set; }
    public Dictionary<TerrainMoveType, float> MovementCosts { get; set; }
    public Dictionary<TerrainMoveType, float> SightCosts { get; set; }
    public List<String> Effects { get; set; }
    public Dictionary<string, (float CombatPower, int UsageCount, int Range, TargetSpecification? Specification)> Abilities { get; set; }
}

public static class UnitLoader
{
    public static Dictionary<UnitType, UnitInfo> unitsDict;

    public static Dictionary<UnitType, string> unitNames = new Dictionary<UnitType, string>
    {
        { UnitType.Founder, "Founder"},
        { UnitType.Scout, "Scout" },
        { UnitType.Settler, "Settler" },
        { UnitType.Galley, "Galley" },
        { UnitType.Slinger, "Slinger" },
        { UnitType.Warrior, "Warrior" },
    };
    
    static UnitLoader()
    {
        string xmlPath = "Units.xml";
        unitsDict = LoadUnitData(xmlPath);
    }
    public static Dictionary<UnitType, UnitInfo> LoadUnitData(string xmlPath)
    {
        XDocument xmlDoc = XDocument.Load(xmlPath);
        var UnitData = xmlDoc.Descendants("Unit")
            .ToDictionary(
                r => (UnitType)Enum.Parse(typeof(UnitType), r.Attribute("Name").Value),
                r => new UnitInfo
                {
                    Class = Enum.TryParse<UnitClass>(r.Attribute("Class")?.Value, out var unitClass) ? unitClass : UnitClass.None,
                    ProductionCost = int.TryParse(r.Attribute("ProductionCost")?.Value, out var productionCost) ? productionCost : 0,
                    GoldCost = int.TryParse(r.Attribute("GoldCost")?.Value, out var goldCost) ? goldCost : 0,
                    MovementSpeed = float.TryParse(r.Attribute("MovementSpeed")?.Value, out var movementSpeed) ? movementSpeed : 0.0f,
                    SightRange = float.TryParse(r.Attribute("SightRange")?.Value, out var sightRange) ? sightRange : 0.0f,
                    CombatPower = float.TryParse(r.Attribute("CombatPower")?.Value, out var combatPower) ? combatPower : 0.0f,
                    HealingFactor = int.TryParse(r.Attribute("HealingFactor")?.Value, out var healingFactor) ? healingFactor : 0,
                    MaintenanceCost = int.TryParse(r.Attribute("MaintenanceCost")?.Value, out var maintenanceCost) ? maintenanceCost : 0,
                    MovementCosts = r.Element("MovementCosts")?.Elements("TerrainMoveType").ToDictionary(
                        m => Enum.TryParse<TerrainMoveType>(m.Attribute("Name")?.Value, out var terrainType) ? terrainType : throw new Exception("Invalid TerrainMoveType"),
                        m => float.TryParse(m.Attribute("Value")?.Value, out var movementCost) ? movementCost : 0.0f
                    ) ?? new Dictionary<TerrainMoveType, float>(),
                    SightCosts = r.Element("SightCosts")?.Elements("TerrainMoveType").ToDictionary(
                        s => Enum.TryParse<TerrainMoveType>(s.Attribute("Name")?.Value, out var terrainType) ? terrainType : throw new Exception("Invalid TerrainMoveType"),
                        s => float.TryParse(s.Attribute("Value")?.Value, out var sightCost) ? sightCost : 0.0f
                    ) ?? new Dictionary<TerrainMoveType, float>(),
                    Effects = r.Element("Effects")?.Elements("Effect").Select(e => e.Value).ToList() ?? new List<string>(),
                    Abilities = r.Element("Abilities")?.Elements("Ability").ToDictionary(
                        a => a.Attribute("Name")?.Value ?? throw new Exception("Invalid Ability Name"),
                        a =>
                        (
                            float.TryParse(a.Attribute("CombatPower")?.Value, out var combatPower) ? combatPower : 0,
                            int.TryParse(a.Attribute("UsageCount")?.Value, out var usageCount) ? usageCount : 1,
                            int.TryParse(a.Attribute("Range")?.Value, out var range) ? range : 0,
                            ParseTargetSpecification(a.Element("TargetSpecification"))
                        )
                    ) ?? new Dictionary<string, (float, int, int, TargetSpecification?)>(),
                }
            );
        return UnitData;
    }
    static TargetSpecification ParseTargetSpecification(XElement targetSpecElement)
    {
        if (targetSpecElement == null) return null;
    
        var targetSpecification = new TargetSpecification
        {
            AllowsAnyUnit = bool.TryParse(targetSpecElement.Attribute("AllowsAnyUnit")?.Value, out var allowsAnyUnit) && allowsAnyUnit,
            AllowsAnyBuilding = bool.TryParse(targetSpecElement.Attribute("AllowsAnyBuilding")?.Value, out var allowsAnyBuilding) && allowsAnyBuilding,
            AllowsAnyTerrain = bool.TryParse(targetSpecElement.Attribute("AllowsAnyTerrain")?.Value, out var allowsAnyTerrain) && allowsAnyTerrain,
            AllowsAlly = bool.TryParse(targetSpecElement.Attribute("AllowsAlly")?.Value, out var allowsAlly) && allowsAlly,
            AllowsEnemy = bool.TryParse(targetSpecElement.Attribute("AllowsEnemy")?.Value, out var allowsEnemy) && allowsEnemy,
            AllowsNeutral = bool.TryParse(targetSpecElement.Attribute("AllowsNeutral")?.Value, out var allowsNeutral) && allowsNeutral,
        };
    
        targetSpecification.ValidUnitTypes = targetSpecElement.Element("ValidUnitTypes")?.Elements("UnitType")
            .Select(u => Enum.TryParse<UnitType>(u.Attribute("Name")?.Value, out var unitType) ? unitType : throw new Exception("Invalid UnitType"))
            .ToHashSet() ?? new HashSet<UnitType>();
    
        targetSpecification.AllowedUnitClasses = targetSpecElement.Element("AllowedUnitClasses")?.Value
            .Split(", ", StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(UnitClass.None, (current, className) =>
                Enum.TryParse<UnitClass>(className, out var unitClass) ? current | unitClass : throw new Exception("Invalid UnitClass"))
            ?? UnitClass.None;

    
        targetSpecification.ValidBuildingTypes = targetSpecElement.Element("ValidBuildingTypes")?.Elements("BuildingType")
            .Select(b => Enum.TryParse<BuildingType>(b.Attribute("Name")?.Value, out var buildingType) ? buildingType : throw new Exception("Invalid BuildingType"))
            .ToHashSet() ?? new HashSet<BuildingType>();
    
        targetSpecification.ValidTerrainTypes = targetSpecElement.Element("ValidTerrainTypes")?.Elements("TerrainType")
            .Select(t => Enum.TryParse<TerrainType>(t.Attribute("Name")?.Value, out var terrainType) ? terrainType : throw new Exception("Invalid TerrainType"))
            .ToHashSet() ?? new HashSet<TerrainType>();
    
        return targetSpecification;
    }
}
