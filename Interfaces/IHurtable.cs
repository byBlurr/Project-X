using Godot;

namespace ProjectX.Interfaces;

public interface IHurtable
{
    public bool TakeDamage(CharacterBody2D source, float damage);
}