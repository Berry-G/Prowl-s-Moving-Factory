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
        static List<Issue> _is;
        static void A(string id, string sev, string p, string m) => _is.Add(new Issue{Id=id,Severity=sev,Path=p,Message=m});
        public static List<Issue> Validate(StageDocument doc, HashSet<string> en)
        {
            _is = new List<Issue>(); var map = doc.Map; var p = doc.Path; var es = p.Edges; var ns = p.Nodes;
            var sp = doc.Spawn; var ec = doc.Economy; var bu = doc.Burst;

            bool hv = false, hb = false;
            foreach (var c in map.Cells) { if (c == CellType.Village) hv = true; if (c == CellType.Buildable) hb = true; }
            if (!hv) A("V-M03","error","map","마을(V)이 없다. 아군을 고용할 곳이 없다");
            if (!hb) A("V-M04","error","map","배치 가능 칸(B)이 없다");
            if (map.Width < 8 || map.Width > 256 || map.Height < 8 || map.Height > 256)
                A("V-M08","error","map",$"맵 크기 {map.Width}x{map.Height} - 8~256 사이여야 한다");

            int sc = 0, xc = 0;
            foreach (var n in ns) { if (n.Role == "start") sc++; if (n.Role == "exit") xc++; }
            if (sc != 1) A("V-P02","error","path",$"start 노드가 {sc}개다. 정확히 1개여야 한다");
            if (xc == 0) A("V-P02","error","path","exit 노드가 없다");

            var idSet = new Dictionary<string, int>();
            for (int i = 0; i < ns.Length; i++)
            {
                if (!Regex.IsMatch(ns[i].Id,@"^[A-Za-z_][A-Za-z0-9_]*$"))
                    A("V-P09","error",$"path.nodes[{i}]",$"id \"{ns[i].Id}\" 형식 위반");
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
                    if (ak==bk) A("V-P08","error",$"path.edges[{j}]",$"\"{b.From}<->{b.To}\" 가 path.edges[{i}] 와 중복");
                }
                foreach (var tid in bu.TriggerNodeIds)
                { var fn = System.Array.Find(ns,n=>n.Id==tid); if (fn.Role=="start"||fn.Role=="exit") A("V-P06","warning","burst.triggerNodeIds", $"\"{fn.Id}\" 는 {fn.Role} 노드다"); }
            }

            foreach (var row in sp.Table)
                if (!en.Contains(row.Enemy)) A("V-S01","error","spawn.table",$"\"{row.Enemy}\" 찾지 못함. 있는 것: {string.Join(", ", en)}");
            if (sp.Table.Length==0) A("V-S02","error","spawn.table","스폰 표가 비었다");
            else { int tw=0; bool neg=false; for(int i=0;i<sp.Table.Length;i++){if(sp.Table[i].Weight<0){A("V-S02","error",$"spawn.table[{i}]",$"가중치 {sp.Table[i].Weight} 는 음수");neg=true;}tw+=sp.Table[i].Weight;} if(!neg&&tw==0) A("V-S02","error","spawn.table","가중치 합 0");}

            bool he=false,hn=false,hh=false;
            foreach(var d in ec.Difficulties){if(d.Difficulty=="Easy")he=true;else if(d.Difficulty=="Normal")hn=true;else if(d.Difficulty=="Hard")hh=true;else A("V-S04","error","economy.difficulties",$"\"{d.Difficulty}\" 는 Easy/Normal/Hard 아님");}
            if(!he) A("V-S04","error","economy.difficulties","Easy 가 없다");
            if(!hn) A("V-S04","error","economy.difficulties","Normal 이 없다");
            if(!hh) A("V-S04","error","economy.difficulties","Hard 가 없다");

            var hp=sp.HealthByProgress; if(hp.Length==0) A("V-S05","error","spawn.healthByProgress","비었다");
            else for(int k=0;k<hp.Length;k++){
                if(hp[k].T<0||hp[k].T>1) A("V-S05","error",$"spawn.healthByProgress[{k}].t",$"t={hp[k].T} 는 [0,1] 밖");
                if(hp[k].Mul<=0) A("V-S05","error",$"spawn.healthByProgress[{k}].mul",$"mul={hp[k].Mul} 는 0 이하");
                if(k>0&&hp[k].T<=hp[k-1].T) A("V-S05","error",$"spawn.healthByProgress[{k}]",$"t={hp[k].T} 는 이전 키({hp[k-1].T})보다 커야 함");
            }

            if(doc.Mother.Speed>=doc.Escortee.Speed) A("V-B01","error","mother.speed",$"mother.speed {doc.Mother.Speed} >= escortee.speed {doc.Escortee.Speed} - 모체가 보호대상을 따라잡는다. 게임이 거부한다");
            var seen=new HashSet<string>(); foreach(var id in bu.TriggerNodeIds){if(seen.Contains(id)) A("V-B02","warning","burst.triggerNodeIds",$"\"{id}\" 가 중복이다. 두 번째부터는 무시된다"); seen.Add(id);}
            if(bu.VolleyCount<=sp.VolleyCount) A("V-B03","warning","burst.volleyCount",$"burst.volleyCount {bu.VolleyCount} <= spawn.volleyCount {sp.VolleyCount} - 버스트가 평시보다 약하다");

            return _is;
        }
    }
}