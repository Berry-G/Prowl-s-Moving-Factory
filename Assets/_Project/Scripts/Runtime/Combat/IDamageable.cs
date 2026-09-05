using UnityEngine;

namespace PMF.Combat
{
    public interface IDamageable
    {
        Team Team { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }

        /// <summary>현재 체력. 처형(ADR-0020)이 잔여 비율을 보므로 인터페이스에 있다.</summary>
        float Current { get; }

        /// <summary>최대 체력. 0 이면 비율을 계산하지 마라.</summary>
        float Max { get; }

        /// <summary>보스인가. 보스는 처형되지 않고 대신 치명타를 받는다 (ADR-0020).</summary>
        bool IsBoss { get; }

        void TakeDamage(float amount, object source);
    }

    /// <summary>대상을 프레임 너머로 들고 있는 쪽(연출 등)이 쓰는 안전장치.</summary>
    public static class DamageableExtensions
    {
        /// <summary>이 대상에 접근해도 되는가.
        ///
        /// ⚠️ <b><see cref="IDamageable"/> 은 인터페이스라 Unity 의 "파괴됨 = null" 오버로드가 걸리지 않는다.</b>
        /// 인터페이스로 담아 둔 <see cref="Health"/> 는 GameObject 가 파괴된 뒤에도 <c>!= null</c> 이 참이고,
        /// <c>Position</c> 처럼 네이티브를 건드리는 멤버에 닿는 순간 <c>MissingReferenceException</c> 이 난다.
        /// 실제로 적을 죽인 직후의 <see cref="UI.MagicMissile"/> 이 이걸로 터졌다 (2026-09-02).
        ///
        /// <c>IsAlive</c> 만으로는 부족하다 — 그건 "죽었다"이고 이건 "객체가 사라졌다"라 서로 다르다.</summary>
        public static bool IsUsable(this IDamageable target)
        {
            if (target is UnityEngine.Object obj) return obj != null;   // 여기서만 Unity 오버로드가 걸린다
            return target != null;
        }
    }
}
