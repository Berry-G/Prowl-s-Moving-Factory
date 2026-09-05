using System.IO;
using UnityEditor;
using UnityEngine;
using PMF.Data;
using PMF.Grid;

namespace PMF.EditorTools.Authoring
{
    /// <summary>
    /// Stage_Greybox.asset + GreyboxMapData → .toon 출력.
    /// 설계 정본: 에디터 레포 docs/SDD-05-저작파이프라인.md
    /// </summary>
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
                Bidirectional = false,
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
                    Enemy = table[i].Enemy.DisplayName,
                    Weight = table[i].Weight,
                };

            // Hard-code: Stage_Greybox.asset 은 상수곡선 (0→1, 1→1)
            var hp = new StageDocument.HealthPoint[]
            {
                new() { T = 0, Mul = 1 },
                new() { T = 1, Mul = 1 },
            };

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
            var triggers = new string[stage.BurstTriggerNodeIds.Count];
            stage.BurstTriggerNodeIds.CopyTo(triggers, 0);
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
        }