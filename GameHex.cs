using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.IO;

public enum TerrainType
{
    Flat,
    Rough,
    Mountain,
    Coast,
    Ocean
}

public enum TerrainTemperature
{
    Desert,
    Plains,
    Grassland,
    Tundra,
    Arctic
}

public enum FeatureType
{
    Forest,
    River,
    Road,
    Coral,
    Wetland,
    Fortification,
    None
}

[Serializable]
public class GameHex
{
    public GameHex(Hex hex, int gameBoardID, TerrainType terrainType, TerrainTemperature terrainTemp, ResourceType resourceType, HashSet<FeatureType> featureSet, List<Unit> units, District district)
    {
        this.hex = hex;
        this.gameBoardID = gameBoardID;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.resourceType = resourceType;
        this.units = units;
        this.district = district;
        this.ownedBy = -1;
        RecalculateYields();
    }

    public Hex hex { get; set; }
    public int gameBoardID { get; set; }
    public TerrainType terrainType { get; set; }
    public TerrainTemperature terrainTemp { get; set; }
    public ResourceType resourceType { get; set; }
    public int ownedBy { get; set; }
    public int owningCityID { get; set; }
    public HashSet<FeatureType> featureSet { get; set; } = new();
    public List<Unit> units { get; set; } = new();
    public District? district { get; set; }

    public Yields yields { get; set; }

    public GameHex()
    {
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static GameHex Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<GameHex>(reader);
    }


    public void RecalculateYields()
    {
        yields = new();
        //if the district is urban the buildings will set our yields
        City temp;
        if ((Global.gameManager.game.cityDictionary.TryGetValue(owningCityID, out temp) && district == null) || (district != null && !district.isUrban))
        {
            //calculate the rural value
            if(terrainType == TerrainType.Flat)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddFlatYields(this);
            }
            else if (terrainType == TerrainType.Rough)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddRoughYields(this);
            }
            else if (terrainType == TerrainType.Mountain)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddMountainYields(this);
            }
            else if (terrainType == TerrainType.Coast)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddCoastYields(this);
            }
            else if (terrainType == TerrainType.Ocean)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddOceanYields(this);
            }
            
            if(terrainTemp == TerrainTemperature.Desert)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddDesertYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Plains)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddPlainsYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Grassland)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddGrasslandYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Tundra)
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddTundraYields(this);
            }
            else
            {
                Global.gameManager.game.cityDictionary[owningCityID].AddArcticYields(this);
            }
            if(featureSet.Contains(FeatureType.Forest))
            {
                yields.production += 1;
            }
            if(featureSet.Contains(FeatureType.Wetland))
            {
                yields.food += 1;
            }
        }
        else if(district == null)
        {
            SetUnownedHexYields();
        }
    }

    public void SetUnownedHexYields()
    {
        if(terrainType == TerrainType.Flat)
        {
            yields.food += 1;
        }
        else if (terrainType == TerrainType.Rough)
        {
            yields.production += 1;
        }
        else if (terrainType == TerrainType.Mountain)
        {
            //nothing
        }
        else if (terrainType == TerrainType.Coast)
        {
            yields.food += 1;
        }
        else if (terrainType == TerrainType.Ocean)
        {
            yields.gold += 1;
        }
        
        if(terrainTemp == TerrainTemperature.Desert)
        {
            yields.gold += 1;
        }
        else if (terrainTemp == TerrainTemperature.Plains)
        {
            yields.production += 1;
        }
        else if (terrainTemp == TerrainTemperature.Grassland)
        {
            yields.food += 1;
        }
        else if (terrainTemp == TerrainTemperature.Tundra)
        {
            yields.happiness += 1;
        }
        else
        {
            //nothing
        }
    }

    public void OnTurnStarted(int turnNumber)
    {

    }

    public void OnTurnEnded(int turnNumber)
    {

    }
    public bool SetTerrainType(TerrainType newTerrainType)
    {
        this.terrainType = newTerrainType;
        return true;
    }

    public bool AddTerrainFeature(FeatureType newFeature)
    {
        this.featureSet.Add(newFeature);
        return true;
    }

    public void ClaimHex(City city)
    {
        ownedBy = city.teamNum;
        owningCityID = city.id;
        city.heldHexes.Add(hex);
    }

    public bool TryClaimHex(City city)
    {
        if(ownedBy == -1)
        {
            ClaimHex(city);
            return true;
        }
        return false;
    }

    public bool IsEnemyPresent(int yourTeamNum)
    {
        bool isEnemy = false;
        foreach (Unit targetHexUnit in Global.gameManager.game.mainGameBoard.gameHexDict[hex].units)
        {
            if (Global.gameManager.game.teamManager.GetEnemies(yourTeamNum).Contains(targetHexUnit.teamNum))
            {
                isEnemy = true;
                break;
            }
        }                                                                           
        if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district != null && Global.gameManager.game.teamManager.GetEnemies(yourTeamNum).Contains(Global.gameManager.game.cityDictionary[Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.cityID].teamNum))
        {
            if (Global.gameManager.game.mainGameBoard.gameHexDict[hex].district.health > 0)
            {
                isEnemy = true;
            }
        }
        return isEnemy;
    }

    //if stackable is true allow multiple units to stack
    //if flexible is true look for adjacent spaces to place
    public bool SpawnUnit(Unit newUnit, bool stackable, bool flexible)
    {
        if((!stackable & units.Any()) | newUnit.movementCosts[(TerrainMoveType)terrainType] > 100) //if they cant stack and their are units or the hex is invalid for this unit
        {
            if (flexible)
            {
                foreach(Hex rangeHex in hex.WrappingRange(3, Global.gameManager.game.mainGameBoard.left, Global.gameManager.game.mainGameBoard.right, Global.gameManager.game.mainGameBoard.top, Global.gameManager.game.mainGameBoard.bottom).OrderBy(h => hex.Distance(h)))
                {
                    if(Global.gameManager.game.mainGameBoard.gameHexDict[rangeHex].SpawnUnit(newUnit, stackable, false))
                    {
                        return true;
                    }
                }
                //if we still havent found a spot give up
            }
            return false;
        }
        else if(newUnit.movementCosts[(TerrainMoveType)terrainType] < 100)//if they cant stack and there aren't units or they can stack and units are/aren't there and the hex is valid for this unit
        {
            units.Add(newUnit);
            newUnit.SpawnSetup(this);
            return true;
        }
        return false;
    }
    
}
