public void makeTestGameBoard(int right, int bottom)
{
    Dictionary<int, Player> playerDictionary = new();
    teamManager TeamManager = new TeamManager();
    Game game = new Game(playerDictionary, teamManager);
    
    GameBoard gameBoard = new GameBoard(game, 0, bottom, 0, right);
    Console.WriteLine(gameBoard.gameHexDict[new Hex(0, 0, 0)];)
    gameBoard.PrintGameBoard();        
}
