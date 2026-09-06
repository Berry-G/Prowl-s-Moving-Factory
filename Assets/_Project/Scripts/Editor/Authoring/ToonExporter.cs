using System.IO;
using UnityEditor;
using UnityEngine;
using PMF.Data;
using PMF.Grid;

namespace PMF.EditorTools.Authoring
{
    /**
 * 목적: Stage_Greybox.asset + GreyboxMapData 를 읽어 StageDocument DTO 를 만들고 ToonWriter 로 출력.
 * 왜 이 구조인가: 익스포터는 .asset 실측값을 읽는다. GreyboxFactory 의 초기값(튜닝 전)이 아니다.
 * 바꾸면 안 되는 것: .asset 의 float 값을 double 로 올려서 찍지 마라 (ToString("R") 은 float 로).
 * 근거: SDD-05 §8 [D-05-08], ADR-E10
 */
    public static class ToonExporter
    {
        const string OutputPath = "Assets/_Project/Data/Stages/Stage_Greybox.toon";

        [MenuItem("PMF/Export .toon")]
        public static void Export()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(GreyboxFactory.StageAssetPath);
            if (stage == null)
            {
                Debug.LogError("[ToonExporter] Stage_Greybox.asset 를 찾을 수 없습니다");
                return;
            }

            var doc = new StageDocument
            {
                Schema = "pmf.stage/1",
                Name = "Stage_Greybox",
                Map = BuildMap(),
                Path = BuildPath(),
                Spawn = BuildSpawn(stage),
                Burst = BuildBurst(stage),
                Economy = BuildEconomy(stage),
                Escortee = BuildEscortee(stage),
                Mother = BuildMother(stage),
                Presentation = BuildPresentation(stage),
                Toggles = BuildToggles(stage),
            };

            string toon = ToonWriter.Write(doc);
            File.WriteAllText(OutputPath, toon);
            AssetDatabase.Refresh();
            Debug.Log($"[ToonExporter] {OutputPath} ({toon.Length} 바이트)");
        }

        static StageDocument.MapDef BuildMap()
        {
            int w = GreyboxMapData.Width, h = GreyboxMapData.Height;
            var cells = new CellType[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    cells[y * w + x] = GreyboxMapData.GetCategory(x, y) switch
                    {
                        GreyboxMapData.Category.Empty => CellType.Empty,
                        GreyboxMapData.Category.Ground => CellType.Ground,
                        GreyboxMapData.Category.Road => CellType.Road,
                        GreyboxMapData.Category.Buildable => CellType.Buildable,
                        GreyboxMapData.Category.Village => CellType.Village,
                        GreyboxMapData.Category.Blocked => CellType.Blocked,
                        GreyboxMapData.Category.Water => CellType.Water,
                        _ => CellType.Empty,
                    };
            return new StageDocument.MapDef
            {
                Width = w, Height = h,
                OriginX = GreyboxMapData.Origin.x, OriginY = GreyboxMapData.Origin.y,
                Cells = cells,
            };
        }

        static StageDocument.PathDef BuildPath()
        {
            var nodes = new StageDocument.PathNodeDef[GreyboxMapData.Nodes.Length];
            for (int i = 0; i < GreyboxMapData.Nodes.Length; i++)
            {
                var nd = GreyboxMapData.Nodes[i];
                string role;
                if (nd.IsStart) role = "start";
                else if (nd.IsExit) role = "exit";
                else if (nd.Name.StartsWith("B")) role = "branch";
                else role = "waypoint";
                nodes[i] = new StageDocument.PathNodeDef
                {
                    Id = nd.Name, Role = role,
                    X = nd.Cell.x, Y = nd.Cell.y,
                };
            }

            int edgeCount = GreyboxMapData.Edges.Length + 1; // +1 for shortcut
            var edges = new StageDocument.PathEdgeDef[edgeCount];
            for (int i = 0; i < GreyboxMapData.Edges.Length; i++)
            {
                var (from, to) = GreyboxMapData.Edges[i];
                edges[i] = new StageDocument.PathEdgeDef
                {
                    From = GreyboxMapData.Nodes[from].Name,
                    To = GreyboxMapData.Nodes[to].Name,
                    Allowed = "All",
                    Bidirectional = true,
                    Shortcut = false,
                };
            }
            var (sf, st) = GreyboxMapData.ShortcutEdge;
            edges[edgeCount - 1] = new StageDocument.PathEdgeDef
            {
                From = GreyboxMapData.Nodes[sf].Name,
                To = GreyboxMapData.Nodes[st].Name,
                Allowed = "Escortee",
                // 왜 true 인가: 게임이 지름길을 포함한 모든 엣지를 양방향으로 만든다
                //   (출처: SceneParts.cs:264 — Bidirectional 을 무조건 true 로 쓴다).
                //   씨앗도 'N05,N08,Escortee,true,true' 다. 게임이 진실이다.
                Bidirectional = true,
                Shortcut = true,
            };

            return new StageDocument.PathDef { Nodes = nodes, Edges = edges };
        }

        static StageDocument.SpawnDef BuildSpawn(StageDefinition stage)
        {
            var table = stage.SpawnTable;
            var rows = new StageDocument.SpawnTableRow[table.Count];
            for (int i = 0; i < table.Count; i++)
                rows[i] = new StageDocument.SpawnTableRow
                {
                    // 왜 .name 인가: 파일은 EnemyDefinition 의 **에셋 이름**(Robot_Walker)을 참조한다.
                    //   DisplayName 은 화면에 보이는 한글 이름("워커")이라 임포터가 에셋을 못 찾는다 (SDD-02 §4).
                    Enemy = table[i].Enemy.name,
                    Weight = table[i].Weight,
                };

            // 왜 SerializedObject 인가: _enemyHealthByProgress 는 private 이고 StageDefinition 에는
            //   키를 읽는 접근자가 없다. 익스포터는 에디터 전용이라 SerializedObject 로 읽어도 된다.
            //   StageDefinition 에 필드를 더하지 않는다 (SDD-05 §4).
            var curve = new SerializedObject(stage).FindProperty("_enemyHealthByProgress").animationCurveValue;
            var hp = new StageDocument.HealthPoint[curve.length > 0 ? curve.length : 2];
            if (curve.length > 0)
                for (int i = 0; i < curve.length; i++)
                    hp[i] = new StageDocument.HealthPoint { T = curve[i].time, Mul = curve[i].value };
            else
            {
                // 곡선이 비어 있으면 게임의 기본값과 같은 상수곡선으로 본다 (StageDefinition.cs:30).
                hp[0] = new StageDocument.HealthPoint { T = 0, Mul = 1 };
                hp[1] = new StageDocument.HealthPoint { T = 1, Mul = 1 };
            }

            return new StageDocument.SpawnDef
            {
                VolleyCount = stage.SpawnVolleyCount,
                VolleySpacing = stage.SpawnVolleySpacing,
                RestSeconds = stage.SpawnRestSeconds,
                TelegraphSeconds = stage.SpawnTelegraphSeconds,
                Table = rows,
                HealthByProgress = hp,
            };
        }

        static StageDocument.BurstDef BuildBurst(StageDefinition stage)
        {
            // 왜 루프인가: BurstTriggerNodeIds 는 IReadOnlyList<string> 라 CopyTo 가 없다.
            //   Linq ToArray 를 쓰면 되지만 using 하나를 더 늘리지 않는다.
            var triggers = new string[stage.BurstTriggerNodeIds.Count];
            for (int i = 0; i < triggers.Length; i++) triggers[i] = stage.BurstTriggerNodeIds[i];
            return new StageDocument.BurstDef
            {
                TriggerNodeIds = triggers,
                Duration = stage.BurstDuration,
                VolleyCount = stage.BurstVolleyCount,
                RestSeconds = stage.BurstRestSeconds,
                RecoverySpeedMultiplier = stage.BurstRecoverySpeedMultiplier,
                RecoverySeconds = stage.BurstRecoverySeconds,
            };
        }

        static StageDocument.EconomyDef BuildEconomy(StageDefinition stage)
        {
            var diffs = stage.Difficulties;
            var rows = new StageDocument.DifficultyRow[diffs.Count];
            for (int i = 0; i < diffs.Count; i++)
            {
                var d = diffs[i];
                rows[i] = new StageDocument.DifficultyRow
                {
                    Difficulty = d.Difficulty.ToString(),
                    DisplayName = d.DisplayName,
                    KillReward = d.KillReward,
                    ResourcePerSecond = d.ResourcePerSecond,
                };
            }
            return new StageDocument.EconomyDef
            {
                StartingResource = stage.StartingResource,
                ShortcutCost = stage.ShortcutCost,
                Difficulties = rows,
            };
        }

        static StageDocument.EscorteeDef BuildEscortee(StageDefinition stage)
            => new() { Speed = stage.EscorteeSpeed, MaxHealth = stage.EscorteeMaxHealth };

        static StageDocument.MotherDef BuildMother(StageDefinition stage)
            => new() { Speed = stage.MotherSpeed, SpawnDelay = stage.MotherSpawnDelay, FollowsPath = true };

        static StageDocument.PresentationDef BuildPresentation(StageDefinition stage)
            => new()
            {
                UiSlowMotionScale = stage.UiSlowMotionScale,
                ShotLineSeconds = stage.ShotLineSeconds,
                MagicMissileSpeed = stage.MagicMissileSpeed,
                HitFlashSeconds = stage.HitFlashSeconds,
                DebrisCount = stage.DebrisCount,
                DebrisSeconds = stage.DebrisSeconds,
                HealthBarHideWhenFull = stage.HealthBarHideWhenFull,
                MasterVolume = stage.MasterVolume,
            };

        static StageDocument.ToggleDef BuildToggles(StageDefinition stage)
            => new()
            {
                AlliesCanDieWhileMarching = stage.AlliesCanDieWhileMarching,
                EnemiesTargetAllies = stage.EnemiesTargetAllies,
            };
    }
}
