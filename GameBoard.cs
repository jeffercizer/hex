using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

public class GameTile
{
    public GameHex gameHex { get; set; } // Represents the tile's core data
    public bool hasBeenSeen { get; set; } // Whether the tile has been revealed at least once by the active players team
    public int unitsSeeing { get; set; } // Number of units currently seeing the tile on the active players team
    
}

struct GameBoard
{
    public GameBoard(int top, int bottom, int left, int right)
    {
        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
        gameTileDict = new();
        Random rnd = new Random();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                gameTileDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), (TerrainType)rnd.Next(0,3), TerrainTemperature.Grassland, new HashSet<FeatureType>()));
            }
        }
    }

    public GameBoard(Dictionary<Hex, GameTile> gameTileDict, int top, int bottom, int left, int right)
    {
        this.gameTileDict = gameTileDict;
        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
    }

    public Dictionary<Hex, GameTile> gameTileDict;
    public int top;
    public int bottom;
    public int left;
    public int right;

    public void OnTurnStarted(int turnNumber)
    {
        foreach (GameTile tile.gameHex in gameTileDict)
        {
            hex.OnTurnStarted();
        }
        Console.WriteLine($"GameBoard: Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (GameTile tile.gameHex in gameTileDict)
        {
            hex.OnTurnEnded();
        }
        Console.WriteLine($"GameBoard: Ended turn {turnNumber}.");
    }

    public void PrintGameBoard()
    {
        //terraintype
        GameHex test;
        GameTile tile;
        for(int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            String mapRow = ""; 
            if (r%2 == 1)
            {
                mapRow += " ";
            }
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(gameTileDict.TryGetValue(new Hex(q, r, -q-r), out tile)){
                    test = tile.gameHex;
                    if(test.terrainType == TerrainType.Flat)
                    {
                        mapRow += "F ";
                    }
                    else if(test.terrainType == TerrainType.Rough)
                    {
                        mapRow += "R ";
                    }
                    else if(test.terrainType == TerrainType.Mountain)
                    {
                        mapRow += "M ";
                    }
                    else if(test.terrainType == TerrainType.Coast)
                    {
                        mapRow += "C ";
                    }
                    else if(test.terrainType == TerrainType.Ocean)
                    {
                        mapRow += "O ";
                    }
                }
            }
            Console.WriteLine(mapRow);
        }
        Console.WriteLine();

        //features
        for(int r = top; r <= bottom; r++)
        {
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            String mapRow = ""; 
            if (r%2 == 1)
            {
                mapRow += " ";
            }
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(gameTileDict.TryGetValue(new Hex(q, r, -q-r), out test)){
                    foreach (FeatureType feature in test.featureSet)
                    {
                        if (feature == FeatureType.Road)
                        {
                            mapRow += "R ";
                            break;
                        }
                        else
                        {
                            mapRow += "* ";
                        }
                    }
                }
            }
            Console.WriteLine(mapRow);
        }
        Console.WriteLine();
    }
}

struct GameBoardTest
{
    
}
