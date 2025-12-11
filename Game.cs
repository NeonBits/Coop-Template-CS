using System.Collections.Generic;
using Godot;

public partial class Game : Node
{
  [Export] public PackedScene PlayerScene;

  private NetworkManager _networkManager;
  private Node _spawner;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _spawner = GetNode<Node>("Spawner");
    _networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    SpawnAllPlayers(_networkManager.Players);
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta)
  {
  }

  public void SpawnAllPlayers(Dictionary<long, string> playerDict)
  {
    if (playerDict.Count == 0)
    {
      // singleplayer
      playerDict = new Dictionary<long, string> { { 1L, "host" } };
    }

    var spawnpoints = _spawner.GetChildren();
    var pidList = new List<long>(playerDict.Keys);

    pidList.Sort();
    // this makes sure that the spawnpoints are same for all clients

    for (int i = 0; i < pidList.Count; i++)
    {
      var player = PlayerScene.Instantiate<Node2D>();
      var spawnpoint = (Node2D)spawnpoints[i];

      player.Position = spawnpoint.GlobalPosition;
      player.Name = pidList[i].ToString();
      player.SetMultiplayerAuthority((int)pidList[i]);
      AddChild(player);
    }
  }
}
