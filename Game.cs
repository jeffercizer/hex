using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Game
{
    public Game(String mapName)
    {
        int top = 0;
        int left = 0;
        this.playerDictionary = new();
        this.turnManager = new TurnManager();
        this.teamManager = new TeamManager();
        turnManager.game = this;
        
        Dictionary<Hex, GameHex> gameHexDict = new();
        String mapData = File.ReadAllText(mapName+".map");
        List<String> lines = mapData.Split('\n').ToList();
        //file format is 1110 1110 (each 4 numbers are a single hex)
        // first number is terraintype, second number is terraintemp, last number is features, last is resource type
        // 0, luxury, bonus, city, iron, horses, coal, oil, uranium, (lithium?), futurething
        int r = 0;
        int q = 0;
        foreach (String line in lines)
        {
            q = 0;
            Queue<String> cells = new Queue(line.Split(' ').ToList());
            int offset = r>>1;
            offset = (offset % cells.Count + cells.Count) % cells.Count; //negatives and overflow
            for (int i = 0; i < offset; i++)
            {
                cells.Enqueue(cells.Dequeue());
            }
            foreach (String cell in cells)
            {
                TerrainType terrainType = (TerrainType)cell[0];
                TerrainTemperature terrainTemperature = (TerrainTemperature)cell[1];
                List<FeatureType> features = new();
                //cell[2] == 0 means no features
                if(cell[2] == 1)
                {
                    features.Add(FeatureType.Forest);
                }
                if(cell[2] == 2)
                {
                    features.Add(FeatureType.River);
                }
                if(cell[2] == 3)
                {
                    features.Add(FeatureType.Road);
                }
                if(cell[2] == 4)
                {
                    features.Add(FeatureType.Coral);
                }
                if(cell[2] == 5)
                {
                    //openslot //TODO
                }
                if(cell[2] == 6)
                {
                    features.Add(FeatureType.Forest);
                    features.Add(FeatureType.River);
                }
                if(cell[2] == 7)
                {
                    features.Add(FeatureType.River);
                    features.Add(FeatureType.Road);
                }
                if(cell[2] == 8)
                {
                    features.Add(FeatureType.Forest);
                    features.Add(FeatureType.Road);
                }
                if(cell[2] == 9)
                {
                    features.Add(FeatureType.Forest);
                    features.Add(FeatureType.River);
                    features.Add(FeatureType.Road);
                }
                if(cell[2] == 9)
                {
                    features.Add(FeatureType.Forest);
                    features.Add(FeatureType.River);
                    features.Add(FeatureType.Road);
                }
                //fourth number is for resources
                ResourceType resource = (ResourceType)cell[3];
                gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), mainBoard, terrainType, terrainTemp, features));
                q += 1;
            }
            r += 1;
        }
        int bottom = r; //CHECK
        int right = q; //CHECK
        GameBoard mainBoard = new GameBoard(this, bottom, right);
        mainBoard.gameHexDict = gameHexDict;
        this.mainGameBoard = mainBoard;
    }
    public Game(int boardHeight, int boardWidth)
    {
        int top = 0;
        int bottom = boardHeight;
        int left = 0;
        int right = boardWidth;
        this.playerDictionary = new();
        this.turnManager = new TurnManager();
        this.teamManager = new TeamManager();
        turnManager.game = this;
        GameBoard mainBoard = new GameBoard(this, bottom, right);
        this.mainGameBoard = mainBoard;
        Dictionary<Hex, GameHex> gameHexDict = new();
        Random rand = new();
        for (int r = top; r <= bottom; r++){
            for (int q = left; q <= right; q++){
                TerrainTemperature terrainTemp = TerrainTemperature.Arctic;
                HashSet<FeatureType> features = new HashSet<FeatureType>();
                float interval = bottom/6;
                if((r >= bottom - interval & r <= bottom) | (r >= top & r <= top + interval))
                {
                    terrainTemp = TerrainTemperature.Arctic;
                }
                if((r >= bottom - 2 * interval & r <= bottom - interval) | (r >= top + interval & r <= top + 2 * interval))
                {
                    terrainTemp = TerrainTemperature.Tundra;
                }
                if((r >= bottom - 3 * interval & r <= bottom - 2 * interval) | (r >= top + 2 * interval & r <= top + 3 * interval))
                {
                    terrainTemp = TerrainTemperature.Grassland;
                }
                if((r >= bottom - 4 * interval & r <= bottom - 3 * interval) | (r >= top + 3 * interval & r <= top + 4 * interval))
                {
                    terrainTemp = TerrainTemperature.Plains;
                }
                if((r >= bottom - 5 * interval & r <= bottom - 4 * interval) | (r >= top + 3 * interval & r <= top + 4 * interval))
                {
                    terrainTemp = TerrainTemperature.Desert;
                }

                if(r == bottom - 2 | r == 2 & (r != 0 | r != bottom | q != right | q != 0))
                {
                    features.Add(FeatureType.Road);
                }
                if(r < bottom - 2 & q > 2 & r > bottom/2 & q < right - 2 & (r != 0 | r != bottom | q != right | q != 0))
                {
                    features.Add(FeatureType.Forest);
                }
                if(q == right/2 & (r != 0 | r != bottom | q != right | q != 0))
                {
                    features.Add(FeatureType.River);
                }
                if(r == bottom/2 |q == right/2)
                {

                }

                TerrainType terrainType = TerrainType.Ocean;
                if(r == bottom/2 && q > left + 2 && q < right - 2 & (r != 0 | r != bottom | q != right | q != 0))
                {
                    terrainType = TerrainType.Mountain;
                }
                else if(r%2 == 0 | q%2 == 0 & (r != 0 | r != bottom | q != right | q != 0))
                {
                    terrainType = TerrainType.Flat;
                }
                else if(r != 0 | r != bottom | q != right | q != 0)
                {
                    terrainType = TerrainType.Rough;
                }
                else
                {
                    terrainType = TerrainType.Coast;
                }
                gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), mainBoard, terrainType, terrainTemp, features));
            }
        }
        mainBoard.gameHexDict = gameHexDict;
    }

    public Game(TeamManager teamManager)
    {
        this.teamManager = teamManager;
        this.playerDictionary = new();
    }
    public Game(Dictionary<int, Player> playerDictionary, TurnManager turnManager, TeamManager teamManager)
    {
        this.playerDictionary = playerDictionary;
        this.turnManager = turnManager;
        this.teamManager = teamManager;
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
    public GameBoard? mainGameBoard;
    public Dictionary<int, Player> playerDictionary;
    public TeamManager? teamManager;
    public TurnManager turnManager;
    public ResourceLoader resourceLoader = new();
}

// Tests


struct GameTests
{
    static public Game SampleTest()
    {
        Game game = new(10, 30);
        // GameBoard mainBoard;
        // StartGameTestHelper(out game, out mainBoard);
        game.AddPlayer(50.0f, 1);
        game.AddPlayer(50.0f, 2);
        TestPlayerRelations(game);
        City player1City = new City(1, 1, "MyCity", game.mainGameBoard.gameHexDict[new Hex(2, 2, -4)]);
        City player2City = new City(2, 2, "Team2City", game.mainGameBoard.gameHexDict[new Hex(15, 7, -22)]);

        //Yields
        TestCityYields(player1City, 2, 2, 2, 4, 5, 6);
        TestCityYields(player2City, 2, 2, 2, 4, 5, 6);

        //City Locations from player
        Tests.EqualHex("player1CityLocation", new Hex(2, 2, -4), game.playerDictionary[1].cityList[0].ourGameHex.hex);
        Tests.EqualHex("player2CityLocation", new Hex(15, 7, -22), game.playerDictionary[2].cityList[0].ourGameHex.hex);

        //Gamehex with city has District with 'City Center'
        if(game.playerDictionary[1].cityList[0].ourGameHex.district != null)
        {
            District? tempDistrict = game.playerDictionary[1].cityList[0].ourGameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1)
            {
                Complain("player1CityDistrictInvalid");
            }
        }
        foreach (Hex hex in game.mainGameBoard.gameHexDict.Keys)
        {
            if (game.mainGameBoard.gameHexDict[hex].district != null)
            {
                Console.Write(game.mainGameBoard.gameHexDict[hex].district.ourGameHex.hex.q +" ");
                Console.WriteLine(game.mainGameBoard.gameHexDict[hex].district.ourGameHex.hex.r);
            }
        }

        if(game.playerDictionary[2].cityList[0].ourGameHex.district != null)
        {
            District tempDistrict = game.playerDictionary[2].cityList[0].ourGameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1)
            {
                Complain("player2CityDistrictInvalid");
            }
        }

        //Yields
        TestCityYields(player1City, 2, 2, 2, 4, 5, 6);
        TestCityYields(player2City, 2, 2, 2, 4, 5, 6);



        player1City.AddToQueue("Scout", ProductionType.Unit, player1City.ourGameHex, 4, false);
        player2City.AddToQueue("Scout", ProductionType.Unit, player2City.ourGameHex, 4, false);

        if(player1City.productionQueue.Any())
        {
            if(player1City.productionQueue[0].name != "Scout" | player1City.productionQueue[0].productionLeft != 4)
            {
                Complain("player1CityFirstQueueNotScoutOrProductionWrong");
            }
        }
        if(player2City.productionQueue.Any() & (player2City.productionQueue[0].name != "Scout" | player2City.productionQueue[0].productionLeft != 4))
        {
            Complain("player2CityFirstQueueNotScoutOrProductionWrong");
        }

        
        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(2);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn(); //rework this somehow smart so when all turns ended we proc


        //Yields
        TestCityYields(player1City, 2, 2, 2, 4, 5, 6);
        TestCityYields(player2City, 2, 2, 2, 4, 5, 6);

        if(!player1City.productionQueue.Any())
        {
            Complain("player1CityQueueEmpty");
        }
        if(!player2City.productionQueue.Any())
        {
            Complain("player2CityQueueEmpty");
        }

        player1City.AddToQueue("Scout", ProductionType.Unit, player1City.ourGameHex, 4, false);
        player1City.RemoveFromQueue(0); 

        if(player1City.productionQueue.Any())
        {
            if((player1City.productionQueue[0].name != "Scout" | player1City.productionQueue[0].productionLeft != 2))
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



        Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].currentGameHex.hex, new Hex(4,3,-7));
        Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].currentGameHex.hex, new Hex(11,8,-19));
        game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(5, 4, -9)], game.teamManager, false);

        game.playerDictionary[2].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(5, 4, -9)], game.teamManager, false);



        Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].currentGameHex.hex, new Hex(4,3,-7));
        Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].currentGameHex.hex, new Hex(11,8,-19));

        Console.Write(game.playerDictionary[1].unitList[0].currentGameHex.hex.q+" ");
        Console.WriteLine(game.playerDictionary[1].unitList[0].currentGameHex.hex.r);

        Console.Write(game.playerDictionary[2].unitList[0].currentGameHex.hex.q+" ");
        Console.WriteLine(game.playerDictionary[2].unitList[0].currentGameHex.hex.r);

        game.turnManager.EndCurrentTurn(1);
        game.turnManager.EndCurrentTurn(2);
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.StartNewTurn(); //rework this somehow smart so when all turns ended we proc

        Console.Write(game.playerDictionary[1].unitList[0].currentGameHex.hex.q+" ");
        Console.WriteLine(game.playerDictionary[1].unitList[0].currentGameHex.hex.r);

        Console.Write(game.playerDictionary[2].unitList[0].currentGameHex.hex.q+" ");
        Console.WriteLine(game.playerDictionary[2].unitList[0].currentGameHex.hex.r);

        Tests.EqualHex("Scout 1 Location", game.playerDictionary[1].unitList[0].currentGameHex.hex, new Hex(4,3,-7));
        Tests.EqualHex("Scout 2 Location", game.playerDictionary[2].unitList[0].currentGameHex.hex, new Hex(11,8,-19));


        return game;
    }


    static public void TestAll()
    {
        GameTests.SampleTest();
    }

    static private void TestPlayerRelations(Game game)
    {
        if(game.teamManager.GetRelationship(1, 2) != 50)
        {
            Complain("PlayerRelations12 " + game.teamManager.GetRelationship(1, 2));
        }
        if(game.teamManager.GetRelationship(2, 1) != 50)
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
        TestCityYield(city.name+"CityFoodYield", city, YieldType.Food, foodExpected);
        TestCityYield(city.name+"CityProductionYield", city, YieldType.Production, productionExpected);
        TestCityYield(city.name+"CityGoldYield", city, YieldType.Gold, goldExpected);
        TestCityYield(city.name+"CityScienceYield", city, YieldType.Science, scienceExpected);
        TestCityYield(city.name+"CityCultureYield", city, YieldType.Culture, cultureExpected);
        TestCityYield(city.name+"CityHappinessYield", city, YieldType.Happiness, happinessExpected);
    }

    static public void TestCityYield(String testName, City testCity, YieldType yieldType, float expectedYield)
    {
        if(yieldType == YieldType.Food & testCity.foodYield != expectedYield)
        {
            Complain(testName + " " + testCity.foodYield);
        }
        if(yieldType == YieldType.Production & testCity.productionYield != expectedYield)
        {
            Complain(testName + " " + testCity.productionYield);
        }
        if(yieldType == YieldType.Gold & testCity.goldYield != expectedYield)
        {
            Complain(testName + " " + testCity.goldYield);
        }
        if(yieldType == YieldType.Science & testCity.scienceYield != expectedYield)
        {
            Complain(testName + " " + testCity.scienceYield);
        }
        if(yieldType == YieldType.Culture & testCity.cultureYield != expectedYield)
        {
            Complain(testName + " " + testCity.cultureYield);
        }
        if(yieldType == YieldType.Happiness & testCity.happinessYield != expectedYield)
        {
            Complain(testName + " " + testCity.happinessYield);
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
