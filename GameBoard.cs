using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class GameBoard
{
    public GameBoard(Game game, int top, int bottom, int left, int right)
    {
        this.game = game;
        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
        gameHexDict = new();
        Random rnd = new Random();
        for (int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            for (int q = left - r_offset; q <= right - r_offset; q++){
                gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), this, (TerrainType)rnd.Next(0,3), TerrainTemperature.Grassland, new HashSet<FeatureType>()));
            }
        }
    }

    public GameBoard(Game game, Dictionary<Hex, GameHex> gameHexDict, int top, int bottom, int left, int right)
    {
        this.game = game;
        this.gameHexDict = gameHexDict;
        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
    }

    public Game game;
    public Dictionary<Hex, GameHex> gameHexDict;
    public int top;
    public int bottom;
    public int left;
    public int right;

    public void OnTurnStarted(int turnNumber)
    {
        foreach (GameHex hex in gameHexDict.Values)
        {
            hex.OnTurnStarted(turnNumber);
        }
        Console.WriteLine($"GameBoard: Started turn {turnNumber}.");
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (GameHex hex in gameHexDict.Values)
        {
            hex.OnTurnEnded(turnNumber);
        }
        Console.WriteLine($"GameBoard: Ended turn {turnNumber}.");
    }

    public void PrintGameBoard()
    {
        //terraintype
        GameHex test;
        for(int r = top; r <= bottom; r++){
            int r_offset = r>>1; //same as (int)Math.Floor(r/2.0f)
            String mapRow = ""; 
            if (r%2 == 1)
            {
                mapRow += " ";
            }
            for (int q = left - r_offset; q <= right - r_offset; q++){
                if(gameHexDict.TryGetValue(new Hex(q, r, -q-r), out test)){
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
                if(gameHexDict.TryGetValue(new Hex(q, r, -q-r), out test)){
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
