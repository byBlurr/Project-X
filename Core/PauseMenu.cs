using Godot;
using System;

public partial class PauseMenu : Control
{
	private GameController _gameController;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_gameController = GetTree().CurrentScene as GameController;
		GetNode<Button>("VBoxContainer/ResumeButton").Pressed += OnResumeButtonPressed;
		GetNode<Button>("VBoxContainer/ExitButton").Pressed += OnExitButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void OnResumeButtonPressed()
	{
		_gameController.PauseGame();
	}
	
	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}
}
