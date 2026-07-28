using Godot;
using System.Text;

public partial class DebugOverlay : Node
{
    private Label _debugLabel;

    public override void _Ready()
    {
        _debugLabel = GetNode<Label>("CanvasLayer/DebugLabel");

        if (!OS.IsDebugBuild())
        {
            ProcessMode = ProcessModeEnum.Disabled;
            _debugLabel?.Hide();
        }
    }

    public override void _Process(double delta)
    {
        if (_debugLabel == null) return;

        StringBuilder debugReport = new StringBuilder();
        debugReport.AppendLine($"=== SYSTEM OVERLAY (FPS: {Engine.GetFramesPerSecond()}) ===");
        FindAndAppendDebuggables(GetTree().Root, debugReport);
        _debugLabel.Text = debugReport.ToString();
    }

    private void FindAndAppendDebuggables(Node currentNode, StringBuilder builder)
    {
        if (currentNode is IDebuggable debuggableNode)
        {
            builder.AppendLine(debuggableNode.GetDebugText());
            builder.AppendLine("------------------------------------");
        }

        int childCount = currentNode.GetChildCount();
        for (int i = 0; i < childCount; i++)
        {
            FindAndAppendDebuggables(currentNode.GetChild(i), builder);
        }
    }
}