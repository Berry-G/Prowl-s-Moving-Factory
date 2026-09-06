using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PMF.EditorTools.Authoring
{
    /**
     * 목적: StageDocument → TOON 정규 출력. 바이트 동일이 목표.
     * 왜 이 구조인가: TS encode.ts 의 C# 포팅. 같은 입력 = 같은 출력이어야 TS·C# 두 구현이 같은 계약을 본 증거가 된다.
     * 바꾸면 안 되는 것: 섹션·키 순서, formatNumber (float.ToString("R")), 주석 문구·들여쓰기. 씨앗과 바이트가 달라진다.
     * 근거: SDD-05 §3 [D-05-03], SDD-02 §6-1 [D-02-08], ADR-E10
     */
    public static class ToonWriter
    {
        // 주석은 씨앗 파일(Stage_Greybox.toon)과 정확히 같아야 한다.
        const string H1 = "# Prowl's Moving Factory — 스테이지 데이터 (pmf.stage/1)";
        const string H2 = "# 이 파일은 PMF Editor 가 만들고 읽는다. 손으로 고쳐도 되지만 저장은 툴로 하는 편이 안전하다.";
        const string H3 = "# 출처: 게임 프로젝트 2026-09-02 상태 — GreyboxMapData.cs(맵·경로) + Stage_Greybox.asset(수치)";
        const string H4 = "#       씨앗값(GreyboxFactory)이 아니라 .asset 실측값이다. 씨앗값은 튜닝 전 숫자라 믿으면 안 된다.";
        const string M1 = "# --- 맵 --------------------------------------------------------------";
        const string M2 = "# 문자: _ 빈칸(타일 없음 = 맵 밖, 런타임 Blocked)  . 땅  R 도로  B 배치가능  V 마을  W 벽  ~ 물";
        const string M3 = "# 맨 위 행이 y = height-1 (화면에 보이는 그대로). 열 인덱스가 x.";
        const string M4 = "# 벽을 '#' 로 쓰지 않는 이유: TOON 은 '#' 로 시작하는 줄을 주석으로 지운다.";
const string P1 = "# --- 경로 --------------------------------------------------------------";
        const string P2 = "# id 는 게임의 GameObject 이름이 된다. 버스트 트리거가 이 이름으로 정확히 매칭하므로 툴은 재번호하지 않는다.";
        const string P3 = "# role: start(보호대상·모체 출발) / exit(탈출) / branch(도로 밖 분기, 아군 행군용) / waypoint(그 외)";
        const string P4 = "# 코너 노드는 도로가 꺾이는 셀에 정확히 놓여야 한다 — 한 칸 어긋나면 직선 구간이 코너를 대각선으로 가로지른다.";
        const string E1 = "  # allowed: All | Escortee | Enemy | Ally, 여러 개면 '+' 로 잇는다. 양방향 엣지는 한 번만 적는다 (게임이 반대 방향을 만든다).";
        const string E2 = "  # 지름길은 반드시 Escortee 만 — 적이 지나가면 지름길의 존재 이유가 사라진다 (ADR-0004).";
        const string S1 = "# --- 스폰 --------------------------------------------------------------";
        const string S2 = "# 리듬 = 묶음(volley) + 휴지(rest). 웨이브가 아니라 '호흡' 이다 (회의 결정 2).";
        const string S3 = "# 사이클 = (volleyCount-1)*volleySpacing + restSeconds = 9.4초에 4마리 → 0.426 마리/초";
        const string T1 = "  # enemy 는 EnemyDefinition 에셋 이름. guid 는 넣지 않는다 — 기획자가 다룰 수 없고 에셋을 옮기면 깨진다.";
        const string H5 = "  # 진행도(0~1)별 적 체력 배율. Unity AnimationCurve 를 (t, 배율) 표로 편 것. 난이도와 곱하지 않는다.";
        const string B1 = "# 버스트 = 모체가 추적을 멈추고 생산에 몰빵. 총량 보존 — 밀도만 바뀐다 (GDD §7).";
        const string N1 = "# --- 경제 --------------------------------------------------------------";
        const string N2 = "# 난이도는 경제로만 조절한다. 적 체력·데미지에 배율을 걸지 않는다 (ADR-0018).";
        const string O1 = "# 모체는 보호대상과 같은 start 노드에서 출발한다 (추격자 그림). 속도는 반드시 보호대상보다 느려야 한다.";
        const string G1 = "# GDD §13 미결정 사항 실험 토글 (D-03, D-04)";

        // TS CELL_TO_CHAR 매핑
        static readonly Dictionary<CellType, char> CharMap = new()
        {
            { CellType.Empty, '_' }, { CellType.Ground, '.' }, { CellType.Road, 'R' },
            { CellType.Buildable, 'B' }, { CellType.Village, 'V' }, { CellType.Blocked, 'W' }, { CellType.Water, '~' },
        };

        // TS formatNumber 에 맞춤: float.ToString("R") 로 최단 표현 (JS String(n) 과 동일).
        // float 으로 내리는 이유: .asset 에서 읽은 값이 float 이므로 double.log(0.42f) ≈ 0.419999986886978,
        //   ToString(\"G\") 는 \"0.419999986886978\" 가 나오지만 \"R\" 은 \"0.42\" 로 복원한다 (SDD-09 §2-3).
        static string FmtNum(double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v))
                throw new ArgumentException($"숫자가 아님: {v}");
            if (v == 0 && 1.0 / v < 0) return "0"; // -0 → 0 (JS Object.is)
            string s = ((float)v).ToString("R", CultureInfo.InvariantCulture);
            if (s.Contains('e') || s.Contains('E'))
                throw new ArgumentException($"지수 표기 금지: {s} (값 {v})");
            return s;
        }

        static bool NeedsQuote(string v)
        {
            if (string.IsNullOrEmpty(v)) return true;
            if (v.Trim() != v) return true;
            if (v == "true" || v == "false" || v == "null") return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^[+-]?[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?$")) return true;
            if (System.Text.RegularExpressions.Regex.IsMatch(v, @"[:""\\[\]{},]")) return true;
            foreach (char c in v) if (c < 0x20) return true;
            if (v.StartsWith("-") || v.StartsWith("#")) return true;
            return false;
        }
        static string Str(string v)
        {
            if (!NeedsQuote(v)) return v;
            var sb = new StringBuilder("\"");
            foreach (char ch in v)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append($"\\u{(int)ch:x4}");
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        static string BoolStr(bool v) => v ? "true" : "false";
        static void AddLine(List<string> lines, string l) => lines.Add(l);
        static void AddRange(List<string> lines, IEnumerable<string> items) { foreach (var l in items) lines.Add(l); }

        public static string Write(StageDocument doc)
        {
            var lines = new List<string>();
            AddRange(lines, new[] { H1, H2, H3, H4 });
            AddLine(lines, "schema: " + doc.Schema);
            AddLine(lines, "name: " + Str(doc.Name));
            AddLine(lines, "");
            WriteMap(doc.Map, lines);
            AddLine(lines, "");
            WritePath(doc.Path, lines);
            AddLine(lines, "");
            WriteSpawn(doc.Spawn, lines);
            AddLine(lines, "");
            WriteBurst(doc.Burst, lines);
            AddLine(lines, "");
            WriteEconomy(doc.Economy, lines);
            AddLine(lines, "");
            WriteEscortee(doc.Escortee, lines);
            AddLine(lines, "");
            WriteMother(doc.Mother, lines);
            AddLine(lines, "");
            WritePresentation(doc.Presentation, lines);
            AddLine(lines, "");
            WriteToggles(doc.Toggles, lines);
            return string.Join("\n", lines) + "\n";
        }
        static void WriteMap(StageDocument.MapDef map, List<string> lines)
        {
            AddRange(lines, new[] { M1, M2, M3, M4 });
            AddLine(lines, "map:");
            AddLine(lines, "  width: " + FmtNum(map.Width));
            AddLine(lines, "  height: " + FmtNum(map.Height));
            AddLine(lines, "  origin[2]: " + FmtNum(map.OriginX) + "," + FmtNum(map.OriginY));
            AddLine(lines, "  rows[" + FmtNum(map.Height) + "]{row}:");
            for (int r = 0; r < map.Height; r++)
            {
                int y = map.Height - 1 - r;
                var sb = new StringBuilder("    ");
                for (int x = 0; x < map.Width; x++)
                {
                    var cell = map.Cells[y * map.Width + x];
                    sb.Append(CharMap.TryGetValue(cell, out char c) ? c : '_');
                }
                AddLine(lines, sb.ToString());
            }
        }

        static void WritePath(StageDocument.PathDef path, List<string> lines)
        {
            AddRange(lines, new[] { P1, P2, P3, P4 });
            AddLine(lines, "path:");
            // 열 순서는 씨앗 파일이 정본이다: id,x,y,role (role 이 마지막).
            AddLine(lines, "  nodes[" + FmtNum(path.Nodes.Length) + "]{id,x,y,role}:");
            for (int i = 0; i < path.Nodes.Length; i++)
            {
                var n = path.Nodes[i];
                AddLine(lines, "    " + Str(n.Id) + "," + FmtNum(n.X) + "," + FmtNum(n.Y) + "," + n.Role);
            }
            // 왜 주석이 먼저인가: 씨앗은 표 주석을 헤더 **위** 에 둔다. 순서가 바뀌면 바이트가 달라진다.
            AddLine(lines, E1);
            AddLine(lines, E2);
            AddLine(lines, "  edges[" + FmtNum(path.Edges.Length) + "]{from,to,allowed,bidirectional,shortcut}:");
            for (int i = 0; i < path.Edges.Length; i++)
            {
                var e = path.Edges[i];
                // 왜 그대로 쓰나: 씨앗은 'All' 을 'All' 로 적는다. 'Escortee+Enemy+Ally' 로 펴면
                //   의미는 같아도 바이트가 달라진다. 축약형이 정본이다 (SDD-02 §4).
                AddLine(lines, "    " + Str(e.From) + "," + Str(e.To) + "," + e.Allowed + "," + BoolStr(e.Bidirectional) + "," + BoolStr(e.Shortcut));
            }
        }

        static void WriteSpawn(StageDocument.SpawnDef spawn, List<string> lines)
        {
            AddRange(lines, new[] { S1, S2, S3 });
            AddLine(lines, "spawn:");
            AddLine(lines, "  volleyCount: " + FmtNum(spawn.VolleyCount));
            AddLine(lines, "  volleySpacing: " + FmtNum(spawn.VolleySpacing));
            AddLine(lines, "  restSeconds: " + FmtNum(spawn.RestSeconds));
            AddLine(lines, "  telegraphSeconds: " + FmtNum(spawn.TelegraphSeconds));
            AddLine(lines, T1);
            AddLine(lines, "  table[" + FmtNum(spawn.Table.Length) + "]{enemy,weight}:");
            for (int i = 0; i < spawn.Table.Length; i++)
            {
                var row = spawn.Table[i];
                AddLine(lines, "    " + Str(row.Enemy) + "," + FmtNum(row.Weight));
            }
            AddLine(lines, H5);
            AddLine(lines, "  healthByProgress[" + FmtNum(spawn.HealthByProgress.Length) + "]{t,mul}:");
            for (int i = 0; i < spawn.HealthByProgress.Length; i++)
            {
                var hp = spawn.HealthByProgress[i];
                AddLine(lines, "    " + FmtNum(hp.T) + "," + FmtNum(hp.Mul));
            }
        }

        static void WriteBurst(StageDocument.BurstDef burst, List<string> lines)
        {
            AddRange(lines, new[] { B1 });
            AddLine(lines, "burst:");
            AddLine(lines, "  triggerNodeIds[" + FmtNum(burst.TriggerNodeIds.Length) + "]: " + string.Join(",", Array.ConvertAll(burst.TriggerNodeIds, Str)));
            AddLine(lines, "  duration: " + FmtNum(burst.Duration));
            AddLine(lines, "  volleyCount: " + FmtNum(burst.VolleyCount));
            AddLine(lines, "  restSeconds: " + FmtNum(burst.RestSeconds));
            AddLine(lines, "  recoverySpeedMultiplier: " + FmtNum(burst.RecoverySpeedMultiplier));
            AddLine(lines, "  recoverySeconds: " + FmtNum(burst.RecoverySeconds));
        }

        static void WriteEconomy(StageDocument.EconomyDef economy, List<string> lines)
        {
            AddRange(lines, new[] { N1, N2 });
            AddLine(lines, "economy:");
            AddLine(lines, "  startingResource: " + FmtNum(economy.StartingResource));
            AddLine(lines, "  shortcutCost: " + FmtNum(economy.ShortcutCost));
            AddLine(lines, "  difficulties[" + FmtNum(economy.Difficulties.Length) + "]{difficulty,displayName,killReward,resourcePerSecond}:");
            for (int i = 0; i < economy.Difficulties.Length; i++)
            {
                var d = economy.Difficulties[i];
                AddLine(lines, "    " + d.Difficulty + "," + Str(d.DisplayName) + "," + FmtNum(d.KillReward) + "," + FmtNum(d.ResourcePerSecond));
            }
        }
        static void WriteEscortee(StageDocument.EscorteeDef escortee, List<string> lines)
        {
            AddLine(lines, "escortee:");
            AddLine(lines, "  speed: " + FmtNum(escortee.Speed));
            AddLine(lines, "  maxHealth: " + FmtNum(escortee.MaxHealth));
        }

        static void WriteMother(StageDocument.MotherDef mother, List<string> lines)
        {
            AddRange(lines, new[] { O1 });
            AddLine(lines, "mother:");
            AddLine(lines, "  speed: " + FmtNum(mother.Speed));
            AddLine(lines, "  spawnDelay: " + FmtNum(mother.SpawnDelay));
            AddLine(lines, "  followsPath: " + BoolStr(mother.FollowsPath));
        }

        static void WritePresentation(StageDocument.PresentationDef pr, List<string> lines)
        {
            AddLine(lines, "presentation:");
            AddLine(lines, "  uiSlowMotionScale: " + FmtNum(pr.UiSlowMotionScale));
            AddLine(lines, "  shotLineSeconds: " + FmtNum(pr.ShotLineSeconds));
            AddLine(lines, "  magicMissileSpeed: " + FmtNum(pr.MagicMissileSpeed));
            AddLine(lines, "  hitFlashSeconds: " + FmtNum(pr.HitFlashSeconds));
            AddLine(lines, "  debrisCount: " + FmtNum(pr.DebrisCount));
            AddLine(lines, "  debrisSeconds: " + FmtNum(pr.DebrisSeconds));
            AddLine(lines, "  healthBarHideWhenFull: " + BoolStr(pr.HealthBarHideWhenFull));
            AddLine(lines, "  masterVolume: " + FmtNum(pr.MasterVolume));
        }

        static void WriteToggles(StageDocument.ToggleDef toggles, List<string> lines)
        {
            AddRange(lines, new[] { G1 });
            AddLine(lines, "toggles:");
            AddLine(lines, "  alliesCanDieWhileMarching: " + BoolStr(toggles.AlliesCanDieWhileMarching));
            AddLine(lines, "  enemiesTargetAllies: " + BoolStr(toggles.EnemiesTargetAllies));
        }
    }
}
