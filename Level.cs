using Godot;
using Godot.Collections;

public partial class Level : Node
{
  private Node _spawner;
  private Node _players;

  [Export]
  public PackedScene PlayerScene { get; set; }

  private MultiplayerSpawner _playerSpawner;
  private NetworkManager _networkManager;

  private bool _isDedicated = false;

  public override void _Ready()
  {
    _spawner = GetNode<Node>("Spawner");
    _players = GetNode<Node>("Players");
    _playerSpawner = GetNode<MultiplayerSpawner>("Players/PlayerSpawner");
    _networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    if (_networkManager.Players.Count == 0)
    {
      // singleplayer situation
      _networkManager.Players = new()
      {
        { 1, _networkManager.ConnectionType }
      };
    }

    foreach (var key in _networkManager.Players.Keys)
    {
      if (_networkManager.Players[key] == "dedicated")
      {
        _isDedicated = true;
        GD.Print("Running on Dedicated Server");
      }
    }

    // everyone needs to know this
    _playerSpawner.SpawnFunction = new Callable(this, nameof(SpawnPlayer));

    if (Multiplayer.IsServer())
    {
      SpawnAllPlayers();
    }
  }

  private void SpawnAllPlayers()
  {
    var spawnpoints = _spawner.GetChildren();
    var pidArray = new Array<long>();

    foreach (var key in _networkManager.Players.Keys)
      pidArray.Add(key);

    if (_isDedicated)
    {
      pidArray.Remove(1);
      // Removes dedicated server as a player
    }

    pidArray.Sort();
    // this makes sure that the spawnpoints are in order. Not needed, just debug

    for (int i = 0; i < pidArray.Count; i++)
    {
      long pid = pidArray[i];
      var spawnpointNode = spawnpoints[i] as Node2D;
      var spawnpoint = spawnpointNode.GlobalPosition;

      _playerSpawner.Spawn(new Array { pid, spawnpoint });
    }
  }

  private Node SpawnPlayer(Array data)
  {
    var player = PlayerScene.Instantiate();
    int pid = (int)data[0];
    Vector2 spawnpoint = (Vector2)data[1];

    if (player is Node2D p2d)
      p2d.Position = spawnpoint;

    player.Name = pid.ToString();

    return player;
  }
}
