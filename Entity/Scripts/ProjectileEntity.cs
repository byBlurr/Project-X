using Godot;
using System;
using ProjectX.Interfaces;

public partial class ProjectileEntity : Node2D
{
	[Export] public float MaxVelocity = 1000.0F;
	[Export] public float MinVelocity = 50.0F;
	[Export] public float Deceleration = 350.0F;
	[Export] public float Damage = 25.0F;
	[Export] public float Penetration = 10.0F;
	[Export] public bool Explosive = false;
	[Export] public float Lifetime = 10.0f;
	private CharacterBody2D _owner;
	private Vector2 _direction;
	private float _currentVelocity;
	private float _timeAlive = 0.0f;
	
	// Draw a bullet
	[Export] public float CircleRadius = 3f;
	[Export] public Color CircleColor = Colors.Yellow;
	
	public override void _Ready()
	{
		_direction = Vector2.FromAngle(Rotation);
		_currentVelocity = MaxVelocity;
	}

	public override void _PhysicsProcess(double delta)
	{
		Cleanup(delta);
		
		if (_owner == null) return; // TODO Don't do anything until the owner is known, this has to be manually setup when spawning the projectile
		_currentVelocity = _currentVelocity - (Deceleration * (float)delta);
		Position += (_direction * _currentVelocity) * (float)delta;
	}
	
	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, CircleRadius, CircleColor);
	}

	// TODO: Check what entity it has hit, use IHurtable to cause damage and then slow the bullet or destroy if velocity too low
	public void Penetrate(CharacterBody2D body)
	{
		if (_owner == body) return; // Will this stop self harm? TODO Test
		if (body is IHurtable hurtBody)
		{
			float speedPercentage = (_currentVelocity / MaxVelocity) * 100f;
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
