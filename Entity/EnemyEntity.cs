using Godot;

public partial class EnemyEntity : CharacterBody2D, IPausable
{
	private Node2D _player;
	private bool _paused;
    
	// Thresholds
	[Export] public float ChaseThreshold { get; set; } = 750f;
	[Export] public float RandomMoveThreshold { get; set; } = 2000f;
	private float _chaseThresholdSq;
	private float _randomMoveThresholdSq;
    
	// Movement
	[Export] public float ChaseSpeed { get; set; } = 125f;
	[Export] public float Acceleration { get; set; } = 10f;
	[Export] public float LookSens = 5.0f;
	private bool _isChasing;
	private Vector2 _targetLocation;

	public override void _Ready()
	{
		_paused = false;
		
		_chaseThresholdSq = ChaseThreshold * ChaseThreshold;
		_randomMoveThresholdSq = RandomMoveThreshold * RandomMoveThreshold;

		_player = GetTree().GetFirstNodeInGroup("Player") as Node2D;

		_isChasing = false;
		_targetLocation = RandomLocation();
	}

	public override void _Process(double delta)
	{
		if (_paused) return;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_paused) return;
		if (_player == null) return;
		
		float distanceSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);

		if (distanceSq <= _chaseThresholdSq || _isChasing) ChasePlayer(distanceSq);
		else if (distanceSq <= _randomMoveThresholdSq) MoveRandomly();
		else ApproachPlayer();

		LookTowardsVelocityOrPlayer(delta);
	}

	private void ChasePlayer(float distanceSq)
	{
		_isChasing = true;
		
		if (distanceSq <= 80) Velocity = Vector2.Zero;
		else
		{
			Vector2 direction = GlobalPosition.DirectionTo(_player.GlobalPosition);
			Vector2 targetVelocity = direction * ChaseSpeed;
			Velocity = Velocity.Lerp(targetVelocity, Acceleration * (float)GetPhysicsProcessDeltaTime());
		}

		MoveAndSlide();
		if (distanceSq >= (_chaseThresholdSq * 1.5)) _isChasing = false;
	}

	private void MoveRandomly()
	{
		if (GlobalPosition.DistanceSquaredTo(_targetLocation) < 100 || Velocity == Vector2.Zero) _targetLocation = RandomLocation();
		
		Vector2 direction = GlobalPosition.DirectionTo(_targetLocation);
		Vector2 targetVelocity = direction * ChaseSpeed;
		Velocity = Velocity.Lerp(targetVelocity, Acceleration * (float)GetPhysicsProcessDeltaTime());
		MoveAndSlide();
	}

	private void ApproachPlayer()
	{
		Vector2 direction = GlobalPosition.DirectionTo(_player.GlobalPosition);
		Vector2 targetVelocity = direction * ChaseSpeed;
		Velocity = Velocity.Lerp(targetVelocity, Acceleration * (float)GetPhysicsProcessDeltaTime());
		MoveAndSlide();
		_targetLocation = RandomLocation();
	}
	
	private void LookTowardsVelocityOrPlayer(double delta)
	{
		if (Velocity == Vector2.Zero) return;

		float targetAngle = GlobalPosition.AngleToPoint(Position + Velocity);
		if (_isChasing) targetAngle = GlobalPosition.AngleToPoint(_player.GlobalPosition);
		float angleDifference = Mathf.AngleDifference(Rotation, targetAngle);
		Rotate(angleDifference * LookSens * (float)delta);
	}

	private readonly RandomNumberGenerator _rng = new();
	private Vector2 RandomLocation()
	{
		return new Vector2(Position.X + _rng.RandfRange(-500, 500), Position.Y + _rng.RandfRange(-500, 500));
	}
	
	// INTERFACE
	public void Pause()
	{
		_paused = true;
	}

	public void Resume()
	{
		_paused = false;
	}
}
