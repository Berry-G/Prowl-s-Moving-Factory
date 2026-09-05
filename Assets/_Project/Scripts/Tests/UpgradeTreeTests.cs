using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PMF.Combat;
using PMF.Data;

namespace PMF.Tests
{
    /// <summary>고양이 Lv1~Lv4 업그레이드 트리 (ADR-0020).
    ///
    /// 실제 에셋(<c>Ally_CatFolk.asset</c>)이 아니라 코드로 만든 SO 를 쓴다 — 기획자가 밸런스 수치를
    /// 바꿀 때마다 테스트가 깨지면 안 되기 때문이다. 여기서 지키려는 것은 <b>구조</b>다:
    /// 분기가 갈리는가, 한 번 고른 갈래에 갇히는가, 처형이 보스를 비껴가는가.</summary>
    public class UpgradeTreeTests
    {
        private UnitDefinition _def;
        private readonly List<int> _options = new List<int>();

        /// <summary>테스트용 트리. 인덱스 = [0] Lv2 공통 · [1] Lv3-A · [2] Lv3-B · [3] Lv4-A · [4] Lv4-B.</summary>
        [SetUp]
        public void SetUp()
        {
            _def = ScriptableObject.CreateInstance<UnitDefinition>();

            var tiers = new UpgradeTier[5];
            SetTier(ref tiers[0], "Lv2", level: 2, branch: 0);
            SetTier(ref tiers[1], "Lv3-A", level: 3, branch: 1);
            SetTier(ref tiers[2], "Lv3-B", level: 3, branch: 2);
            SetTier(ref tiers[3], "Lv4-A", level: 4, branch: 1);
            SetTier(ref tiers[4], "Lv4-B", level: 4, branch: 2);

            SetPrivate(_def, "_tiers", tiers);
        }

        [TearDown]
        public void TearDown()
        {
            if (_def != null) Object.DestroyImmediate(_def);
            ScavengeRegistry.Clear();
        }

        // ---------- 트리 탐색 ----------

        [Test]
        public void Lv1_에서는_공통_한_갈래만_보인다()
        {
            _def.CollectNextTiers(currentLevel: 1, branch: 0, _options);
            CollectionAssert.AreEqual(new[] { 0 }, _options);
        }

        [Test]
        public void Lv2_에서는_분기_둘이_모두_보인다()
        {
            _def.CollectNextTiers(currentLevel: 2, branch: 0, _options);
            CollectionAssert.AreEqual(new[] { 1, 2 }, _options);
        }

        [Test]
        public void 갈래를_고르면_그_갈래의_Lv4_만_보인다()
        {
            _def.CollectNextTiers(currentLevel: 3, branch: 1, _options);
            CollectionAssert.AreEqual(new[] { 3 }, _options);

            _def.CollectNextTiers(currentLevel: 3, branch: 2, _options);
            CollectionAssert.AreEqual(new[] { 4 }, _options);
        }

        [Test]
        public void Lv4_가_최대다()
        {
            _def.CollectNextTiers(currentLevel: 4, branch: 1, _options);
            Assert.IsEmpty(_options);
        }

        [Test]
        public void Level_이_비어_있으면_인덱스로_읽는다()
        {
            // 분기가 없던 옛 선형 데이터(쥐 수인)가 그대로 돌아야 한다.
            var linear = ScriptableObject.CreateInstance<UnitDefinition>();
            var tiers = new UpgradeTier[2];
            SetTier(ref tiers[0], "old-1", level: 0, branch: 0);
            SetTier(ref tiers[1], "old-2", level: 0, branch: 0);
            SetPrivate(linear, "_tiers", tiers);

            Assert.AreEqual(2, linear.LevelOf(0));
            Assert.AreEqual(3, linear.LevelOf(1));

            linear.CollectNextTiers(currentLevel: 1, branch: 0, _options);
            CollectionAssert.AreEqual(new[] { 0 }, _options);
            linear.CollectNextTiers(currentLevel: 2, branch: 0, _options);
            CollectionAssert.AreEqual(new[] { 1 }, _options);

            Object.DestroyImmediate(linear);
        }

        // ---------- 처형 (Lv3-2 / Lv4-2) ----------

        [Test]
        public void 임계_이상이면_평타다()
        {
            float dmg = Attacker.ResolveHitDamage(10f, current: 30f, max: 100f, isBoss: false,
                                                  executeThreshold: 0.05f, bossCritMultiplier: 2f,
                                                  out bool executed);
            Assert.AreEqual(10f, dmg);
            Assert.IsFalse(executed);
        }

        [Test]
        public void 임계_미만이면_남은_체력만큼_때려_즉사시킨다()
        {
            float dmg = Attacker.ResolveHitDamage(10f, current: 4f, max: 100f, isBoss: false,
                                                  executeThreshold: 0.05f, bossCritMultiplier: 2f,
                                                  out bool executed);
            Assert.AreEqual(4f, dmg);
            Assert.IsTrue(executed);
        }

        [Test]
        public void 임계_바로_위는_처형되지_않는다()
        {
            // 경계값(정확히 5%)은 일부러 규정하지 않는다 — float 에서 5/100 은 0.05f 와 같지 않아
            // 어느 쪽으로 떨어질지가 오차에 달렸고, 게임적으로도 구분할 이유가 없다.
            float dmg = Attacker.ResolveHitDamage(10f, current: 5.5f, max: 100f, isBoss: false,
                                                  executeThreshold: 0.05f, bossCritMultiplier: 2f,
                                                  out bool executed);
            Assert.AreEqual(10f, dmg);
            Assert.IsFalse(executed);
        }

        [Test]
        public void 보스는_처형_대신_치명타를_받는다()
        {
            float dmg = Attacker.ResolveHitDamage(10f, current: 4f, max: 100f, isBoss: true,
                                                  executeThreshold: 0.05f, bossCritMultiplier: 2.5f,
                                                  out bool executed);
            Assert.AreEqual(25f, dmg);
            Assert.IsFalse(executed, "보스는 즉사하지 않는다");
        }

        [Test]
        public void 처형이_없으면_임계를_보지_않는다()
        {
            float dmg = Attacker.ResolveHitDamage(10f, current: 1f, max: 100f, isBoss: false,
                                                  executeThreshold: 0f, bossCritMultiplier: 2f,
                                                  out bool executed);
            Assert.AreEqual(10f, dmg);
            Assert.IsFalse(executed);
        }

        // ---------- 손기술 (Lv2) ----------

        [Test]
        public void 반경_밖에서_죽으면_보상이_그대로다()
        {
            var owner = new GameObject("owner");
            ScavengeRegistry.Register(owner, Vector3.zero, radius: 2f, bonus: 0.5f);

            Assert.AreEqual(1f, ScavengeRegistry.MultiplierAt(new Vector3(5f, 0f, 0f)));

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void 반경_안에서_죽으면_보상이_오른다()
        {
            var owner = new GameObject("owner");
            ScavengeRegistry.Register(owner, Vector3.zero, radius: 2f, bonus: 0.5f);

            Assert.AreEqual(1.5f, ScavengeRegistry.MultiplierAt(new Vector3(1f, 0f, 0f)));

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void 반경이_겹쳐도_합산하지_않고_가장_큰_것만_쓴다()
        {
            var a = new GameObject("a");
            var b = new GameObject("b");
            ScavengeRegistry.Register(a, Vector3.zero, radius: 2f, bonus: 0.5f);
            ScavengeRegistry.Register(b, Vector3.zero, radius: 2f, bonus: 0.8f);

            Assert.AreEqual(1.8f, ScavengeRegistry.MultiplierAt(Vector3.zero),
                            "합산하면 한 칸에 몰아 놓는 것이 정답이 되어 배치 판단이 사라진다");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void 같은_소유자를_다시_등록하면_갱신된다()
        {
            var owner = new GameObject("owner");
            ScavengeRegistry.Register(owner, Vector3.zero, radius: 2f, bonus: 0.5f);
            ScavengeRegistry.Register(owner, new Vector3(10f, 0f, 0f), radius: 2f, bonus: 0.5f);

            Assert.AreEqual(1f, ScavengeRegistry.MultiplierAt(Vector3.zero), "옛 자리가 남으면 안 된다");
            Assert.AreEqual(1.5f, ScavengeRegistry.MultiplierAt(new Vector3(10f, 0f, 0f)));

            Object.DestroyImmediate(owner);
        }

        [Test]
        public void 해제하면_보상이_돌아온다()
        {
            var owner = new GameObject("owner");
            ScavengeRegistry.Register(owner, Vector3.zero, radius: 2f, bonus: 0.5f);
            ScavengeRegistry.Unregister(owner);

            Assert.AreEqual(1f, ScavengeRegistry.MultiplierAt(Vector3.zero));

            Object.DestroyImmediate(owner);
        }

        // ---------- 도우미 ----------

        /// <summary>구조체라서 박싱한 뒤 리플렉션으로 채우고 되돌려 담는다 —
        /// 박싱하지 않고 <c>SetValue</c> 를 부르면 복사본에 쓰고 사라진다.</summary>
        private static void SetTier(ref UpgradeTier tier, string name, int level, int branch)
        {
            object boxed = tier;
            SetPrivate(boxed, "_displayName", name);
            SetPrivate(boxed, "_level", level);
            SetPrivate(boxed, "_branch", branch);
            tier = (UpgradeTier)boxed;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var info = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(info, $"필드 {field} 를 찾지 못했다");
            info.SetValue(target, value);
        }
    }
}
