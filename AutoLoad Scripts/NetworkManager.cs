using System.Collections.Generic;
using Godot;

public partial class NetworkManager : Node
{
  // Autoload named Network Manager

  // These signals can be connected to by a UI lobby scene or the game scene.
  [Signal]
  public delegate void PlayerConnectedEventHandler(long peer_id, string player_info);
  [Signal]
  public delegate void PlayerDisconnectedEventHandler(long peer_id);
  [Signal]
  public delegate void ServerDisconnectedEventHandler();

  public const int PORT = 36666;
  public const string DEFAULT_SERVER_IP = "127.0.0.1"; // IPv4 localhost, replace with server
  public const int MAX_CONNECTIONS = 5;

  // This will contain player info for every player,
  // with the keys being each player's unique IDs.
  public Dictionary<long, string> Players = [];

  public string ConnectionType = "player";

  public override void _Ready()
  {
    Multiplayer.PeerConnected += OnPlayerConnected;
    Multiplayer.PeerDisconnected += OnPlayerDisconnected;
    Multiplayer.ConnectedToServer += OnConnectedOk;
    Multiplayer.ConnectionFailed += OnConnectedFail;
    Multiplayer.ServerDisconnected += OnServerDisconnected;

    if (OS.HasFeature("dedicated_server"))
    {
      ConnectionType = "dedicated";
      CreateGame();
    }
  }

  public Error JoinGame(string address = "")
  {
    GD.Print("Joining");
    if (string.IsNullOrEmpty(address))
      address = DEFAULT_SERVER_IP;

    ENetMultiplayerPeer peer = new();
    var error = peer.CreateClient(address, PORT);
    if (error != Error.Ok)
    {
      GD.Print("Couldnt join");
      return error;
    }
    Multiplayer.MultiplayerPeer = peer;
    return Error.Ok;
  }

  public Error CreateGame()
  {
    ENetMultiplayerPeer peer = new();
    var error = peer.CreateServer(PORT, MAX_CONNECTIONS);
    if (error != Error.Ok)
      return error;

    Multiplayer.MultiplayerPeer = peer;

    Players[1] = ConnectionType;
    EmitSignal(SignalName.PlayerConnected, 1L, ConnectionType);

    return Error.Ok;
  }

  //func remove_multiplayer_peer():
  //  multiplayer.multiplayer_peer = null
  //  players.clear()

  // When a peer connects, send them my player info.
  // This allows transfer of all desired data for each player, not only the unique ID.
  private void OnPlayerConnected(long id)
    => RpcId(id, nameof(RegisterPlayer), ConnectionType);

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RegisterPlayer(string newPlayerInfo)
  {
    int newPlayerId = Multiplayer.GetRemoteSenderId();
    Players[newPlayerId] = newPlayerInfo;
    EmitSignal(SignalName.PlayerConnected, newPlayerId, newPlayerInfo);
  }

  private void OnPlayerDisconnected(long id)
  {
    GD.Print($"Player {id} Left");
    Players.Remove(id);
    EmitSignal(SignalName.PlayerDisconnected, id);
  }

  private void OnConnectedOk()
  {
    GD.Print("Connected");
    int peerId = Multiplayer.GetUniqueId();
    Players[peerId] = ConnectionType;
    EmitSignal(SignalName.PlayerConnected, peerId, ConnectionType);
  }

  public void OnConnectedFail()
  {
    GD.Print("Couldn't Join");
    Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
  }

  private void OnServerDisconnected()
  {
    GD.Print("Server Disconnected");
    Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();
    Players.Clear();
    EmitSignal(SignalName.ServerDisconnected);
  }
}
