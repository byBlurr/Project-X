using Godot;

public partial class EnemyEntity : CharacterBody2D, IPausable
{
	private Node2D _player;
	private bool _paused;
    
	// Thresholds
	[Export] public float ChaseThreshold { get; set; } = 1000f;
	[Export] public float RandomMoveThreshold { get; set; } = 2500f;
	private float _chaseThresholdSq;
	private float _randomMoveThresholdSq;

    // Movement
    [Export] public float MaximumVelocity = 3.0F;
    [Export] public float Inertia = 25.0F;
    [Export] public float Deceleration = 10.0F;
    [Export] public float LookSens = 4.0f;
    private Vector2 _movementVelocity;
    private bool _isMoving;
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

        _movementVelocity = new Vector2(0, 0);
        _isMoving = false;
    }

	public override void _Process(double delta)
	{
		if (_paused) return;
        if (_player == null) return;
    }

	public override void _PhysicsProcess(double delta)
	{
		if (_paused) return;
		if (_player == null) return;


        float distanceSq = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);

        if (distanceSq <= _chaseThresholdSq || _isChasing) ChasePlayer(delta, distanceSq);
        else if (distanceSq <= _randomMoveThresholdSq) MoveRandomly(delta);
        else ApproachPlayer(delta);

        LookTowardsVelocityOrPlayer(delta);
	}

	private void ChasePlayer(double delta, float distanceSq)
	{
		_isChasing = true;
		
		if (distanceSq <= 80) Velocity = Vector2.Zero;
		else
		{
			Vector2 direction = GlobalPosition.DirectionTo(_player.GlobalPosition);
            float velocityChange = MaximumVelocity / Inertia;
			_movementVelocity = _movementVelocity += (direction * velocityChange);
			_movementVelocity = _movementVelocity.LimitLength(MaximumVelocity);
            _movementVelocity = _movementVelocity.Clamp(-MaximumVelocity, MaximumVelocity);
        }

        Move(delta);
        if (distanceSq >= (_chaseThresholdSq * 1.5)) _isChasing = false;
	}

	private void MoveRandomly(double delta)
	{
		if (GlobalPosition.DistanceSquaredTo(_targetLocation) < 100 || Velocity == Vector2.Zero) _targetLocation = RandomLocation();
		
		Vector2 direction = GlobalPosition.DirectionTo(_targetLocation);
        float velocityChange = MaximumVelocity / Inertia;
        _movementVelocity = _movementVelocity += (direction * velocityChange);
        _movementVelocity = _movementVelocity.LimitLength(MaximumVelocity);
        _movementVelocity = _movementVelocity.Clamp(-MaximumVelocity, MaximumVelocity);
        Move(delta);
    }

	private void ApproachPlayer(double delta)
	{
		Vector2 direction = GlobalPosition.DirectionTo(_player.GlobalPosition);
        float velocityChange = MaximumVelocity / Inertia;
        _movementVelocity = _movementVelocity += (direction * velocityChange);
        _movementVelocity = _movementVelocity.LimitLength(MaximumVelocity);
        _movementVelocity = _movementVelocity.Clamp(-MaximumVelocity, MaximumVelocity);
        Move(delta);
		_targetLocation = RandomLocation();
	}

	public void Move(double delta)
	{
        float currentResistance = _isMoving ? Inertia : Deceleration;

        Velocity = _movementVelocity * 60.0f;
        MoveAndSlide();
        _movementVelocity = Velocity / 60.0f;

        Vector2 frictionChange = ((_movementVelocity / currentResistance) * 60.0f) * (float)delta;

        if (Mathf.Abs(_movementVelocity.X) <= Mathf.Abs(frictionChange.X)) _movementVelocity.X = 0.0f;
        else _movementVelocity.X -= frictionChange.X;

        if (Mathf.Abs(_movementVelocity.Y) <= Mathf.Abs(frictionChange.Y)) _movementVelocity.Y = 0.0f;
        else _movementVelocity.Y -= frictionChange.Y;
    }
	
	private void LookTowardsVelocityOrPlayer(double delta)
	{
		if (Velocity == Vector2.Zero) return;

		float targetAngle = GlobalPosition.AngleToPoint(Position + Velocity);
        if (_isChasing) targetAngle = (targetAngle + (GlobalPosition.AngleToPoint(_player.GlobalPosition) - targetAngle) / 2.0F);
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
