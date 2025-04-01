using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;

public class TargetSpecification
{
    public bool AllowTargetSelf { get; set; } = true;
    public bool AllowsAnyUnit { get; set; } = false;
    public bool AllowsAnyBuilding { get; set; } = false; // New flag for wildcard building targets
    public HashSet<UnitType> ValidUnitTypes { get; set; } = new HashSet<UnitType>();
    public UnitClass AllowedUnitClasses { get; set; } = UnitClass.None;
    public HashSet<BuildingType> ValidBuildingTypes { get; set; } = new HashSet<BuildingType>();
    public HashSet<TerrainType> ValidTerrainTypes { get; set; } = new HashSet<TerrainType>();

    public bool IsValidTarget(UnitType? unitType, UnitClass? unitClass, BuildingType? buildingType, TerrainType? terrainType)
    {
        if (AllowsAnyUnit && unitType.HasValue)
            return true;

        if (AllowsAnyBuilding && buildingType.HasValue)
            return true;

        if (unitType.HasValue && !ValidUnitTypes.Contains(unitType.Value))
            return false;

        if (unitClass.HasValue && (AllowedUnitClasses & unitClass.Value) == 0)
            return false;

        if (buildingType.HasValue && !ValidBuildingTypes.Contains(buildingType.Value))
            return false;

        if (terrainType.HasValue && !ValidTerrainTypes.Contains(terrainType.Value))
            return false;

        return true;
    }
}
