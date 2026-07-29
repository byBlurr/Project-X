using Godot;
using System;

public partial class GameController : Node2D
{
	private bool _isPaused;
    private PlayerEntity _player;
	private CanvasLayer _canvas;

    private Control _pauseUi;
    private ProgressBar _healthBar, _staminaBar, _adrenalineBar;
    private TextureRect _dashStatus;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_isPaused = false;
        _player = GetNode<PlayerEntity>("PlayerEntity");
        _canvas = GetNode<CanvasLayer>("CanvasLayer");

        _pauseUi = _canvas.GetNode<Control>("PauseMenu");
        _healthBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("HealthBar");
        _staminaBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("StaminaBar");
        _adrenalineBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("AdrenalineBar");
        _dashStatus = _canvas.GetNode<TextureRect>("DashStatus");

        _healthBar.MaxValue = _player.MaxHealth;
        _staminaBar.MaxValue = _player.MaxStamina;
        _adrenalineBar.MaxValue = _player.MaxAdrenaline;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) PauseGame();

        _healthBar.Value = _player._currentHealth;
        _staminaBar.Value = _player._currentStamina;
        _adrenalineBar.Value = _player._currentAdrenaline;
        _dashStatus.Visible = _player._isDashing;
    }

	private void PauseGame()
	{
		_isPaused = !_isPaused;
		_pauseUi.Visible = _isPaused;

        PauseNode(GetTree().Root, _isPaused);
	}

	private void PauseNode(Node currentNode, bool pause)
	{
		if (currentNode is IPausable pausableNode) if (pause) pausableNode.Pause(); else pausableNode.Resume();

        int childCount = currentNode.GetChildCount();
        for (int i = 0; i < childCount; i++)
        {
            PauseNode(currentNode.GetChild(i), pause);
        }
    }
}
