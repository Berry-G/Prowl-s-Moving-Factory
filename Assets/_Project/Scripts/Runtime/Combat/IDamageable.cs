using UnityEngine;

namespace PMF.Combat
{
    public interface IDamageable
    {
        Team Team { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        void TakeDamage(float amount, object source);
    }
}
