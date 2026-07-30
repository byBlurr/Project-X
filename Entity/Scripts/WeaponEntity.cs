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

	public void Shoot(Vector2 direction)
	{
		CharacterBody2D owner = Owner as CharacterBody2D;
		Node2D projectile = Projectile.Instantiate<Node2D>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.GlobalRotation = GlobalRotation;
		GetTree().Root.AddChild(projectile);
	}
}
