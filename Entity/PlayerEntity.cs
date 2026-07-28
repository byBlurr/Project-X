using Godot;

public partial class PlayerEntity : CharacterBody2D, IDebuggable
{
    // Nodes
    private Camera2D playerCamera;
    private AnimatedSprite2D playerSprite;
    private CollisionShape2D playerCollision;

    // Health, Stamina and Adrenaline
    [Export] public float MaxHealth = 100.0f;
    [Export] public float MaxStamina = 100.0f;
    [Export] public float MaxAdrenaline = 100.0f;
    [Export] public float StaminaDrainRate = 5.0f;      // Points per second while sprinting
    [Export] public float StaminaWalkRegen = 10.0f;    // Points per second while walking
    [Export] public float StaminaIdleRegen = 25.0f;    // Points per second while stopped

    public float CurrentHealth { get; private set; }
    public float CurrentStamina { get; private set; }
    public float CurrentAdrenaline { get; private set; }

    // Movement
    [Export] public float MaximumVelocity = 2.0F;
    [Export] public float Inertia = 40.0F;
    [Export] public float Deceleration = 12.0F;
    [Export] public float SprintVelocityModifier = 2.3F;
    [Export] public float LookSens = 5.0f;
    [Export] public float AimPenaltyModifier = 0.75f;
    private Vector2 MovementVelocity;
    private bool isMoving;

    // Camera
    [Export] public float CameraSmoothSpeed = 5.0f;

    // Animation
    [Export] public bool UseStaticPlaceholder = true;

    public override void _Ready()
    {
        playerCamera = GetNode<Camera2D>("PlayerCamera");
        playerSprite = GetNode<AnimatedSprite2D>("PlayerSprite");
        playerCollision = GetNode<CollisionShape2D>("PlayerCollision");

        if (playerCamera == null ||  playerSprite == null || playerCollision == null)
        {
            throw new System.InvalidOperationException(
            $"[PlayerEntity Fatal Error]: Required child nodes are missing from the scene tree!\n" +
            $"-> PlayerCamera found: {playerCamera != null}\n" +
            $"-> PlayerSprite found: {playerSprite != null}\n" +
            $"-> PlayerCollision found: {playerCollision != null}\n" +
            $"Please check that child node names match exactly in the Godot Editor scene dock."
        );
        }

        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
        CurrentAdrenaline = 0.0f;

        MovementVelocity = new Vector2(0, 0);
        isMoving = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleInput(delta);
        Move(delta);
    }

    public override void _Process(double delta)
    {
        UpdateCamera(delta);
        UpdateAnimations();
    }

    private void HandleInput(double delta)
    {
        bool isSprinting = Input.IsActionPressed("sprint") && !Input.IsActionPressed("aim") && CurrentStamina > 0.0f;
        float velocityChange = MaximumVelocity / Inertia;
        if (isSprinting) velocityChange = velocityChange * SprintVelocityModifier;

        isMoving = Input.IsActionPressed("move_up") || Input.IsActionPressed("move_down") || Input.IsActionPressed("move_left") || Input.IsActionPressed("move_right");

        if (isSprinting) CurrentStamina = Mathf.Max(0.0f, CurrentStamina - (StaminaDrainRate * (float)delta));
        else if (isMoving) CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + (StaminaWalkRegen * (float)delta));
        else CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + (StaminaIdleRegen * (float)delta));

        if (isMoving)
        {
            if (Input.IsActionPressed("move_up")) MovementVelocity += new Vector2(0, -velocityChange);
            if (Input.IsActionPressed("move_down")) MovementVelocity += new Vector2(0, velocityChange);
            if (Input.IsActionPressed("move_left")) MovementVelocity += new Vector2(-velocityChange, 0);
            if (Input.IsActionPressed("move_right")) MovementVelocity += new Vector2(velocityChange, 0);
        }

        float maxCurrentSpeed = isSprinting ? MaximumVelocity * SprintVelocityModifier : MaximumVelocity;
        maxCurrentSpeed = ApplyAimPenalty(maxCurrentSpeed);
        MovementVelocity = MovementVelocity.LimitLength(maxCurrentSpeed);

        if (!isSprinting) MovementVelocity = MovementVelocity.Clamp(-MaximumVelocity, MaximumVelocity);
        else MovementVelocity = MovementVelocity.Clamp(-MaximumVelocity * SprintVelocityModifier, MaximumVelocity * SprintVelocityModifier);

        if (Input.IsActionPressed("aim")) SmoothLookAtMouse(delta);
        else LookTowardsVelocity(delta);

    }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0.0f, CurrentHealth - amount);
        CurrentAdrenaline = Mathf.Min(MaxAdrenaline, CurrentAdrenaline + amount);
    }

    public void UseDashResourceCost(float staminaCost, float adrenalineCost)
    {
        CurrentStamina = Mathf.Max(0.0f, CurrentStamina - staminaCost);
        CurrentAdrenaline = Mathf.Max(0.0f, CurrentAdrenaline - adrenalineCost);
    }

    private float ApplyAimPenalty(float currentMaxSpeed)
    {
        if (Input.IsActionPressed("aim") && MovementVelocity != Vector2.Zero)
        {
            Vector2 aimDirection = (GetGlobalMousePosition() - GlobalPosition).Normalized();
            Vector2 movementDirection = MovementVelocity.Normalized();
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
        if (MovementVelocity == Vector2.Zero) return;

        float targetAngle = GlobalPosition.AngleToPoint(Position + MovementVelocity);
        float angleDifference = Mathf.AngleDifference(Rotation, targetAngle);
        Rotate(angleDifference * LookSens * (float)delta);
    }

    public void Move(double delta)
    {
        float currentResistance = isMoving ? Inertia : Deceleration;

        Velocity = MovementVelocity * 60.0f;
        MoveAndSlide();
        MovementVelocity = Velocity / 60.0f;

        MovementVelocity += Vector2.Zero - (((MovementVelocity / currentResistance) * 60.0F) * (float)delta);
    }

    private void UpdateCamera(double delta)
    {
        if (playerCamera == null) return;
        playerCamera.Position = playerCamera.Position.Lerp(Vector2.Zero, CameraSmoothSpeed * (float)delta);
    }

    private void UpdateAnimations()
    {
        if (playerSprite == null) return;

        // If using a placeholder, stop here so no walk/sprint loops trigger
        if (UseStaticPlaceholder)
        {
            playerSprite.Stop(); // Freezes the animation loop
            return;
        }

        bool isSprinting = Input.IsActionPressed("sprint") && MovementVelocity.Length() > 0.1f;

        if (isMoving && MovementVelocity.Length() > 0.1f)
        {
            if (isSprinting) playerSprite.Play("sprint");
            else playerSprite.Play("walk");
        }
        else
        {
            playerSprite.Play("idle");
        }
    }


    // --- IDebuggable  ---
    public string GetDebugText()
    {
        return $"[PLAYERENTITY]\n" +
               $"Health: {CurrentHealth:F1} / {MaxHealth}\n" +
               $"Stamina: {CurrentStamina:F1} / {MaxStamina}\n" +
               $"Adrenaline: {CurrentAdrenaline:F1} / {MaxAdrenaline}\n" +
               $"Velocity: {MovementVelocity.Length():F2}";
    }
}