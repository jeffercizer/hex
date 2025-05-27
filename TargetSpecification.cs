using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Formats.Asn1;
using Godot;
using System.IO;

public class TargetSpecification
{
    public bool TargetUnits { get; set; } = false;
    public bool TargetRuralBuildings { get; set; } = false;
    public bool TargetUrbanBuildings { get; set; } = false;
    public bool TargetTiles { get; set; } = false;
    public bool TargetSelf { get; set; } = false;

    public HashSet<String> ValidUnitTypes { get; set; } = new HashSet<String>();
    public UnitClass AllowedUnitClasses { get; set; } = UnitClass.None;
    public bool AllowsAnyUnit { get; set; } = false;

    public HashSet<String> ValidBuildingTypes { get; set; } = new HashSet<String>();
    public bool AllowsAnyBuilding { get; set; } = false;

    public HashSet<TerrainType> ValidTerrainTypes { get; set; } = new HashSet<TerrainType>();
    public bool AllowsAnyTerrain { get; set; } = false;

    public HashSet<ResourceType> ValidResourceTypes { get; set; } = new HashSet<ResourceType>();
    public bool AllowsAnyResource { get; set; } = false;
    public bool RequiresAResource { get; set; } = false;

    public HashSet<FeatureType> ValidFeatureTypes { get; set; } = new HashSet<FeatureType>();
    public bool AllowsAnyFeature { get; set; } = false;
    public bool RequiresAFeature { get; set; } = false;

    public bool AllowsAlly { get; set; } = false;
    public bool AllowsEnemy { get; set; } = false;
    public bool AllowsNeutral { get; set; } = false;


    public bool IsHexValidTarget(GameHex gameHex, Unit castingUnit)
    {
        //check hex info first before units/buildings to save time
        if(RequiresAResource && gameHex.resourceType == ResourceType.None)
        {
            return false;
        }
        if(RequiresAFeature && gameHex.featureSet.Count == 0)
        {
            return false;
        }
        if(!AllowsAnyFeature)
        {
            if (ValidFeatureTypes.Count == 0)
            {
                throw new Exception("Must define ValidFeatureTypes (none is an option) or set AllowsAnyFeature to true for import: " + castingUnit.name);
            }
            bool validFeature = false;
            foreach(FeatureType featureType in gameHex.featureSet)
            {
                foreach (FeatureType featureType2 in  ValidFeatureTypes)
                {
                    if (featureType == featureType2)
                    {
                        validFeature = true;
                        break;
                    }
                }
            }
            if(!validFeature)
            {
                return false;
            }
        }
        if (!AllowsAnyResource)
        {
            if (ValidResourceTypes.Count == 0)
            {
                throw new Exception("Must define ValidResourceTypes (none is an option) or set AllowsAnyResource to true for import: " + castingUnit.name);
            }
            bool validResource = false;
            foreach (ResourceType resourceType in ValidResourceTypes)
            {
                if (resourceType == gameHex.resourceType)
                {
                    validResource = true;
                    break;
                }
            }
            if (!validResource)
            {
                return false;
            }
        }
        if (!AllowsAnyTerrain)
        {
            if (ValidTerrainTypes.Count == 0)
            {
                throw new Exception("Must define ValidTerrainTypes or set AllowsAnyTerrain to true for import: " + castingUnit.name);
            }
            bool validTerrain = false;
            foreach (TerrainType terrainType in ValidTerrainTypes)
            {
                if (terrainType == gameHex.terrainType)
                {
                    validTerrain = true;
                    break;
                }
            }
            if (!validTerrain)
            {
                return false;
            }
        }

        if(!AllowsAnyBuilding)
        {
            if (ValidBuildingTypes.Count == 0)
            {
                throw new Exception("Must define ValidStrings (none is an option) or set AllowsAnyBuilding to true for import: " + castingUnit.name);
            }
            bool validBuilding = false;
            if (gameHex.district != null)
            {
                foreach (String allowedBuildingType in ValidBuildingTypes)
                {
                    if (allowedBuildingType == "" && gameHex.district.buildings.Count == 0)
                    {
                        //probably a bugged edge case?
                        return true;
                    }
                    foreach (Building building in gameHex.district.buildings)
                    {
                        if (allowedBuildingType == building.buildingType)
                        {
                            validBuilding = true;
                            break;
                        }
                    }
                }
                if (!validBuilding)
                {
                    return false;
                }
            }
            else
            {
                foreach (String allowedBuildingType in ValidBuildingTypes)
                {
                    if (allowedBuildingType == "")
                    {
                        return true;
                    }
                }
               return false;
            }
        }

        if (!AllowsAnyUnit)
        {
            if (ValidUnitTypes.Count == 0)
            {
                throw new Exception("Must define ValidUnitTypes (none is an option) or set AllowsAnyUnit to true for import: " + castingUnit.name);
            }
            bool validUnit = false;
            foreach (String unitType in ValidUnitTypes)
            {
                foreach (Unit unit in gameHex.units)
                {
                    if (unitType == unit.unitType)
                    {
                        validUnit = true;
                        break;
                    }
                }
            }
            if (!validUnit)
            {
                return false;
            }
        }

        //we know that the gamehex terrain,features, and resources are valid or not needed to be checked
        //now check self, unit, ruralbuilding, urban building, can return early if we find a valid case since we checked all other hex requirements
        if (TargetSelf)
        {
            return true;
        }
        //check if we can target empty tiles
        if (TargetTiles)
        {
            return true;
        }
        if (TargetUnits)
        {
            foreach (Unit unit in gameHex.units)
            {
                if(AllowsAlly)
                {
                    if (Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(unit.teamNum))
                    {
                        return true;
                    }
                }
                if(AllowsEnemy)
                {
                    if (Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(unit.teamNum))
                    {
                        return true;
                    }
                }
                if(AllowsNeutral)
                {
                    if (!Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(unit.teamNum) && !Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(unit.teamNum))
                    {
                        return true;
                    }
                }
            }
        }
        if (TargetRuralBuildings)
        {
            if(gameHex.district != null)
            {
                foreach (Building building in gameHex.district.buildings)
                {
                    if (AllowsAlly)
                    {
                        if (Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                    if (AllowsEnemy)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                    if (AllowsNeutral)
                    {
                        if (!Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum) && !Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        if (TargetUrbanBuildings)
        {
            if (gameHex.district != null)
            {
                foreach (Building building in gameHex.district.buildings)
                {
                    if (AllowsAlly)
                    {
                        if (Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                    if (AllowsEnemy)
                    {
                        if (Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                    if (AllowsNeutral)
                    {
                        if (!Global.gameManager.game.teamManager.GetAllies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum) && !Global.gameManager.game.teamManager.GetEnemies(castingUnit.teamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[building.districtHex].district.cityID].teamNum))
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}
