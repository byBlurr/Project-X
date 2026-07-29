using Godot;

public partial class MainMenu : Control
{
    [Export] private string _gameplayScenePath = "res://Game.tscn";

    public override void _Ready()
    {
        GetNode<Button>("MainMenuButtons/PlayButton").Pressed += OnPlayButtonPressed;
        GetNode<Button>("MainMenuButtons/SettingsButton").Pressed += OnSettingsButtonPressed;
        GetNode<Button>("MainMenuButtons/ExitButton").Pressed += OnExitButtonPressed;
    }

    private void OnPlayButtonPressed()
    {
        GetTree().ChangeSceneToFile(_gameplayScenePath);
    }

    private void OnSettingsButtonPressed()
    {
        GD.Print("Settings menu opened!");
    }

    private void OnExitButtonPressed()
    {
        GetTree().Quit();
    }
}