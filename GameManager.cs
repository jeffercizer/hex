using Godot;
using NetworkMessages;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using Godot;
using System.Linq;


public partial class GameManager: Node
{
    public static GameManager instance;
    public Game game;
    

    public GameManager()
    {
        instance = this;
        Global.gameManager = this;
    }


    public void SaveGame()
    {
        String filePath = "C:/Users/jeffe/Desktop/Stuff/HexGame/game_data.dat";
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            game.Serialize(writer);
        }
        GD.Print("Game data saved!");
    }

    public Game LoadGame(String filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            return Game.Deserialize(reader);
        }
    }

    public void startGame()
    {
        Layout pointyReal = new Layout(Layout.pointy, new Point(10, 10), new Point(0, 0));
        Layout pointy = new Layout(Layout.pointy, new Point(-10, 10), new Point(0, 0));
        Global.layout = pointy;
        //game = GameTests.GameStartTest();
        //game = GameTests.MapLoadTest();
        game = GameTests.TestSlingerCombat();
        Global.graphicManager = new GraphicManager(game, pointy);
        Global.graphicManager.Name = "GraphicManager";
        //game = GameTests.TestMassScoutBuild(game);
        //game = GameTests.TestScoutMovementCombat(game);
        Camera3D camera3D = GetChild<Camera3D>(0);//TODO
        Global.camera = camera3D as Camera;
        Global.camera.SetGame(game);
        Global.camera.SetGraphicManager(Global.graphicManager);
        AddSibling(Global.graphicManager);
    }

    public override void _PhysicsProcess(double delta)
    {
        game.turnManager.EndCurrentTurn(0);
        game.turnManager.EndCurrentTurn(2);
        List<int> waitingForPlayerList = game.turnManager.CheckTurnStatus();
        if (!waitingForPlayerList.Any())
        {
            game.turnManager.StartNewTurn();
            Global.graphicManager.StartNewTurn();
        }
        else
        {
            //push waitingForPlayerList to UI
        }
    }

    public void MoveUnit(int unitID, Hex Target, bool local = true)
    {
        if (local)
        {
            Global.networkPeer.CommandAllPeers(CommandParser.ConstructMoveUnitCommand(unitID, Target));
            return;
        }


        Unit unit = SearchUnitByID(unitID);
        if (unit == null)
        {
            Global.debugLog("Unit is null"); //TODO - Potential Desync
            return;
        }

        GameHex target = game.mainGameBoard.gameHexDict[Target];
        if (target == null)
        {
            Global.debugLog("Target hex is null");//TODO - Potential Desync
            return;
        }

        try
        {
            unit.TryMoveToGameHex(target, game.teamManager);
        }
        catch (Exception e)
        {
            Global.debugLog("Error moving unit: " + e.Message); //TODO - Potential Desync
            return;
        }
    }

    private Unit SearchUnitByID(int unitID)
    {
        foreach (Player player in game.playerDictionary.Values)
        {
            foreach (Unit unit in player.unitList)
            {
                if (unit.id == unitID)
                {
                    return unit;
                }
            }
        }
        return null;
    }

    public void ActivateAbility(int unitID, string AbilityName, Hex Target, bool local = true) 
    {
        if (local)
        {
            Global.networkPeer.CommandAllPeers(CommandParser.ConstructActivateAbilityCommand(unitID, AbilityName, Target));
            return;
        }

        Unit unit = SearchUnitByID(unitID);
        if (unit == null)
        {
            Global.debugLog("Unit is null"); //TODO - Potential Desync
            return;
        }
        GameHex target = game.mainGameBoard.gameHexDict[Target];
        if (target == null)
        {
            Global.debugLog("Target hex is null");//TODO - Potential Desync
            return;
        }

        UnitAbility ability = unit.abilities.Find(x => x.name == AbilityName);
        if (ability == null)
        {
            Global.debugLog("Ability is null"); //TODO - Potential Desync
            return;
        }

        try
        {
            ability.ActivateAbility(target);
        }
        catch (Exception e)
        {
            Global.debugLog("Error activating ability: " + e.Message); //TODO - Potential Desync
            return;
        }
    }

    public void ChangeProductionQueue(City city, List<ProductionQueueType> queue, bool local = true)
    {
        if (local)
        {
            Global.networkPeer.CommandAllPeers(CommandParser.ConstructChangeProductionQueueCommand(city,queue));
            return;
        }

        if (city == null)
        {
            Global.debugLog("City is null"); //TODO - Potential Desync
            return;
        }

        if (queue == null)
        {
            Global.debugLog("Queue is null"); //TODO - Potential Desync
            return;
        }

        try
        {
            //city.SetProductionQueue(queue);
        }
        catch (Exception e)
        {
            Global.debugLog("Error changing production queue: " + e.Message); //TODO - Potential Desync
            return;
        }
    }
}
