using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

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
    public GameHex(Hex hex, GameBoard gameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, ResourceType resourceType, HashSet<FeatureType> featureSet, List<Unit> units, District district)
    {
        this.hex = hex;
        this.gameBoard = gameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.resourceType = resourceType;
        this.units = units;
        this.district = district;
        this.ownedBy = -1;
        RecalculateYields();
    }

    public Hex hex;
    public GameBoard gameBoard;
    public TerrainType terrainType;
    public TerrainTemperature terrainTemp;
    public ResourceType resourceType;
    public int ownedBy;
    public City? owningCity;
    public HashSet<FeatureType> featureSet = new();
    public List<Unit> units = new();
    public District? district;

    public Yields yields;

    public void RecalculateYields()
    {
        yields = new();
        //if the district is urban the buildings will set our yields
        if ((owningCity != null && district == null) || (district != null && !district.isUrban))
        {
            //calculate the rural value
            if(terrainType == TerrainType.Flat)
            {
                owningCity.AddFlatYields(this);
            }
            else if (terrainType == TerrainType.Rough)
            {
                owningCity.AddRoughYields(this);
            }
            else if (terrainType == TerrainType.Mountain)
            {
                owningCity.AddMountainYields(this);
            }
            else if (terrainType == TerrainType.Coast)
            {
                owningCity.AddCoastYields(this);
            }
            else if (terrainType == TerrainType.Ocean)
            {
                owningCity.AddOceanYields(this);
            }
            
            if(terrainTemp == TerrainTemperature.Desert)
            {
                owningCity.AddDesertYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Plains)
            {
                owningCity.AddPlainsYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Grassland)
            {
                owningCity.AddGrasslandYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Tundra)
            {
                owningCity.AddTundraYields(this);
            }
            else
            {
                owningCity.AddArcticYields(this);
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
        owningCity = city;
        city.heldHexes.Add(hex);
    }

    public bool TryClaimHex(City city)
    {
        if(ownedBy == -1)
        {
            ownedBy = city.teamNum;
            owningCity = city;
            owningCity.heldHexes.Add(hex);
            return true;
        }
        return false;
    }

    public bool IsEnemyPresent(int yourTeamNum)
    {
        bool isEnemy = false;
        foreach (Unit targetHexUnit in gameBoard.gameHexDict[hex].units)
        {
            if (gameBoard.game.teamManager.GetEnemies(yourTeamNum).Contains(targetHexUnit.teamNum))
            {
                isEnemy = true;
                break;
            }
        }
        if (gameBoard.gameHexDict[hex].district != null && gameBoard.game.teamManager.GetEnemies(yourTeamNum).Contains(gameBoard.gameHexDict[hex].district.city.teamNum))
        {
            if (gameBoard.gameHexDict[hex].district.health > 0)
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
                foreach(Hex rangeHex in hex.WrappingRange(3, gameBoard.left, gameBoard.right, gameBoard.top, gameBoard.bottom).OrderBy(h => hex.Distance(h)))
                {
                    if(gameBoard.gameHexDict[rangeHex].SpawnUnit(newUnit, stackable, false))
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
