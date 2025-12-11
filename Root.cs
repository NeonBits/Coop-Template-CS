using Godot;

public partial class Root : Node
{
  [Export] public PackedScene LevelScene;
  private int _dedicatedCorrection = 0;

  private Timer _connectionTimer;
  private Label _playersJoinedLabel;
  private Button _startButton;
  private Button _hostButton;
  private Button _joinButton;
  private Button _quitButton;
  private CanvasItem _mainMenuStuff;
  private CanvasItem _background;
  private Node _levels;
  private NetworkManager _networkManager;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    _connectionTimer = GetNode<Timer>("ConnectionTimer");
    _playersJoinedLabel = GetNode<Label>("MainMenuStuff/PlayersJoined");
    _startButton = GetNode<Button>("MainMenuStuff/ButtonContainer/Start");
    _hostButton = GetNode<Button>("MainMenuStuff/ButtonContainer/Host");
    _joinButton = GetNode<Button>("MainMenuStuff/ButtonContainer/Join");
    _quitButton = GetNode<Button>("MainMenuStuff/ButtonContainer/Quit");
    _mainMenuStuff = GetNode<CanvasItem>("MainMenuStuff");
    _background = GetNode<CanvasItem>("Background");
    _levels = GetNode<Node>("Levels");
    _networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    _playersJoinedLabel.Visible = false;

    _startButton.Pressed += () => Rpc(nameof(LoadGame));
    _hostButton.Pressed += () => _networkManager.CreateGame();
    _joinButton.Pressed += JoinGame;

    _quitButton.Pressed += () => GetTree().Quit();
    _startButton.GrabFocus();  // for keyboard/controller use

    _networkManager.PlayerConnected += OnPlayerConnected;
    _networkManager.PlayerDisconnected += OnPlayerDisconnected;
    _networkManager.ServerDisconnected += OnServerDisconnected;
  }

  private void OnPlayerConnected(long pid, string info)
  {
    _connectionTimer.Stop();
    if (info == "dedicated")
      _dedicatedCorrection = 1;

    _playersJoinedLabel.Text = 
      $"Players in Lobby: {_networkManager.Players.Count - _dedicatedCorrection}";
    _playersJoinedLabel.Visible = true;
  }

  private void OnPlayerDisconnected(long pid)
  {
    _playersJoinedLabel.Text =
      $"Players in Lobby: {_networkManager.Players.Count - _dedicatedCorrection}";
  }

  // If any player presses this, it starts the game for everyone
  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  public void LoadGame()
  {
    if (Multiplayer.IsServer())
    {
      // only server loads scene. MultiplayerSpawner spawns it for everyone
      var level = LevelScene.Instantiate();
      _levels.AddChild(level);
    }
    _background.Visible = false;
    _mainMenuStuff.Visible = false;
  }

  public void JoinGame()
  {
    _networkManager.JoinGame();
    // Start a timer to check if joining
    _connectionTimer.Start();
    _playersJoinedLabel.Text = "Joining...";
    _playersJoinedLabel.Visible = true;
  }

  private void OnConnectionTimerTimeout()
  {
    _networkManager.OnConnectedFail();
    _playersJoinedLabel.Text = "Connection Timed Out";
  }

  private void OnServerDisconnected()
  {
    // Back to main menu
    var levelsChildren = _levels.GetChildren();
    foreach (Node child in levelsChildren)
    {
      if (child is not MultiplayerSpawner)
        child.QueueFree();
    }

    _mainMenuStuff.Visible = true;
    _background.Visible = true;
    _playersJoinedLabel.Text = "Lost Connection to Server";
  }
}
