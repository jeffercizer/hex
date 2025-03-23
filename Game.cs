using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;

[Serializable]
public class Game
{
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
}

// Tests


struct GameTests
{
    static public void SampleTest()
    {
        Game game;
        GameBoard mainBoard;
        StartGameTestHelper(out game, out mainBoard);
        game.AddPlayer(50.0f, 1);
        game.AddPlayer(50.0f, 2);
        TestPlayerRelations(game);
        City player1City = new City(1, 1, "MyCity", mainBoard.gameHexDict[new Hex(1, 1, -2)]);
        City player2City = new City(2, 2, "Team2City", mainBoard.gameHexDict[new Hex(15, 7, -22)]);

        //Yields
        TestCityYields(player1City);
        TestCityYields(player2City);

        //City Locations from player
        Tests.EqualHex("player1CityLocation", new Hex(1, 1, -2), game.playerDictionary[1].cityList[0].ourGameHex.hex);
        Tests.EqualHex("player2CityLocation", new Hex(15, 7, -22), game.playerDictionary[2].cityList[0].ourGameHex.hex);

        //Gamehex with city has District with 'City Center'
        if(game.playerDictionary[1].cityList[0].ourGameHex.district != null)
        {
            District? tempDistrict = game.playerDictionary[1].cityList[0].ourGameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1 | !tempDistrict.buildings.Contains(new Building("City Center")))
            {
                Complain("player1CityDistrictInvalid");
            }
        }

        if(game.playerDictionary[2].cityList[0].ourGameHex.district != null)
        {
            District tempDistrict = game.playerDictionary[2].cityList[0].ourGameHex.district;
            if(!tempDistrict.isCityCenter | !tempDistrict.isUrban | tempDistrict.buildings.Count != 1 | !tempDistrict.buildings.Contains(new Building("City Center")))
            {
                Complain("player1CityDistrictInvalid");
            }
        }

        //Yields
        TestCityYields(player1City);
        TestCityYields(player2City);



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
        TestCityYields(player1City);
        TestCityYields(player2City);

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

        //both scouts should now be spawned at city center TODO location check
        game.playerDictionary[1].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(5, 5, -10)], game.teamManager, false);

        game.playerDictionary[2].unitList[0].MoveTowards(game.mainGameBoard.gameHexDict[new Hex(5, 5, -10)], game.teamManager, false);

        //NEW LOCATION CHECK TODO

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

    static private void StartGameTestHelper(out Game game, out GameBoard mainBoard)
    {
        int top = 0;
        int bottom = 10;
        int left = 0;
        int right = 30;
        Dictionary<int, Player> playerDictionary = new();
        TurnManager turnManager = new TurnManager();
        TeamManager teamManager = new TeamManager();
        game = new Game(playerDictionary, turnManager, teamManager);
        turnManager.game = game;
        mainBoard = new GameBoard(game, bottom, right);
        Dictionary<Hex, GameHex> gameHexDict = new();
        for (int r = top; r <= bottom; r++){
            for (int q = left; q <= right; q++){
                if(r == bottom/2 && q > left + 2 && q < right - 2)
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), mainBoard, TerrainType.Mountain, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
                else
                {
                    gameHexDict.Add(new Hex(q, r, -q-r), new GameHex(new Hex(q, r, -q-r), mainBoard, TerrainType.Flat, TerrainTemperature.Grassland, new HashSet<FeatureType>()));
                }
            }
        }
        mainBoard.gameHexDict = gameHexDict;
        game.mainGameBoard = mainBoard;
    }

    public enum YieldType{
        Food,
        Production,
        Gold,
        Science,
        Culture,
        Happiness
    }

    static public void TestCityYields(City city)
    {
        TestCityYield(city.name+"CityFoodYield", city, YieldType.Food, 2);
        TestCityYield(city.name+"CityProductionYield", city, YieldType.Production, 2);
        TestCityYield(city.name+"CityGoldYield", city, YieldType.Gold, 8);
        TestCityYield(city.name+"CityScienceYield", city, YieldType.Science, 4);
        TestCityYield(city.name+"CityCultureYield", city, YieldType.Culture, 5);
        TestCityYield(city.name+"CityHappinessYield", city, YieldType.Happiness, 6);
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
