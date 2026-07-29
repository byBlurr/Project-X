using Godot;
using System;

public partial class GameController : Node2D
{
	private bool _isPaused;
    private CharacterBody2D _player;
    private Control _pauseUi;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_isPaused = false;
        _player = GetNode<CharacterBody2D>("PlayerEntity");
        _pauseUi = GetNode<Control>("PauseMenu");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("pause")) PauseGame();
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
