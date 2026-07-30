using Godot;
using System;

public partial class WeaponEntity : Node2D
{
	[Export] public PackedScene Projectile;
	
	public override void _Ready()
	{
		if (Projectile == null) throw new System.InvalidOperationException($"[WeaponEntity Fatal Error]: Required projectile PackedScene is missing!");
		
	}

	public override void _Process(double delta)
	{
	}

	public void Shoot()
	{
		CharacterBody2D owner = Owner as CharacterBody2D;
		ProjectileEntity projectile = Projectile.Instantiate<ProjectileEntity>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.GlobalRotation = GlobalRotation;
		projectile.ProjectileOwner = owner;
		GetTree().Root.AddChild(projectile);
	}
}
