using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Game
{
    public GameBoard? mainGameBoard;
    public Dictionary<int, Player> playerDictionary;
    public HashSet<BuildingType> builtWonders;
    public TeamManager? teamManager;
    public TurnManager turnManager;
    public GraphicManager graphicManager;
    int currentID = 0;

    public Game(String mapName)
    {
        this.playerDictionary = new();
        this.turnManager = new TurnManager();
        this.teamManager = new TeamManager();
        turnManager.game = this;
        GameBoard mainBoard = new GameBoard(this, GetUniqueID(), 0, 0);
        Dictionary<Hex, GameHex> gameHexDict = new();
        String mapData = System.IO.File.ReadAllText(mapName+".map");
        List<String> lines = mapData.Split('\n').ToList();
        //file format is 1110 1110 (each 4 numbers are a single hex)
        // first number is terraintype, second number is terraintemp, last number is features, last is resource type
        // 0, luxury, bonus, city, iron, horses, coal, oil, uranium, (lithium?), futurething
        int r = 0;
        int q = 0;
        foreach (String line in lines)
        {
            q = 0;
            Queue<String> cells = new Queue<String>(line.Split(' ').ToList());
            int offset = r>>1;
            offset = (offset % cells.Count + cells.Count) % cells.Count; //negatives and overflow
            for (int i = 1; i < offset; i++)
            {
                cells.Enqueue(cells.Dequeue());
            }
            foreach (String cell in cells)
            {
                if(cell.Length >= 4)
                {
                    TerrainType terrainType = (TerrainType)int.Parse(cell[0].ToString());
                    TerrainTemperature terrainTemperature = (TerrainTemperature)int.Parse(cell[1].ToString());
                    HashSet<FeatureType> features = new();
                    //cell[2] == 0 means no features
                    if(int.Parse(cell[2].ToString()) == 1)
                    {
                        features.Add(FeatureType.Forest);
                    }
                    if(int.Parse(cell[2].ToString()) == 2)
                    {
                        features.Add(FeatureType.River);
                    }
                    if(int.Parse(cell[2].ToString()) == 3)
                    {
                        features.Add(FeatureType.Road);
                    }
                    if(int.Parse(cell[2].ToString()) == 4)
                    {
                        features.Add(FeatureType.Coral);
                    }
                    if(int.Parse(cell[2].ToString()) == 5)
                    {
                        //openslot //TODO
                    }
                    if(int.Parse(cell[2].ToString()) == 6)
                    {
                        features.Add(FeatureType.Forest);
                        features.Add(FeatureType.River);
                    }
                    if(int.Parse(cell[2].ToString()) == 7)
                    {
                        features.Add(FeatureType.River);
                        features.Add(FeatureType.Road);
                    }
                    if(int.Parse(cell[2].ToString()) == 8)
                    {
                        features.Add(FeatureType.Forest);
                        features.Add(FeatureType.Road);
                    }
                    if(int.Parse(cell[2].ToString()) == 9)
                    {
                        features.Add(FeatureType.Forest);
                        features.Add(FeatureType.River);
                        features.Add(FeatureType.Road);
                    }
                    // if(int.Parse(cell[2].ToString()) == 9)
                    // {
                    //     features.Add(FeatureType.Forest);
                    //     features.Add(FeatureType.River);
                    //     features.Add(FeatureType.Road);
                    // }
                    //fourth number is for resources
                    ResourceType resource = ResourceLoader.resourceNames[cell[3].ToString()];
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), mainBoard, terrainType, terrainTemperature, resource, features, new List<Unit>(), null));
                }
                q += 1;
            }
            r += 1;
        }
        mainBoard.bottom = r;
        mainBoard.right = q;
        mainBoard.gameHexDict = gameHexDict;
        mainGameBoard = mainBoard;
        builtWonders = new();
    }

    public bool TryGetGraphicManager(out GraphicManager manager)
    {
        if (graphicManager != null) {
            manager = graphicManager;
            return true;
        }
        manager = null;
        return false;
    }

    public void AssignGameBoard(GameBoard mainGameBoard)
    {
        this.mainGameBoard = mainGameBoard;
    }

    public void AssignTurnManager(TurnManager turnManager)
    {
        this.turnManager = turnManager;
    }

    public void AddPlayer(float startGold, int teamNum)
    {
        Player newPlayer = new Player(this, startGold, teamNum);
        playerDictionary.Add(teamNum, newPlayer);
    }

    public int GetUniqueID()
    {
        return currentID++;
    }
}

// Tests


struct GameTests
{
    static public Game MapLoadTest()
    {
        Game game = new Game("hex/sample");
        //test that hexes have features and such
        if(game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType != TerrainType.Flat)
        {
            Console.WriteLine("Expected Flat Got " + game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType);
        }
        if(game.mainGameBoard.gameHexDict[new Hex(5,3, -8)].terrainType != TerrainType.Flat)
        {
            Console.WriteLine("Expected Flat Got " + game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType);
        }
        game.AddPlayer(0.0f, 0);
        game.AddPlayer(50.0f, 1);
        game.AddPlayer(50.0f, 2);
        TestPlayerRelations(game, 1, 2, 50, 50);
        Hex player1CityLocation = new Hex(4, 3, -7);
        Hex player2CityLocation = new Hex(0, 10, -10);
        Unit player1Settler = new Unit(UnitType.Founder, game.GetUniqueID(), game.mainGameBoard.gameHexDict[player1CityLocation], 1);
        Unit player2Settler = new Unit(UnitType.Founder, game.GetUniqueID(), game.mainGameBoard.gameHexDict[player2CityLocation], 2);

        player1Settler.abilities.Find(ability => ability.name == "SettleCapitalAbility").ActivateAbility(player1Settler);
        
        player2Settler.abilities.Find(ability => ability.name == "SettleCapitalAbility").ActivateAbility(player2Settler);

        City player1City = game.playerDictionary[1].cityList[0];
        City player2City = game.playerDictionary[2].cityList[0];

        //Yields
        //TestHexYield(new Hex())
        TestCityYields(player1City, 5, 5, 5, 5, 5, 5);
        TestCityYields(player2City, 5, 5, 5, 5, 5, 5);

        //City Locations from player
        Tests.EqualHex("player1CityLocation", player1CityLocation, game.playerDictionary[1].cityList[0].gameHex.hex);
        Tests.EqualHex("player2CityLocation", player2CityLocation, game.playerDictionary[2].cityList[0].gameHex.hex);

        //Gamehex with city has District with 'City Center'
        if(game.playerDictionary[1].cityList[0].gameHex.district != null)
        {
            District? tempDistrict = game.playerDictionary[1].cityList[0].gameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1)
            {
                Complain("player1CityDistrictInvalid");
            }
        }
        foreach (Hex hex in game.mainGameBoard.gameHexDict.Keys)
        {
            if (game.mainGameBoard.gameHexDict[hex].district != null)
            {
                if (!hex.Equals(player1CityLocation) & !hex.Equals(player2CityLocation))
                {
                    Complain("playerCityMissingFromGameHex");
                }
            }
        }

        if(game.playerDictionary[2].cityList[0].gameHex.district != null)
        {
            District tempDistrict = game.playerDictionary[2].cityList[0].gameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1)
            {
                Complain("player2CityDistrictInvalid");
            }
        }

        //Yields
        TestCityYields(player1City, 5, 5, 5, 5, 5, 5);
        TestCityYields(player2City, 5, 5, 5, 5, 5, 5);

        return game;
    }

    static public Game TestScoutMovementCombat(Game game)
    {
        City player1City = game.playerDictionary[1].cityList[0];
        City player2City = game.playerDictionary[2].cityList[0];
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 10);
        player2City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player2City.gameHex, 10);

        if(player1City.productionQueue.Any())
        {
            if(player1City.productionQueue[0].name != "Scout" | player1City.productionQueue[0].productionLeft != 10)
            {
                Complain("player1CityFirstQueueNotScoutOrProductionWrong");
            }
        }
        if(player2City.productionQueue.Any() & (player2City.productionQueue[0].name != "Scout" | player2City.productionQueue[0].productionLeft != 10))
        {
            Complain("player2CityFirstQueueNotScoutOrProductionWrong");
        }

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(2);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn(); //rework this somehow smart so when all turns ended we proc


        //Yields
            TestCityYields(player1City, 5, 5, 5, 5, 5, 5);
            TestCityYields(player2City, 5, 5, 5, 5, 5, 5);

            if(!player1City.productionQueue.Any())
            {
                Complain("player1CityQueueEmpty");
            }
            if(!player2City.productionQueue.Any())
            {
                Complain("player2CityQueueEmpty");
            }

            player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 10);
            player1City.RemoveFromQueue(0); 

            if(player1City.productionQueue.Any())
            {
                if(player1City.productionQueue[0].name != "Scout" | player1City.productionQueue[0].productionLeft != 5)
                {
                    Complain("player1CityAddNewRemovePartial");
                }
            }
            else
            {
                Complain("player1CityQueueIsEmpty");
            }

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);

            game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn(); //rework this somehow smart so when all turns ended we proc



            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(4,3,-7));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(0, 10, -10));
            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(10, 10, -20)], game.teamManager, false);

            game.playerDictionary[2].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, false);



            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(5,3,-8));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(21, 9, -30));


            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn(); //rework this somehow smart so when all turns ended we proc

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(6,4,-10));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(19, 9, -28));

        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(6,6,-12));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(17, 10, -27));

        game.turnManager.StartNewTurn();
        
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(7,7,-14));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(15, 10, -25));

        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(7,9,-16));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(13, 10, -23));

        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(8, 10, -18));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));

        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(9, 10, -19));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));


        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            
            Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(10, 10, -20));
            Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));

        game.turnManager.StartNewTurn();

            //Scout1 Attack Scout2
            game.teamManager.DecreaseRelationship(1, 2, 100);
            game.teamManager.DecreaseRelationship(2, 1, 100);
            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, true);

            if(game.playerDictionary[1].unitList[0].health != 80.0f & game.playerDictionary[2].unitList[0].health != 75.0f)
            {
                Complain("IncorrectHealthAfterCombatPlayerDict");
            }
            if(game.mainGameBoard.gameHexDict[new Hex(10, 10, -20)].unitsList[0].health != 80.0f & game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)].unitsList[0].health != 75.0f)
            {
                Complain("IncorrectHealthAfterCombatGameHex");
            }
            Tests.EqualHex("Scout 1 Location PostCombat", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(10, 10, -20));
            Tests.EqualHex("Scout 2 Location PostCombat", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));

            //try attacking again
            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, true);
            Tests.EqualHex("Scout 1 Location PostFailCombat1", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(10, 10, -20));
            Tests.EqualHex("Scout 2 Location PostFailCombat1", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));

            if(game.playerDictionary[1].unitList[0].health != 80.0f & game.playerDictionary[2].unitList[0].health != 75.0f)
            {
                Complain("IncorrectHealthAfterFailCombatPlayerDict");
            }
            if(game.mainGameBoard.gameHexDict[new Hex(10, 10, -20)].unitsList[0].health != 80.0f & game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)].unitsList[0].health != 75.0f)
            {
                Complain("IncorrectHealthAfterFailCombatGameHex");
            }

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);
        
        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, true);
            Tests.EqualHex("Scout 1 Location PostFailCombat2", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(10, 10, -20));
            Tests.EqualHex("Scout 2 Location PostFailCombat2", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));
            
            if(game.playerDictionary[1].unitList[0].health != 60.0f & game.playerDictionary[2].unitList[0].health != 50.0f)
            {
                Complain("IncorrectHealthAfterCombatPlayerDict");
            }
            if(game.mainGameBoard.gameHexDict[new Hex(10, 10, -20)].unitsList[0].health != 60.0f & game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)].unitsList[0].health != 50.0f)
            {
                Complain("IncorrectHealthAfterCombatGameHex");
            }

        game.turnManager.StartNewTurn();

            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);
            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, true);
            Tests.EqualHex("Scout 1 Location PostFailCombat3", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(10, 10, -20));
            Tests.EqualHex("Scout 2 Location PostFailCombat3", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));
            
            if(game.playerDictionary[1].unitList[0].health != 40.0f & game.playerDictionary[2].unitList[0].health != 25.0f)
            {
                Complain("IncorrectHealthAfterCombatPlayerDict");
            }

        game.turnManager.StartNewTurn();
        
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);

            game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)], game.teamManager, true);
            Tests.EqualHex("Scout 1 Location PostSuccessCombat", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(11, 10, -21));
            //Tests.EqualHex("Scout 2 Location PostSuccessCombat", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(11, 10, -21));

            if(game.playerDictionary[2].unitList.Any())
            {
                Complain("UnitInPlayerDictionary");
            }
            if(game.playerDictionary[1].unitList[0].health != 20.0f)
            {
                Complain("IncorrectHealthAfterCombatPlayerDict");
            }
            if(game.mainGameBoard.gameHexDict[new Hex(11, 10, -21)].unitsList.Count > 1)
            {
                Complain("TooManyUnitsOnHex");
            }

        game.turnManager.StartNewTurn();
        Console.WriteLine("TestScoutMovementCombat Finished");
        return game;
    }

    static public Game TestMassScoutBuild(Game game)
    {
        City player1City = game.playerDictionary[1].cityList[0];
        City player2City = game.playerDictionary[2].cityList[0];
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 2);
        int targetTurn = 30;
        for(int i = 0; i < targetTurn; i++)
        {
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(0);
            game.turnManager.StartNewTurn();
        }
        if(player1City.productionQueue.Any())
        {
            Complain("MassScout-CityHasQueueLeft");
        }
        if(game.playerDictionary[1].unitList.Count != 26)
        {
            Complain("MassScout-PlayerUnitCountWrong Expected: 26 Got: " + game.playerDictionary[1].unitList.Count);
        }
        Console.WriteLine("TestMassScoutBuild Finished");
        return game;
    }

    static public Game TestOverflowProduction(Game game)
    {
        City player1City = game.playerDictionary[1].cityList[0];
        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 1);

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();

        if(player1City.productionQueue.Any())
        {
            Complain("OverflowProduction-CityHasQueueLeft");
        }

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();

        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 3);

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();

        if(player1City.productionQueue.Any())
        {
            Complain("OverflowProduction-CityHasQueueLeft");
        }

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();

        player1City.AddToQueue("Scout", BuildingType.None, UnitType.Scout, player1City.gameHex, 4);
        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();
        if(player1City.productionQueue.Any())
        {
            Complain("OverflowProduction-CityHasQueueLeft");
        }

        Console.WriteLine("TestOverflowProduction Finished");

        return game;
    }

    static public Game TestCityExpand(Game game)
    {
        City player1City = game.playerDictionary[1].cityList[0];
        City player2City = game.playerDictionary[2].cityList[0];

        if(game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType != TerrainType.Flat)
        {
            Console.WriteLine("Expected Flat Got " + game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType);
        }
        if(game.mainGameBoard.gameHexDict[new Hex(5,3, -8)].terrainType != TerrainType.Flat)
        {
            Console.WriteLine("Expected Flat Got " + game.mainGameBoard.gameHexDict[new Hex(4,3, -7)].terrainType);
        }

        //what hex we use from validHexes should come from the user
        List<Hex> validHexes = player1City.ValidUrbanBuildHexes(BuildingLoader.buildingsDict[BuildingType.Granary].TerrainTypes);
        player1City.AddToQueue(BuildingLoader.buildingNames[BuildingType.Granary], BuildingType.Granary, UnitType.None, game.mainGameBoard.gameHexDict[validHexes[0]], BuildingLoader.buildingsDict[BuildingType.Granary].ProductionCost);
                
        //what hex we use from validHexes should come from the user
        validHexes = player2City.ValidUrbanBuildHexes(BuildingLoader.buildingsDict[BuildingType.StoneCutter].TerrainTypes);
        player2City.AddToQueue(BuildingLoader.buildingNames[BuildingType.StoneCutter], BuildingType.StoneCutter, UnitType.None, game.mainGameBoard.gameHexDict[validHexes[0]], BuildingLoader.buildingsDict[BuildingType.StoneCutter].ProductionCost);

        int count = 0;
        while(count < (BuildingLoader.buildingsDict[BuildingType.StoneCutter].ProductionCost/player2City.yields.production))
        {
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(0);
            game.turnManager.StartNewTurn();
            count++;
        }

        if(player1City.productionQueue.Any())
        {
            Complain("CityHasQueueLeft");
        }

        Console.WriteLine("TestCityExpand Finished");

        return game;
    }

    static public Game TestSlingerCombat()
    {
        Game game = new Game("sample");

        game.AddPlayer(0.0f, 0);
        game.AddPlayer(50.0f, 1);
        game.AddPlayer(50.0f, 2);
        TestPlayerRelations(game, 1, 2, 50, 50);
        Hex player1CityLocation = new Hex(4, 8, -12);
        Hex player2CityLocation = new Hex(5, 10, -15);
        Unit player1Settler = new Unit(UnitType.Founder, game.GetUniqueID(), game.mainGameBoard.gameHexDict[player1CityLocation], 1);
        Unit player2Settler = new Unit(UnitType.Founder, game.GetUniqueID(), game.mainGameBoard.gameHexDict[player2CityLocation], 2);

        player1Settler.abilities.Find(ability => ability.name == "SettleCapitalAbility").ActivateAbility(player1Settler);
        
        player2Settler.abilities.Find(ability => ability.name == "SettleCapitalAbility").ActivateAbility(player2Settler);

        City player1City = game.playerDictionary[1].cityList[0];
        City player2City = game.playerDictionary[2].cityList[0];
        player1City.AddUnitToQueue(UnitType.Slinger);
        player2City.AddUnitToQueue(UnitType.Slinger);
        int count = 0;
        while(count < 5)
        {
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);
            game.turnManager.StartNewTurn();
            count++;
        }
        game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(5, 9, -14)], game.teamManager, false);
        count = 0;
        while(count < 5)
        {
            game.turnManager.EndCurrentTurn(1);
            game.turnManager.EndCurrentTurn(2);
            game.turnManager.EndCurrentTurn(0);
            game.turnManager.StartNewTurn();
            count++;
        }
        Tests.EqualHex("Slinger 1 Location", game.playerDictionary[1].unitList[0].gameHex.hex, new Hex(5, 9, -14));
        Tests.EqualHex("Slinger 2 Location", game.playerDictionary[2].unitList[0].gameHex.hex, new Hex(5, 10, -15));

        if(game.playerDictionary[1].unitList[0].abilities[0].ActivateAbility(game.playerDictionary[1].unitList[0], game.mainGameBoard.gameHexDict[new Hex(5, 10, -15)]))
        {
            Complain("Attacked Neutral or Ally");
        }

        game.teamManager.DecreaseRelationship(1, 2, 100);
        game.teamManager.DecreaseRelationship(2, 1, 100);

        TestPlayerRelations(game, 1, 2, 0, 0);

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(2);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();


        //the way I imagine abilities will work is we iterate over the list to get each and put it into the list of buttons that will call the ability, each ability having an icon and such.
        //main concern being that abilities being in different orders mean they are in different spots on the UI which is bad, so need assigned spots depending, thinking Starcraft inspo, grid bottom rightish
        //for testing we just call it directly since references could be made via name but those also are bound to change
        //abilities use 
        game.playerDictionary[1].unitList[0].abilities[0].ActivateAbility(game.playerDictionary[1].unitList[0], game.mainGameBoard.gameHexDict[new Hex(5, 10, -15)]);
        if(game.playerDictionary[2].cityList[0].districts[0].health != 135)
        {
            Complain("District didn't take 15 damage");
        }
        if(game.playerDictionary[2].unitList[0].health != 100)
        {
            Complain("Unit Took damage while in district");
        }

        game.playerDictionary[2].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(6, 9, -15)], game.teamManager, false);


        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(2);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn();

        game.playerDictionary[1].unitList[0].abilities[0].ActivateAbility(game.playerDictionary[1].unitList[0], game.mainGameBoard.gameHexDict[new Hex(6, 9, -15)]);
        
        if(game.playerDictionary[2].unitList[0].health != 85)
        {
            Complain("Unit didnt take correct damage");
        }

        Console.WriteLine("TestSlingerCombat Finished");
        return game;
    }


    static public void TestAll()
    {
        Game game = MapLoadTest();
        TestScoutMovementCombat(game);
        TestMassScoutBuild(game);
        TestOverflowProduction(game);
        //TODO tests
        //TestResearchBuildAndEffects();
        TestCityExpand(game); //not finished
        
        TestSlingerCombat(); //not finished
    }

    static private void TestPlayerRelations(Game game, int team1, int team2, float expected1, float expected2)
    {
        if(game.teamManager.GetRelationship(team1, team2) != expected1)
        {
            Complain("PlayerRelations12 " + game.teamManager.GetRelationship(1, 2));
        }
        if(game.teamManager.GetRelationship(team2, team1) != expected2)
        {
            Complain("PlayerRelations21 " + game.teamManager.GetRelationship(2, 1));
        }
    }

    public enum YieldType{
        Food,
        Production,
        Gold,
        Science,
        Culture,
        Happiness
    }

    static public void TestCityYields(City city, int foodExpected, int productionExpected, int goldExpected, int scienceExpected, int cultureExpected, int happinessExpected)
    {
        TestCityYield(city.name+"Cityyields.food", city, YieldType.Food, foodExpected);
        TestCityYield(city.name+"Cityyields.production", city, YieldType.Production, productionExpected);
        TestCityYield(city.name+"Cityyields.gold", city, YieldType.Gold, goldExpected);
        TestCityYield(city.name+"Cityyields.science", city, YieldType.Science, scienceExpected);
        TestCityYield(city.name+"Cityyields.culture", city, YieldType.Culture, cultureExpected);
        TestCityYield(city.name+"Cityyields.happiness", city, YieldType.Happiness, happinessExpected);
    }

    static public void TestCityYield(String testName, City testCity, YieldType yieldType, float expectedYield)
    {
        if(yieldType == YieldType.Food & testCity.yields.food != expectedYield)
        {
            Complain(testName + " " + testCity.yields.food);
        }
        if(yieldType == YieldType.Production & testCity.yields.production != expectedYield)
        {
            Complain(testName + " " + testCity.yields.production);
        }
        if(yieldType == YieldType.Gold & testCity.yields.gold != expectedYield)
        {
            Complain(testName + " " + testCity.yields.gold);
        }
        if(yieldType == YieldType.Science & testCity.yields.science != expectedYield)
        {
            Complain(testName + " " + testCity.yields.science);
        }
        if(yieldType == YieldType.Culture & testCity.yields.culture != expectedYield)
        {
            Complain(testName + " " + testCity.yields.culture);
        }
        if(yieldType == YieldType.Happiness & testCity.yields.happiness != expectedYield)
        {
            Complain(testName + " " + testCity.yields.happiness);
        }
    }



    static public void Complain(String name)
    {
        Console.WriteLine("FAIL " + name);
    }

}


struct GameMain
{
    static public void Main()
    {
        GameTests.TestAll();
    }
}

//things to test
//game board has been mostly tested previously and implicitly in other tests
//building: yields, cost (prod,gold), building effect, recalculate
//city spawn, yields, productionQueue, turns, change team, change name, getbuilding for rural, expandto, buildon, valid hexes
//district spawn building, claim hexes, recalculate yields, isUrban
//gamehex ownership/claim hex, spawn unit
//player visibility, seen, unitList, cityList
//unit spawn, move, attack, die, kill, vision, team
