using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.IO;

[Serializable]
public class GameBoard
{
    public GameBoard(int id, int bottom, int right)
    {
        this.id = id;
        this.top = 0;
        this.bottom = bottom;
        this.left = 0;
        this.right = right;
        gameHexDict = new();
        Random rnd = new Random();
        for (int r = 0; r <= bottom; r++){
            for (int q = 0; q <= right; q++){
                gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), this.id, (TerrainType)rnd.Next(0,3), TerrainTemperature.Grassland, (ResourceType)0, new HashSet<FeatureType>(), new List<Unit>(), null));
            }
        }
        if (Global.gameManager.game.TryGetGraphicManager(out GraphicManager manager)) manager.NewGameBoard(this);
    }

    public int id { get; set; }
    public Dictionary<Hex, GameHex> gameHexDict { get; set; }
    public int top { get; set; }
    public int bottom { get; set; }
    public int left { get; set; }
    public int right { get; set; }

    public GameBoard()
    {
        //for loading
    }

    public void Serialize(BinaryWriter writer)
    {
        Serializer.Serialize(writer, this);
    }

    public static GameBoard Deserialize(BinaryReader reader)
    {
        return Serializer.Deserialize<GameBoard>(reader);
    }


    public void OnTurnStarted(int turnNumber)
    {
        foreach (GameHex hex in gameHexDict.Values)
        {
            hex.OnTurnStarted(turnNumber);
        }
    }

    public void OnTurnEnded(int turnNumber)
    {
        foreach (GameHex hex in gameHexDict.Values)
        {
            hex.OnTurnEnded(turnNumber);
        }
    }

    public void PrintGameBoard()
    {
        //terraintype
        GameHex test;
        for(int r = top; r <= bottom; r++){
            String mapRow = ""; 
            if (r%2 == 1)
            {
                mapRow += " ";
            }
            for (int q = left; q <= right; q++){
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
