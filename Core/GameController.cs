using Godot;
using System;

public partial class GameController : Node2D
{
	private bool _isPaused;
    private PlayerEntity _player;
	private CanvasLayer _canvas;

    private Control _pauseUi;
    private ProgressBar _healthBar, _staminaBar, _armStaminaBar, _adrenalineBar;
    private TextureRect _dashStatus;

    private StyleBoxFlat _healthStyle, _staminaStyle, _adrenalineStyle;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_isPaused = false;
        _player = GetNode<PlayerEntity>("PlayerEntity");
        _canvas = GetNode<CanvasLayer>("CanvasLayer");

        _pauseUi = _canvas.GetNode<Control>("PauseMenu");
        _healthBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("HealthBar");
        _staminaBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("StaminaBar");
        _armStaminaBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("ArmStaminaBar");
        _adrenalineBar = _canvas.GetNode<VBoxContainer>("StatBars").GetNode<ProgressBar>("AdrenalineBar");
        _dashStatus = _canvas.GetNode<TextureRect>("DashStatus");

        _healthBar.MaxValue = _player.MaxHealth;
        _staminaBar.MaxValue = _player.MaxStamina;
        _armStaminaBar.MaxValue = _player.MaxArmStamina;
        _adrenalineBar.MaxValue = _player.MaxAdrenaline;
        
        _healthStyle = new StyleBoxFlat();
        _healthStyle.BgColor = new Color(0.8f, 0.2f, 0.2f, 1.0f);
        _healthStyle.SetCornerRadiusAll(0);
        _staminaStyle = new StyleBoxFlat();
        _staminaStyle.BgColor = new Color(0.4f, 0.4f, 0.8f, 1.0f);
        _staminaStyle.SetCornerRadiusAll(0);
        _adrenalineStyle = new StyleBoxFlat();
        _adrenalineStyle.BgColor = new Color(0.2f, 0.8f, 0.4f, 1.0f);
        _adrenalineStyle.SetCornerRadiusAll(0);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) PauseGame();

        _healthBar.Value = _player.CurrentHealth;
        _staminaBar.Value = _player.CurrentStamina;
        _armStaminaBar.Value = _player.CurrentArmStamina;
        _adrenalineBar.Value = _player.CurrentAdrenaline;
        _dashStatus.Visible = _player.IsDashing;

        _healthBar.AddThemeStyleboxOverride("fill", _healthStyle);
        _staminaBar.AddThemeStyleboxOverride("fill", _staminaStyle);
        StyleBoxFlat armStyle = new StyleBoxFlat();
        armStyle.SetCornerRadiusAll(0);
        armStyle.BgColor = _player.CurrentArmStamina < _player.ArmStaminaShakePoint ? new Color(1.0f, 0.0f, 0.0f, 1.0f) : new Color(0.3f, 0.3f, 1.0f, 1.0f);
        _armStaminaBar.AddThemeStyleboxOverride("fill", armStyle);
        _adrenalineBar.AddThemeStyleboxOverride("fill", _adrenalineStyle);
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
