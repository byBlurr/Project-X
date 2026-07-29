using Godot;
using Vector2 = Godot.Vector2;

public partial class PlayerEntity : CharacterBody2D, IDebuggable, IPausable
{
	private bool _paused;
	
	// Nodes
	private Camera2D _playerCamera;
	private AnimatedSprite2D _playerSprite;
	private CollisionShape2D _playerCollision;
	private Area2D _playerRadar;
	private CollisionShape2D _playerRadarCollision;

	// Health, Stamina and Adrenaline
	[Export] public float MaxHealth = 100.0f;
	[Export] public float MaxStamina = 100.0f;
	[Export] public float MaxAdrenaline = 60.0f;
	[Export] public float StaminaDrainRate = 1.1f;      // Points per second while sprinting
	[Export] public float StaminaWalkRegen = 5.0f;    // Points per second while walking
	[Export] public float StaminaIdleRegen = 10.0f;    // Points per second while stopped
	[Export] public float AdrenalineProximityGainRate = 1.0f;
	[Export] public float AdrenalinePassiveDecayRate = 0.2f;

	public float _currentHealth { get; private set; }
	public float _currentStamina { get; private set; }
	public float _currentAdrenaline { get; private set; }

	// Movement
	[Export] public float MaximumVelocity = 2.0F;
	[Export] public float Inertia = 40.0F;
	[Export] public float Deceleration = 12.0F;
	[Export] public float SprintVelocityModifier = 2.3F;
	[Export] public float LookSens = 5.0f;
	[Export] public float AimPenaltyModifier = 0.75f;
	private Vector2 _movementVelocity;
	private bool _isMoving;

	// Dashing
	[Export] public float DashVelocity = 30.0f;
	[Export] public float DashDuration = 0.2f;
	[Export] public float DashStaminaCost = 40.0f;
	[Export] public float DashAdrenalineCost = 40.0f;

	private Vector2 _dashDirection;
	private float _dashTimer = 0.0f;
	public bool _isDashing = false;

	// Camera
	[Export] public float CameraSmoothSpeed = 5.0f;

	// Animation
	[Export] public bool UseStaticPlaceholder = true;

	public override void _Ready()
	{
		_paused = false;
		
		_playerCamera = GetNode<Camera2D>("PlayerCamera");
		_playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
		_playerCollision = GetNode<CollisionShape2D>("PlayerCollision");
		_playerRadar = GetNodeOrNull<Area2D>("PlayerRadar");
		_playerRadarCollision = _playerRadar?.GetNodeOrNull<CollisionShape2D>("PlayerRadarCollision");

		if (_playerCamera == null || _playerSprite == null || _playerCollision == null || _playerRadar == null || _playerRadarCollision == null)
		{
			throw new System.InvalidOperationException(
				$"[PlayerEntity Fatal Error]: Required child nodes are missing from the scene tree!\n" +
				$"-> PlayerCamera found: {_playerCamera != null}\n" +
				$"-> PlayerSprite found: {_playerSprite != null}\n" +
				$"-> PlayerCollision found: {_playerCollision != null}\n" +
				$"-> PlayerRadar found: {_playerRadar != null}\n" +
				$"-> PlayerRadar Shape found: {_playerRadarCollision != null}\n" +
				$"Please check that child node names match exactly in the Godot Editor scene dock."
			);
		}

		_currentHealth = MaxHealth;
		_currentStamina = MaxStamina;
		_currentAdrenaline = 0.0f;

		_movementVelocity = new Vector2(0, 0);
		_isMoving = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_paused) return;
		
		HandleInput(delta);
		ProcessAdrenalineProximity(delta);
		Move(delta);
	}

	public override void _Process(double delta)
	{
		if (_paused) return;
		
		UpdateCamera(delta);
		UpdateAnimations();
	}

	private void HandleInput(double delta)
	{
		if (_playerCamera != null)
		{
			if (Input.IsActionJustPressed("zoom_out")) _playerCamera.Zoom = (_playerCamera.Zoom + new Vector2(-0.01F, -0.01F)).Clamp(0.0F, 1.0F);
			if (Input.IsActionJustPressed("zoom_in")) _playerCamera.Zoom = (_playerCamera.Zoom + new Vector2(0.01F, 0.01F)).Clamp(0.0F, 1.0F);
		}

		if (_isDashing)
		{
			_dashTimer -= (float)delta;
			if (_dashTimer <= 0.0f)
			{
				_isDashing = false;
			}
			return;
		}

		if (Input.IsActionJustPressed("dash") && TrySpendDashResources())
		{
			_isDashing = true;
			_dashTimer = DashDuration;
			
			if (_movementVelocity != Vector2.Zero)
			{
				_dashDirection = _movementVelocity.Normalized();
			}
			else
			{
				_dashDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();
			}

			_movementVelocity = _dashDirection * DashVelocity;
			return;
		}

		bool isSprinting = Input.IsActionPressed("sprint") && !Input.IsActionPressed("aim") && _currentStamina > 0.0f;
		float velocityChange = MaximumVelocity / Inertia;
		if (isSprinting) velocityChange = velocityChange * SprintVelocityModifier;

		_isMoving = Input.IsActionPressed("move_up") || Input.IsActionPressed("move_down") || Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right");

		if (isSprinting) _currentStamina = Mathf.Max(0.0f, _currentStamina - (StaminaDrainRate * (float)delta));
		else if (_isMoving) _currentStamina = Mathf.Min(MaxStamina, _currentStamina + (StaminaWalkRegen * (float)delta));
		else _currentStamina = Mathf.Min(MaxStamina, _currentStamina + (StaminaIdleRegen * (float)delta));

		if (_isMoving)
		{
			if (Input.IsActionPressed("move_up")) _movementVelocity += new Vector2(0, -velocityChange);
			if (Input.IsActionPressed("move_down")) _movementVelocity += new Vector2(0, velocityChange);
			if (Input.IsActionPressed("move_left")) _movementVelocity += new Vector2(-velocityChange, 0);
			if (Input.IsActionPressed("move_right")) _movementVelocity += new Vector2(velocityChange, 0);
		}

		float maxCurrentSpeed = isSprinting ? MaximumVelocity * SprintVelocityModifier : MaximumVelocity;
		maxCurrentSpeed = ApplyAimPenalty(maxCurrentSpeed);
		_movementVelocity = _movementVelocity.LimitLength(maxCurrentSpeed);

		if (!isSprinting) _movementVelocity = _movementVelocity.Clamp(-MaximumVelocity, MaximumVelocity);
		else _movementVelocity = _movementVelocity.Clamp(-MaximumVelocity * SprintVelocityModifier, MaximumVelocity * SprintVelocityModifier);

		if (Input.IsActionPressed("aim")) SmoothLookAtMouse(delta);
		else LookTowardsVelocity(delta);

	}

	public void TakeDamage(float amount)
	{
		_currentHealth = Mathf.Max(0.0f, _currentHealth - amount);
		_currentAdrenaline = Mathf.Min(MaxAdrenaline, _currentAdrenaline + amount);
	}

	public void UseDashResourceCost(float staminaCost, float adrenalineCost)
	{
		_currentStamina = Mathf.Max(0.0f, _currentStamina - staminaCost);
		_currentAdrenaline = Mathf.Max(0.0f, _currentAdrenaline - adrenalineCost);
	}

	private bool TrySpendDashResources()
	{
		if (_currentStamina >= DashStaminaCost)
		{
			UseDashResourceCost(DashStaminaCost, 0.0f);
			return true;
		}
		else if (_currentAdrenaline >= DashAdrenalineCost)
		{
			UseDashResourceCost(0.0f, DashAdrenalineCost);
			return true;
		}

		return false;
	}

	private void ProcessAdrenalineProximity(double delta)
	{
		if (_playerRadar == null) return;

		var overlappingBodies = _playerRadar.GetOverlappingBodies();
		int enemyCount = 0;

		foreach (Node2D body in overlappingBodies)
		{
			if (body == this) continue;

			if (body is CharacterBody2D || body.IsInGroup("enemies"))
			{
				enemyCount++;
			}
		}

		if (enemyCount > 0)
		{
			float gainAmount = AdrenalineProximityGainRate * enemyCount * (float)delta;
			_currentAdrenaline = Mathf.Min(MaxAdrenaline, _currentAdrenaline + gainAmount);
		}
		else
		{
			float decayAmount = AdrenalinePassiveDecayRate * (float)delta;
			_currentAdrenaline = Mathf.Max(0.0f, _currentAdrenaline - decayAmount);
		}
	}

	private float ApplyAimPenalty(float currentMaxSpeed)
	{
		if (Input.IsActionPressed("aim") && _movementVelocity != Vector2.Zero)
		{
			Vector2 aimDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();
			Vector2 movementDirection = _movementVelocity.Normalized();
			if (movementDirection.Dot(aimDirection) < 0.0f)
			{
				return currentMaxSpeed * AimPenaltyModifier;
			}
		}

		return currentMaxSpeed;
	}

	private void SmoothLookAtMouse(double delta)
	{
		float targetAngle = GlobalPosition.AngleToPoint(GetGlobalMousePosition());
		float angleDifference = Mathf.AngleDifference(Rotation, targetAngle);
		Rotate(angleDifference * LookSens * (float)delta);
	}

	private void LookTowardsVelocity(double delta)
	{
		if (_movementVelocity == Vector2.Zero) return;

		float targetAngle = GlobalPosition.AngleToPoint(Position + _movementVelocity);
		float angleDifference = Mathf.AngleDifference(Rotation, targetAngle);
		Rotate(angleDifference * LookSens * (float)delta);
	}

	public void Move(double delta)
	{
		if (_isDashing)
		{
			Velocity = _movementVelocity * 60.0f;
			MoveAndSlide();
			_movementVelocity = Velocity / 60.0f;
			return;
		}

		float currentResistance = _isMoving ? Inertia : Deceleration;

		Velocity = _movementVelocity * 60.0f;
		MoveAndSlide();
		_movementVelocity = Velocity / 60.0f;

		Vector2 frictionChange = ((_movementVelocity / currentResistance) * 60.0f) * (float)delta;

		if (Mathf.Abs(_movementVelocity.X) <= Mathf.Abs(frictionChange.X)) _movementVelocity.X = 0.0f;
		else _movementVelocity.X -= frictionChange.X;

		if (Mathf.Abs(_movementVelocity.Y) <= Mathf.Abs(frictionChange.Y)) _movementVelocity.Y = 0.0f;
		else _movementVelocity.Y -= frictionChange.Y;

		// MovementVelocity += Vector2.Zero - (((MovementVelocity / currentResistance) * 60.0F) * (float)delta);
	}

	private void UpdateCamera(double delta)
	{
		if (_playerCamera == null) return;
		_playerCamera.Position = _playerCamera.Position.Lerp(Vector2.Zero, CameraSmoothSpeed * (float)delta);
	}

	private void UpdateAnimations()
	{
		if (_playerSprite == null) return;

		// If using a placeholder, stop here so no walk/sprint loops trigger
		if (UseStaticPlaceholder)
		{
			_playerSprite.Stop(); // Freezes the animation loop
			return;
		}

		bool isSprinting = Input.IsActionPressed("sprint") && _movementVelocity.Length() > 0.1f;

		if (_isMoving && _movementVelocity.Length() > 0.1f)
		{
			if (isSprinting) _playerSprite.Play("sprint");
			else _playerSprite.Play("walk");
		}
		else
		{
			_playerSprite.Play("idle");
		}
	}


	// --- INTERFACES  ---
	public string GetDebugText()
	{
		return $"[PLAYERENTITY]\n" +
			   $"Health: {_currentHealth:F1} / {MaxHealth}\n" +
			   $"Stamina: {_currentStamina:F1} / {MaxStamina}\n" +
			   $"Adrenaline: {_currentAdrenaline:F1} / {MaxAdrenaline}\n" +
			   $"Velocity: {_movementVelocity.Length():F2}\n" +
			   $"Is Dashing: {_isDashing} | Dash Timer: {_dashTimer:F2}\n" +
			   $"Zoom: {_playerCamera.Zoom}";
	}
	
	public void Pause()
	{
		_paused = true;
	}

	public void Resume()
	{
		_paused = false;
	}
}
