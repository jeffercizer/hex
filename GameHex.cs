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
    Coral
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
    public HashSet<FeatureType> featureSet = new();
    public List<Unit> unitsList = new();
    public District? district;

    public void OnTurnStarted(int turnNumber)
    {
        //Console.WriteLine($"GameHex ({hex.q},{hex.r}): Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        //Console.WriteLine($"GameHex ({hex.q},{hex.r}): Ended turn {turnNumber}.");
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

    public void ClaimHex(int teamNum)
    {
        ownedBy = teamNum;
    }

    public bool TryClaimHex(int teamNum)
    {
        if(ownedBy == -1)
        {
            ownedBy = teamNum;
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
                for (int i = 0; i < 6; i++) //ask all our neighbors if they have space
                {
                    if(ourGameBoard.gameHexDict[hex.WrappingNeighbor(i, ourGameBoard.left, ourGameBoard.left)].SpawnUnit(newUnit, stackable, flexible))
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
