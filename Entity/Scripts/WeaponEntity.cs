using Godot;
using System;
using System.Diagnostics;

public partial class WeaponEntity : Node2D
{
	[Export] public PackedScene Projectile;
	[Export] public float MinScatterRad = Mathf.DegToRad(0.10F);  // Spread at 1.0 accuracy
	[Export] public float MaxScatterRad = Mathf.DegToRad(15.0F); // Spread at 0.0 accuracy

	
	public override void _Ready()
	{
		if (Projectile == null) throw new System.InvalidOperationException($"[WeaponEntity Fatal Error]: Required projectile PackedScene is missing!");
		
	}

	public override void _Process(double delta)
	{
	}
	
	public void Shoot(float modifier = 1.0F)
	{
		float clampedAccuracy = Mathf.Clamp(modifier, 0.0f, 1.0f);
		float inversion = 1.0f - clampedAccuracy;
		float maxSpread = Mathf.Lerp(MinScatterRad, MaxScatterRad, inversion);
		float randomScatter = (float)GD.RandRange(-maxSpread, maxSpread);
		
		CharacterBody2D owner = Owner as CharacterBody2D;
		ProjectileEntity projectile = Projectile.Instantiate<ProjectileEntity>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.GlobalRotation = GlobalRotation + randomScatter;
		projectile.ProjectileOwner = owner;
		GetTree().Root.AddChild(projectile);
	}
}
