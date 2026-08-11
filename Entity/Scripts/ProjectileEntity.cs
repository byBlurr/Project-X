using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProjectX.Enums;
using ProjectX.Interfaces;

public partial class ProjectileEntity : Node2D
{
	private PlayerEntity _player;
	
	[Export] public float MaxVelocity = 5000.0F;
	[Export] public float MinVelocity = 100.0F;
	[Export] public float Deceleration = 350.0F;
	[Export] public float Damage = 25.0F;
	[Export] public float Penetration = 10.0F;
	[Export] public bool Explosive = false;
	[Export] public float Lifetime = 10.0f;
	[Export] public RayCast2D CollisionRaycast; 
	public CharacterBody2D ProjectileOwner;
	private Vector2 _direction;
	private float _currentVelocity;
	private float _timeAlive = 0.0f;
	private readonly HashSet<GodotObject> _hitTargets = new HashSet<GodotObject>();
	
	// Draw a bullet
	[Export] public float CircleRadius = 3f;
	[Export] public Color CircleColor = Colors.Yellow;
	
	public override void _Ready()
	{
		_player = GetTree().CurrentScene.GetNode<PlayerEntity>("PlayerEntity");
		_direction = Vector2.FromAngle(Rotation);
		_currentVelocity = MaxVelocity;
	}

	public override void _PhysicsProcess(double delta)
	{
		Cleanup(delta);
		CheckCollision(delta);
		
		if (ProjectileOwner == null) return; // TODO Don't do anything until the owner is known, this has to be manually setup when spawning the projectile
		Position += (_direction * _currentVelocity) * (float)delta;
		_currentVelocity = _currentVelocity - (Deceleration * (float)delta);
	}
	
	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, CircleRadius, CircleColor);
	}

	private void CheckCollision(double delta)
	{
		CollisionRaycast.TargetPosition = new Vector2(_currentVelocity * (float)delta, 0.0F);
		CollisionRaycast.ForceRaycastUpdate();

		if (CollisionRaycast.IsColliding())
		{
			GodotObject collider = CollisionRaycast.GetCollider();
			if (_hitTargets.Contains(collider)) return;
			if ((collider is IHurtable hurtBody)) Penetrate(collider as CharacterBody2D);
		}
	}

	// TODO: Check what entity it has hit, use IHurtable to cause damage and then slow the bullet or destroy if velocity too low
	public void Penetrate(CharacterBody2D body)
	{
		if (ProjectileOwner == body) return; // Will this stop self harm? TODO Test
		_hitTargets.Add(body);
		_player.AddPlayerStat(Actions.HIT_TARGET);
		if (body is IHurtable hurtBody)
		{
			float speedPercentage = (_currentVelocity / MaxVelocity);
			float damage = Damage * speedPercentage;
			
			hurtBody.TakeDamage(this, damage);
			_currentVelocity -= (MaxVelocity / 10.0F); // Slow the bullet by 10% of max velocity, this will need tweaking TODO why isnt this an export already?
		}
	}

	// After LifeTime seconds or once the velocity is as low as MinVelocity, destroy the projectile next frame
	public void Cleanup(double delta)
	{
		_timeAlive += (float)delta;
		if (_timeAlive >= Lifetime || _currentVelocity <= MinVelocity)
		{
			QueueFree();
		}
	}
}
