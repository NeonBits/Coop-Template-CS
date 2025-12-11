using System.Collections.Generic;
using Godot;

public partial class Player : CharacterBody2D
{
  [Export] public float speed = 400.0f;

  private Vector2 syncPos = new(0.0f, 0.0f);

  private NetworkManager networkManager;
  private Label playerIdLabel;
  private AnimatedSprite2D animatedSprite;

  public override void _EnterTree()
  {
    // Doing this here instead of on ready prevents bugs.
    SetMultiplayerAuthority(int.Parse(Name));
  }

  public override void _Ready()
  {
    syncPos = GlobalPosition;

    playerIdLabel = GetNode<Label>("Player ID");
    animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    networkManager = GetNode<NetworkManager>("/root/NetworkManager");

    playerIdLabel.Text = Name;

    GameManager.PlayerInfo[int.Parse(Name)] = new()
    {
      { "spawnpoint", syncPos },
      { "node", this }
    };

    networkManager.PlayerDisconnected += OnPlayerDisconnected;
  }

  public override void _PhysicsProcess(double _delta)
  {
    if (!IsMultiplayerAuthority())
    {
      // Making it 30fps (save bandwidth) and lerping with local fps to hide the stutter
      Position = Position.Lerp(syncPos, 0.5f);
      return;
    }

    var velocity = Vector2.Zero; // The player's movement vector.
    if (Input.IsActionPressed("right_1"))
      velocity.X += 1;
    if (Input.IsActionPressed("left_1"))
      velocity.X -= 1;
    if (Input.IsActionPressed("down_1"))
      velocity.Y += 1;
    if (Input.IsActionPressed("up_1"))
      velocity.Y -= 1;

    if (velocity.Length() > 0)
    {
      velocity = velocity.Normalized() * speed;
      animatedSprite.Play();
    }
    else
      animatedSprite.Stop();

    syncPos = GlobalPosition;
    Velocity = velocity;
    MoveAndSlide();
  }

  private void OnPlayerDisconnected(long pid)
  {
    if (pid == long.Parse(Name))
    {
      GameManager.PlayerInfo.Remove(pid);
      QueueFree();
    }
  }
}
