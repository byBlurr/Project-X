using Godot;

namespace ProjectX.Interfaces;

public interface IHurtable
{
    public bool TakeDamage(Node2D source, float damage);
}