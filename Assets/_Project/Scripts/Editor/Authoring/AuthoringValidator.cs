/**
 * 목적: StageDocument 검증. SDD-02 §5 규칙표 28개 구현.
 * 왜 이 구조인가: 메시지 문자열은 TS src/core/validate/*.ts 와 같아야 한다.
 * 바꾸면 안 되는 것: 규칙 ID/심각도/메시지. V-S01 은 by-mode.
 * 근거: SDD-05 §4 [D-05-04], SDD-02 §5 [D-02-06]
 */
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace PMF.EditorTools.Authoring
{
    public struct Issue { public string Id, Severity, Path, Message; }
    public static class AuthoringValidator
    {
        struct Pt { public int x,y; }
        static List<Issue> _is;
        static void A(string id, string sev, string p, string m) => _is.Add(new Issue{Id=id,Severity=sev,Path=p,Message=m});
        public static List<Issue> Validate(StageDocument doc, HashSet<string> en)
        {
            _is = new List<Issue>(); var map = doc.Map; var p = doc.Path; var es = p.Edges; var ns = p.Nodes;
            var sp = doc.Spawn; var ec = doc.Economy; var bu = doc.Burst;

            // V-F02
            if (string.IsNullOrEmpty(doc.Name) || Regex.IsMatch(doc.Name, @"[\\/:*?""<>|]") || doc.Name.Trim() != doc.Name)
                A("V-F02","error","name",$"name \"{doc.Name}\" 은(는) 파일 이름으로 쓸 수 없다");
            if (_is.Exists(i => i.Severity == "error")) return _is;

            // V-M03
            bool hv = false;
            foreach (var c in map.Cells) { if (c == CellType.Village) { hv = true; break; } }
            if (!hv) A("V-M03","error","map","마을(V)이 없다. 아군을 고용할 곳이 없다");
            // V-M04
            bool hb = false;
            foreach (var c in map.Cells) { if (c == CellType.Buildable) { hb = true; break; } }
            if (!hb) A("V-M04","error","map","배치 가능 칸(B)이 없다");
            // V-M08
            if (map.Width < 8 || map.Width > 256 || map.Height < 8 || map.Height > 256)
                A("V-M08","error","map",$"맵 크기 {map.Width}×{map.Height} — 8~256 사이여야 한다");

            // V-M05: road components
            var roadCells = new List<Pt>();
            for (int y=0;y<map.Height;y++) for (int x=0;x<map.Width;x++)
                if (map.Cells[y*map.Width+x]==CellType.Road) roadCells.Add(new Pt{x=x,y=y});
            if (roadCells.Count>0)
            {
                var seen=new HashSet<int>(); var comps=new List<List<Pt>>();
                foreach (var rc in roadCells)
                {
                    int ri=rc.y*map.Width+rc.x; if (seen.Contains(ri)) continue;
                    var f=FloodFill(map,rc.x,rc.y,c=>c==CellType.Road);
                    foreach (var c in f) seen.Add(c.y*map.Width+c.x);
                    comps.Add(f);
                }
                if (comps.Count>1)
                {
                    var second=comps[1];
                    A("V-M05","warning","map",$"도로가 {comps.Count}조각으로 끊겨 있다. 첫 조각 밖의 도로 칸 예: ({second[0].x},{second[0].y})");
                }
            }

            // V-M06/V-M07: reachability
            var reach = ComputeReach(map);
            int unreachable = reach.totalB - reach.reachB;
            float ratio = reach.totalB==0 ? 0 : (float)unreachable/reach.totalB;
            A("V-M06",ratio>0.5f?"warning":"info","",$"마을에서 갈 수 없는 배치 칸이 {unreachable}/{reach.totalB} ({System.Math.Round(ratio*100)}%) 다");
            foreach (var pv in reach.perV)
            {
                if (pv.bc==0)
                    A("V-M07","error","map",$"마을 ({pv.vx},{pv.vy}) 에서 갈 수 있는 배치 칸이 없다");
            }
            if (_is.Exists(i => i.Severity == "error")) return _is;

            // V-P02
            int sc = 0, xc = 0;
            string fe = "";
            foreach (var n in ns) { if (n.Role == "start") sc++; if (n.Role == "exit") { xc++; if (fe=="") fe=n.Id; } }
            if (sc != 1) A("V-P02","error","path",$"start 노드가 {sc}개다. 정확히 1개여야 한다");
            if (xc == 0) A("V-P02","error","path","exit 노드가 없다");
            else if (xc > 1) A("V-P02","warning","path",$"exit 노드가 {xc}개다. 게임은 첫 번째({fe})만 쓴다");

            // V-P09
            var idSet = new Dictionary<string, int>();
            for (int i = 0; i < ns.Length; i++)
            {
                if (!Regex.IsMatch(ns[i].Id,@"^[A-Za-z_][A-Za-z0-9_]*$"))
                    A("V-P09","error",$"path.nodes[{i}]",$"id \"{ns[i].Id}\" 형식 위반 (영문·숫자·_ 만, 숫자로 시작 불가)");
                else if (idSet.ContainsKey(ns[i].Id))
                    A("V-P09","error",$"path.nodes[{i}]",$"id \"{ns[i].Id}\" 가 path.nodes[{idSet[ns[i].Id]}] 와 중복");
                else idSet[ns[i].Id] = i;
            }
            if (_is.FindIndex(x=>x.Id=="V-P09")<0)
            {
                for (int i = 0; i < es.Length; i++)
                {
                    if (!idSet.ContainsKey(es[i].From)) A("V-P01","error",$"path.edges[{i}]",$"노드 \"{es[i].From}\" 가 없다");
                    if (!idSet.ContainsKey(es[i].To)) A("V-P01","error",$"path.edges[{i}]",$"노드 \"{es[i].To}\" 가 없다");
                }
                for (int i = 0; i < bu.TriggerNodeIds.Length; i++)
                    if (!idSet.ContainsKey(bu.TriggerNodeIds[i]))
                        A("V-P01","error",$"burst.triggerNodeIds[{i}]",$"노드 \"{bu.TriggerNodeIds[i]}\" 가 없다");
                for (int i = 0; i < es.Length; i++)
                    if (es[i].Shortcut && es[i].Allowed != "Escortee")
                        A("V-P04","error",$"path.edges[{i}]",$"지름길 allowed \"{es[i].Allowed}\"");
                for (int i = 0; i < es.Length; i++) for (int j = i+1; j < es.Length; j++)
                {
                    var a=es[i];var b=es[j];
                    var ak = a.Bidirectional ? (a.From.CompareTo(a.To)<0 ? a.From+"|"+a.To : a.To+"|"+a.From) : a.From+">"+a.To;
                    var bk = b.Bidirectional ? (b.From.CompareTo(b.To)<0 ? b.From+"|"+b.To : b.To+"|"+b.From) : b.From+">"+b.To;
                    if (ak==bk) A("V-P08","error",$"path.edges[{j}]",$"\"{b.From}↔{b.To}\" 가 path.edges[{i}] 와 중복이다. 양방향 엣지는 한 번만 적는다");
                }
                foreach (var tid in bu.TriggerNodeIds)
                { var fn = System.Array.Find(ns,n=>n.Id==tid); if (fn.Role=="start"||fn.Role=="exit") A("V-P06","warning","burst.triggerNodeIds", $"\"{fn.Id}\" 는 {fn.Role} 노드다. 시작 즉시/도착 후 버스트는 의미가 없다"); }
                // V-P07
                foreach (var n in ns)
                {
                    int idx = System.Array.IndexOf(ns,n);
                    if (!IB(map,n.X,n.Y)) { A("V-P07","error",$"path.nodes[{idx}]",$"\"{n.Id}\" 맵 밖이다"); continue; }
                    var cell = map.Cells[n.Y * map.Width + n.X];
                    if (n.Role == "branch")
                    { if (!AW(cell)) A("V-P07","error",$"path.nodes[{idx}]",$"\"{n.Id}\" ({n.X},{n.Y}) 은 {CN(cell)}. branch 노드는 통행 가능 칸(. B V)에 있어야 함"); }
                    else if (cell != CellType.Road)
                        A("V-P07","error",$"path.nodes[{idx}]",$"\"{n.Id}\" ({n.X},{n.Y}) 은 {CN(cell)}. start/exit/waypoint 노드는 도로(R) 위에 있어야 함");
                }
// V-P03: axis-aligned + road check
                for (int i=0;i<es.Length;i++)
                {
                    var e=es[i]; if (e.Shortcut) continue;
                    var fnIdx=System.Array.FindIndex(ns,n=>n.Id==e.From); var tnIdx=System.Array.FindIndex(ns,n=>n.Id==e.To);
                    if (fnIdx<0||tnIdx<0) continue; var fn=ns[fnIdx]; var tn=ns[tnIdx];
                    if (fn.Role=="branch"||tn.Role=="branch") continue;
                    if (fn.X!=tn.X&&fn.Y!=tn.Y)
                        A("V-P03","error",$"path.edges[{i}]",$"\"{e.From}→{e.To}\" — 대각선 엣지다. 일반 엣지는 가로 또는 세로여야 한다 (코너에 노드를 놓아라)");
                    else
                    {
                        var offRoad=new List<Pt>();
                        if (fn.X==tn.X) { int x=fn.X; for (int y=System.Math.Min(fn.Y,tn.Y);y<=System.Math.Max(fn.Y,tn.Y);y++) { if (map.Cells[y*map.Width+x]!=CellType.Road) offRoad.Add(new Pt{x=x,y=y}); } }
                        else { int y=fn.Y; for (int x=System.Math.Min(fn.X,tn.X);x<=System.Math.Max(fn.X,tn.X);x++) { if (map.Cells[y*map.Width+x]!=CellType.Road) offRoad.Add(new Pt{x=x,y=y}); } }
                        if (offRoad.Count>0) A("V-P03","error",$"path.edges[{i}]",$"\"{e.From}→{e.To}\" — 도로 밖 칸 ({offRoad[0].x},{offRoad[0].y}) 을 지난다");
                    }
                }
                // V-P05: path existence
                int si=0,ei=0; bool hasS=false,hasE=false;
                for (int i=0;i<ns.Length;i++) { if (ns[i].Role=="start") { si=i; hasS=true; } if (ns[i].Role=="exit") { ei=i; hasE=true; } }
                if (hasS&&hasE&&idSet.Count==ns.Length)
                {
                    try
                    {
                        var g=BuildGraph(doc);
                        var spE=ShortestPath(g,si,ei,1);
                        if (spE==null) A("V-P05","error","path",$"start \"{ns[si].Id}\" 에서 exit \"{ns[ei].Id}\" 까지 보호대상이 갈 수 있는 경로가 없다");
                        var spN=ShortestPath(g,si,ei,2);
                        if (spN==null) A("V-P05","error","path",$"start \"{ns[si].Id}\" 에서 exit \"{ns[ei].Id}\" 까지 적(Enemy)이 갈 수 있는 경로가 없다. 모체가 얼어붙는다");
                    } catch { }
                }
            }
            // V-S01
            foreach (var row in sp.Table)
                if (!en.Contains(row.Enemy)) A("V-S01","error","spawn.table",$"\"{row.Enemy}\" 찾지 못함. 있는 것: {string.Join(", ", en)}");
            // V-S02
            if (sp.Table.Length==0) A("V-S02","error","spawn.table","스폰 표가 비었다");
            else { int tw=0; bool neg=false; for(int i=0;i<sp.Table.Length;i++){if(sp.Table[i].Weight<0){A("V-S02","error",$"spawn.table[{i}]",$"가중치 {sp.Table[i].Weight} 는 음수");neg=true;}tw+=sp.Table[i].Weight;} if(!neg&&tw==0) A("V-S02","error","spawn.table","가중치 합 0");}

            // V-S03
            SC("escortee.speed",doc.Escortee.Speed,v=>v>0);
            SC("escortee.maxHealth",doc.Escortee.MaxHealth,v=>v>0);
            SC("mother.speed",doc.Mother.Speed,v=>v>0);
            SC("mother.spawnDelay",doc.Mother.SpawnDelay,v=>v>=0);
            SC("spawn.volleyCount",sp.VolleyCount,v=>v>=1&&System.Math.Floor(v)==v);
            SC("spawn.volleySpacing",sp.VolleySpacing,v=>v>=0);
            SC("spawn.restSeconds",sp.RestSeconds,v=>v>0);
            SC("spawn.telegraphSeconds",sp.TelegraphSeconds,v=>v>=0);
            SC("burst.duration",bu.Duration,v=>v>=0);
            SC("burst.volleyCount",bu.VolleyCount,v=>v>=1&&System.Math.Floor(v)==v);
            SC("burst.restSeconds",bu.RestSeconds,v=>v>=0);
            SC("burst.recoverySpeedMultiplier",bu.RecoverySpeedMultiplier,v=>v>=1);
            SC("burst.recoverySeconds",bu.RecoverySeconds,v=>v>=0);
            SC("economy.startingResource",ec.StartingResource,v=>v>=0&&System.Math.Floor(v)==v);
            SC("economy.shortcutCost",ec.ShortcutCost,v=>v>=0&&System.Math.Floor(v)==v);
            SC("presentation.uiSlowMotionScale",doc.Presentation.UiSlowMotionScale,v=>v>0&&v<=1);
            SC("presentation.shotLineSeconds",doc.Presentation.ShotLineSeconds,v=>v>=0);
            SC("presentation.magicMissileSpeed",doc.Presentation.MagicMissileSpeed,v=>v>=0.1f);
            SC("presentation.hitFlashSeconds",doc.Presentation.HitFlashSeconds,v=>v>=0);
            SC("presentation.debrisCount",doc.Presentation.DebrisCount,v=>v>=0&&System.Math.Floor(v)==v);
            SC("presentation.debrisSeconds",doc.Presentation.DebrisSeconds,v=>v>=0);
            SC("presentation.masterVolume",doc.Presentation.MasterVolume,v=>v>=0&&v<=1);
            foreach (var d in ec.Difficulties)
            {
                SC($"economy.difficulties.{d.Difficulty}.killReward",d.KillReward,v=>v>=0);
                SC($"economy.difficulties.{d.Difficulty}.resourcePerSecond",d.ResourcePerSecond,v=>v>=0);
            }

            // V-S04
            bool he=false,hn=false,hh=false;
            foreach(var d in ec.Difficulties){if(d.Difficulty=="Easy")he=true;else if(d.Difficulty=="Normal")hn=true;else if(d.Difficulty=="Hard")hh=true;else A("V-S04","error","economy.difficulties",$"\"{d.Difficulty}\" 는 Easy/Normal/Hard 아님");}
            if(!he) A("V-S04","error","economy.difficulties","Easy 가 없다");
            if(!hn) A("V-S04","error","economy.difficulties","Normal 이 없다");
            if(!hh) A("V-S04","error","economy.difficulties","Hard 가 없다");

            // V-S05
            var hp=sp.HealthByProgress; if(hp.Length==0) A("V-S05","error","spawn.healthByProgress","비었다");
            else for(int k=0;k<hp.Length;k++){
                if(hp[k].T<0||hp[k].T>1) A("V-S05","error",$"spawn.healthByProgress[{k}].t",$"t={hp[k].T} 는 [0,1] 밖");
                if(hp[k].Mul<=0) A("V-S05","error",$"spawn.healthByProgress[{k}].mul",$"mul={hp[k].Mul} 는 0 이하");
                if(k>0&&hp[k].T<=hp[k-1].T) A("V-S05","error",$"spawn.healthByProgress[{k}]",$"t={hp[k].T} 는 이전 키({hp[k-1].T})보다 커야 함");
            }

            // V-B01
            if(doc.Mother.Speed>=doc.Escortee.Speed) A("V-B01","error","mother.speed",$"mother.speed {doc.Mother.Speed} ≥ escortee.speed {doc.Escortee.Speed} — 모체가 보호대상을 따라잡는다. 게임이 거부한다");
            // V-B02
            var seen2=new HashSet<string>(); foreach(var id in bu.TriggerNodeIds){if(seen2.Contains(id)) A("V-B02","warning","burst.triggerNodeIds",$"\"{id}\" 가 중복이다. 두 번째부터는 무시된다"); seen2.Add(id);}
            // V-B03
            if(bu.VolleyCount<=sp.VolleyCount) A("V-B03","warning","burst.volleyCount",$"burst.volleyCount {bu.VolleyCount} ≤ spawn.volleyCount {sp.VolleyCount} — 버스트가 평시보다 약하다");

            return _is;
        }
        static void SC(string path, float v, System.Func<float,bool> ok)
        { if(!ok(v)) A("V-S03","error",path,$"{path} = {v} — 조건 불일치"); }
        static bool IB(StageDocument.MapDef m, int x, int y)
        { return x>=0&&x<m.Width&&y>=0&&y<m.Height; }
        static bool AW(CellType c)
        { return c==CellType.Ground||c==CellType.Buildable||c==CellType.Village; }
        static string CN(CellType c)
        {
            switch(c) {
                case CellType.Empty: return "빈칸"; case CellType.Ground: return "땅";
                case CellType.Road: return "도로"; case CellType.Buildable: return "배치 가능";
                case CellType.Village: return "마을"; case CellType.Blocked: return "벽";
                case CellType.Water: return "물"; default: return "?";
            }
        }
static List<Pt> FloodFill(StageDocument.MapDef m, int sx, int sy, System.Func<CellType,bool> same)
        {
            if (!IB(m,sx,sy)||!same(m.Cells[sy*m.Width+sx])) return new List<Pt>();
            int w=m.Width,h=m.Height; var seen=new HashSet<int>(); var stack=new List<int>();
            int si=sy*w+sx; seen.Add(si); stack.Add(si);
            var outL=new List<Pt>();
            while (stack.Count>0)
            {
                int i=stack[stack.Count-1]; stack.RemoveAt(stack.Count-1);
                int x=i%w, y=(i-x)/w; outL.Add(new Pt{x=x,y=y});
                int[] dx={1,-1,0,0}, dy={0,0,1,-1};
                for (int d=0;d<4;d++)
                {
                    int nx=x+dx[d], ny=y+dy[d];
                    if (nx>=0&&nx<w&&ny>=0&&ny<h)
                    {
                        int ni=ny*w+nx; if (!seen.Contains(ni)&&same(m.Cells[ni])) { seen.Add(ni); stack.Add(ni); }
                    }
                }
            }
            return outL;
        }
        struct ReachRes { public int reachB,totalB; public List<PV> perV; }
        struct PV { public int vx,vy,bc; }
        static ReachRes ComputeReach(StageDocument.MapDef m)
        {
            int w=m.Width,h=m.Height,totalB=0;
            var villages=new List<Pt>();
            for (int y=0;y<h;y++) for (int x=0;x<w;x++)
            {
                if (m.Cells[y*w+x]==CellType.Buildable) totalB++;
                if (m.Cells[y*w+x]==CellType.Village) villages.Add(new Pt{x=x,y=y});
            }
            var regionOf=new int[w*h]; for (int i=0;i<w*h;i++) regionOf[i]=-1;
            var perV=new List<PV>();
            int regions=0; var regB=new List<int>();
            foreach (var v in villages)
            {
                int vi=v.y*w+v.x; if (regionOf[vi]!=-1) { perV.Add(new PV{vx=v.x,vy=v.y,bc=regionOf[vi]>=0?regB[regionOf[vi]]:0}); continue; }
                var cells=FloodFill(m,v.x,v.y,c=>c==CellType.Ground||c==CellType.Buildable||c==CellType.Village);
                int bc=0; foreach (var c in cells) { regionOf[c.y*w+c.x]=regions; if (m.Cells[c.y*w+c.x]==CellType.Buildable) bc++; }
                regB.Add(bc); perV.Add(new PV{vx=v.x,vy=v.y,bc=bc}); regions++;
            }
            int reachB=0; for (int y=0;y<h;y++) for (int x=0;x<w;x++) { int i=y*w+x; if (regionOf[i]!=-1&&m.Cells[i]==CellType.Buildable) reachB++; }
            return new ReachRes{reachB=reachB,totalB=totalB,perV=perV};
        }
        struct GEdge { public int to; public float cost; public int allowed; }
        static List<GEdge>[] BuildGraph(StageDocument doc)
        {
            var ns=doc.Path.Nodes; var es=doc.Path.Edges;
            var idMap=new Dictionary<string,int>();
            for (int i=0;i<ns.Length;i++) idMap[ns[i].Id]=i;
            var g=new List<GEdge>[ns.Length]; for (int i=0;i<ns.Length;i++) g[i]=new List<GEdge>();
            for (int i=0;i<es.Length;i++)
            {
                var e=es[i];
                if (!idMap.TryGetValue(e.From,out int f)||!idMap.TryGetValue(e.To,out int t))
                    throw new System.Exception($"노드 없음: {(!idMap.ContainsKey(e.From)?e.From:e.To)}");
                if (e.Shortcut) continue;
                float dx=ns[f].X-ns[t].X, dy=ns[f].Y-ns[t].Y;
                float cost=(float)System.Math.Sqrt(dx*dx+dy*dy);
                int bits=0;
                if (e.Allowed=="All") bits=7;
                else { var parts=e.Allowed.Split('+'); foreach (var a in parts) { if (a=="Escortee") bits|=1; else if (a=="Enemy") bits|=2; else if (a=="Ally") bits|=4; } }
                g[f].Add(new GEdge{to=t,cost=cost,allowed=bits});
                if (e.Bidirectional) g[t].Add(new GEdge{to=f,cost=cost,allowed=bits});
            }
            return g;
        }
        static int[] ShortestPath(List<GEdge>[] g, int from, int to, int agent)
        {
            int n=g.Length; var dist=new float[n]; var prev=new int[n]; var done=new bool[n];
            for (int i=0;i<n;i++) { dist[i]=float.MaxValue; prev[i]=-1; }
            dist[from]=0;
            for (int iter=0;iter<n;iter++)
            {
                int u=-1;
                for (int i=0;i<n;i++) { if (!done[i]&&dist[i]<float.MaxValue&&(u==-1||dist[i]<dist[u]-1e-9f)) u=i; }
                if (u==-1) break;
                done[u]=true;
                foreach (var e in g[u]) { if ((e.allowed&agent)==0) continue; float nd=dist[u]+e.cost; if (nd<dist[e.to]-1e-9f) { dist[e.to]=nd; prev[e.to]=u; } }
            }
            if (dist[to]>=float.MaxValue) return null;
            var path=new List<int>(); int c=to; while (c!=-1) { path.Add(c); c=prev[c]; } path.Reverse();
            return path.ToArray();
        }
    }
}
