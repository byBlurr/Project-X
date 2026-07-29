using Godot;
using System.Collections.Generic;

public partial class EnemySpawner : Node2D, IDebuggable, IPausable
{
    private bool _paused;

    [Export] public PackedScene EnemyScene;
    [Export] public float SpawnRadius = 400.0f;
    [Export] public int MaxActiveEnemies = 10;
    [Export] public float ClearanceRadius = 64.0f;
    [Export] public int MaxSpawnAttempts = 10;

    private List<Node2D> activeEnemies = new List<Node2D>();
    private Timer spawnTimer;

    public override void _Ready()
    {
        spawnTimer = GetNode<Timer>("Timer");
        spawnTimer.Timeout += OnSpawnTimerTimeout;
    }

    private void OnSpawnTimerTimeout()
    {
        while (_paused) { }
        activeEnemies.RemoveAll(enemy => !GodotObject.IsInstanceValid(enemy));
        if (activeEnemies.Count >= MaxActiveEnemies) return;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (EnemyScene == null) return;

        var spaceState = GetWorld2D().DirectSpaceState;
        if (spaceState == null) return;

        var query = new PhysicsShapeQueryParameters2D();

        var circleShape = new CircleShape2D();
        circleShape.Radius = ClearanceRadius;
        query.Shape = circleShape;

        query.CollisionMask = 1 << 2;

        Vector2 validSpawnPosition = Vector2.Zero;
        bool locationFound = false;

        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            float randomAngle = GD.Randf() * Mathf.Tau;
            float randomDistance = GD.Randf() * SpawnRadius;
            Vector2 spawnOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;

            Vector2 testPosition = GlobalPosition + spawnOffset;
            query.Transform = new Transform2D(0, testPosition);

            var intersections = spaceState.IntersectShape(query, maxResults: 1);

            if (intersections.Count == 0)
            {
                validSpawnPosition = testPosition;
                locationFound = true;
                break;
            }
        }

        if (!locationFound)
        {
            GD.Print("[EnemySpawner]: Could not find an un-crowded location this frame.");
            return;
        }

        Node2D enemyInstance = EnemyScene.Instantiate<Node2D>();
        enemyInstance.GlobalPosition = validSpawnPosition;

        activeEnemies.Add(enemyInstance);
        GetParent().AddChild(enemyInstance);
    }




    public string GetDebugText()
    {
        return $"[{Name.ToString().ToUpper()}]\n" +
               $"Active Enemies: {activeEnemies.Count} / {MaxActiveEnemies}";
    }

    public void Pause()
    {
        spawnTimer.Paused = true;
        _paused = true;
    }

    public void Resume()
    {
        spawnTimer.Paused = false;
        _paused = false;
    }
}
