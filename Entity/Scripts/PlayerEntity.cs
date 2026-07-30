using System;
using System.Diagnostics;
using Godot;
using ProjectX.Interfaces;
using Vector2 = Godot.Vector2;

public partial class PlayerEntity : CharacterBody2D, IHurtable, IDebuggable, IPausable
{
	private bool _paused;
	
	// Nodes
	private Camera2D _playerCamera;
	private AnimatedSprite2D _playerSprite;
	private CollisionShape2D _playerCollision;
	private Area2D _playerRadar;
	private CollisionShape2D _playerRadarCollision;

	// Health, Stamina and Adrenaline
	[Export] public float MaxHealth = 100.0F;
	[Export] public float MaxStamina = 100.0F;
	[Export] public float MaxArmStamina = 100.0F;
	[Export] public float MaxAdrenaline = 60.0F;
	[Export] public float StaminaDrainRate = 1.1F;
	[Export] public float StaminaWalkRegen = 5.0F;
	[Export] public float StaminaIdleRegen = 10.0F;
	[Export] public float ArmStaminaDrainRate = 2.5F;
	[Export] public float ArmStaminaRegen = 7.5F;
	[Export] public float ArmStaminaShakePoint = 20.0F; // Percentage of arm stamina when aim becomes too inaccurate
	[Export] public float AdrenalineProximityGainRate = 1.0f;
	[Export] public float AdrenalinePassiveDecayRate = 0.2f;

	public float CurrentHealth { get; private set; }
	public float CurrentStamina { get; private set; }
	public float CurrentArmStamina { get; private set; }
	public float CurrentAdrenaline { get; private set; }

	// Movement
	[Export] public float MaxVelocity = 2.0F;
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
	public bool IsDashing = false;
	
	// Combat
	private bool _isAiming;

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

		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
		CurrentArmStamina = MaxArmStamina;
		CurrentAdrenaline = 0.0f;

		_movementVelocity = new Vector2(0, 0);
		_isMoving = false;
		_isAiming = false;
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

		if (IsDashing)
		{
			_dashTimer -= (float)delta;
			if (_dashTimer <= 0.0f)
			{
				IsDashing = false;
			}
			return;
		}

		if (Input.IsActionJustPressed("dash") && TrySpendDashResources())
		{
			IsDashing = true;
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

		_isAiming = Input.IsActionPressed("aim");
		if (_isAiming) CurrentArmStamina = Mathf.Max(0.0f, CurrentArmStamina - (ArmStaminaDrainRate * (float)delta));
		else CurrentArmStamina = Mathf.Min(MaxArmStamina, CurrentArmStamina + (ArmStaminaRegen * (float)delta));
		
		bool isSprinting = Input.IsActionPressed("sprint") && !_isAiming && CurrentStamina > 0.0f;
		float velocityChange = MaxVelocity / Inertia;
		if (isSprinting) velocityChange = velocityChange * SprintVelocityModifier;

		_isMoving = Input.IsActionPressed("move_up") || Input.IsActionPressed("move_down") || Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right");

		if (isSprinting) CurrentStamina = Mathf.Max(0.0f, CurrentStamina - (StaminaDrainRate * (float)delta));
		else if (_isMoving) CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + (StaminaWalkRegen * (float)delta));
		else CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + (StaminaIdleRegen * (float)delta));

		if (_isMoving)
		{
			if (Input.IsActionPressed("move_up")) _movementVelocity += new Vector2(0, -velocityChange);
			if (Input.IsActionPressed("move_down")) _movementVelocity += new Vector2(0, velocityChange);
			if (Input.IsActionPressed("move_left")) _movementVelocity += new Vector2(-velocityChange, 0);
			if (Input.IsActionPressed("move_right")) _movementVelocity += new Vector2(velocityChange, 0);
		}

		float maxCurrentSpeed = isSprinting ? MaxVelocity * SprintVelocityModifier : MaxVelocity;
		maxCurrentSpeed = ApplyAimPenalty(maxCurrentSpeed);
		_movementVelocity = _movementVelocity.LimitLength(maxCurrentSpeed);

		if (!isSprinting) _movementVelocity = _movementVelocity.Clamp(-MaxVelocity, MaxVelocity);
		else _movementVelocity = _movementVelocity.Clamp(-MaxVelocity * SprintVelocityModifier, MaxVelocity * SprintVelocityModifier);

		if (_isAiming) SmoothLookAtMouse(delta);
		else LookTowardsVelocity(delta);

	}

	public void UseDashResourceCost(float staminaCost, float adrenalineCost)
	{
		CurrentStamina = Mathf.Max(0.0f, CurrentStamina - staminaCost);
		CurrentAdrenaline = Mathf.Max(0.0f, CurrentAdrenaline - adrenalineCost);
	}

	private bool TrySpendDashResources()
	{
		if (CurrentStamina >= DashStaminaCost)
		{
			UseDashResourceCost(DashStaminaCost, 0.0f);
			return true;
		}
		else if (CurrentAdrenaline >= DashAdrenalineCost)
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
			CurrentAdrenaline = Mathf.Min(MaxAdrenaline, CurrentAdrenaline + gainAmount);
		}
		else
		{
			float decayAmount = AdrenalinePassiveDecayRate * (float)delta;
			CurrentAdrenaline = Mathf.Max(0.0f, CurrentAdrenaline - decayAmount);
		}
	}

	private float ApplyAimPenalty(float currentMaxSpeed)
	{
		if (_isAiming && _movementVelocity != Vector2.Zero)
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
		if (IsDashing)
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
	public bool TakeDamage(Node2D source, float damage)
	{
		CurrentHealth = Mathf.Max(0.0f, CurrentHealth - damage);
		CurrentAdrenaline = Mathf.Min(MaxAdrenaline, CurrentAdrenaline + damage);
		return true;
	}
	
	public string GetDebugText()
	{
		return $"[PLAYERENTITY]\n" +
			   $"Health: {CurrentHealth:F1} / {MaxHealth}\n" +
			   $"Stamina: {CurrentStamina:F1} / {MaxStamina}\n" +
			   $"Adrenaline: {CurrentAdrenaline:F1} / {MaxAdrenaline}\n" +
			   $"Velocity: {_movementVelocity.Length():F2}\n" +
			   $"Is Dashing: {IsDashing} | Dash Timer: {_dashTimer:F2}\n" +
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
