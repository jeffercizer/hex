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
    Fortification
}

[Serializable]
public class GameHex
{
    public GameHex(Hex hex, GameBoard ourGameBoard, TerrainType terrainType, TerrainTemperature terrainTemp, ResourceType resourceType, HashSet<FeatureType> featureSet, List<Unit> unitsList, District district)
    {
        this.hex = hex;
        this.ourGameBoard = ourGameBoard;
        this.terrainType = terrainType;
        this.terrainTemp = terrainTemp;
        this.featureSet = featureSet;
        this.resourceType = resourceType;
        this.unitsList = unitsList;
        this.district = district;
    }

    public Hex hex;
    public GameBoard ourGameBoard;
    public TerrainType terrainType;
    public TerrainTemperature terrainTemp;
    public ResourceType resourceType;
    public int ownedBy;
    public City owningCity;
    public HashSet<FeatureType> featureSet = new();
    public List<Unit> unitsList = new();
    public District? district;

    public Yields yields;

    public void RecalculateYields()
    {
        yields = new();
        //if the distrit is urban the buildings will set our yields
        if (district != null & !district.isUrban)
        {
            //calculate the rural value
            if(terrainType == TerrainType.Flat)
            {
                ourCity.AddFlatYields(this);
            }
            else if (terrainType == TerrainType.Rough)
            {
                ourCity.AddRoughYields(this);
            }
            else if (terrainType == TerrainType.Mountain)
            {
                ourCity.AddMountainYields(this);
            }
            else if (terrainType == TerrainType.Coast)
            {
                ourCity.AddCoastYields(this);
            }
            else if (terrainType == TerrainType.Ocean)
            {
                ourCity.AddOceanYields(this);
            }
            
            if(terrainTemp == TerrainTemperature.Desert)
            {
                ourCity.AddDesertYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Plains)
            {
                ourCity.AddPlainsYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Grassland)
            {
                ourCity.AddGrasslandYields(this);
            }
            else if (terrainTemp == TerrainTemperature.Tundra)
            {
                ourCity.AddTundraYields(this);
            }
            else
            {
                ourCity.AddArcticYields(this);
            }
            if(featureSet.Contains(FeatureType.Forest))
            {
                yields.production += 1;
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

    public void ClaimHex(int teamNum, City city)
    {
        ownedBy = teamNum;
        owningCity = city;
    }

    public bool TryClaimHex(int teamNum, City city)
    {
        if(ownedBy == -1)
        {
            ownedBy = teamNum;
            owningCity = city;
            return true;
        }
        return false;
    }

    //if stackable is true allow multiple units to stack
    //if flexible is true look for adjacent spaces to place
    public bool SpawnUnit(Unit newUnit, bool stackable, bool flexible)
    {
        if((!stackable & unitsList.Any()) | newUnit.movementCosts[(TerrainMoveType)terrainType] > 100) //if they cant stack and their are units or the hex is invalid for this unit
        {
            if (flexible)
            {
                foreach(Hex rangeHex in hex.WrappingRange(3, ourGameBoard.left, ourGameBoard.right, ourGameBoard.top, ourGameBoard.bottom))
                {
                    if(ourGameBoard.gameHexDict[rangeHex].SpawnUnit(newUnit, stackable, false))
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
            unitsList.Add(newUnit);
            newUnit.SetGameHex(this);
            return true;
        }
        return false;
    }
    
}
