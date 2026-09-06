/**
 * 목적: TOON 텍스트 -> StageDocument 변환. decode 소관 4규칙(V-F01/V-F03/V-M01/V-M02) 만 낸다.
 * 왜 이 구조인가: TS decode.ts 의 C# 포팅. ObjectNode 를 StageDocument 로 옮기면서 decode 검증.
 * 바꾸면 안 되는 것: decode 소관 4규칙 외 다른 규칙을 여기 넣지 마라. 누락 필드를 기본값으로 채우지 마라.
 * 근거: SDD-05 §3 [D-05-03], SDD-02 §5 [D-02-06]
 */
using System;
using System.Collections.Generic;
using PMF.EditorTools.Authoring.Toon;
namespace PMF.EditorTools.Authoring
{
    public struct ReadResult { public bool Ok; public StageDocument Doc; public List<Issue> Issues; }
    public static class StageDocumentReader
    {
        static readonly Dictionary<char, CellType> C2C = new()
        { {'_',CellType.Empty},{'.',CellType.Ground},{'R',CellType.Road},{'B',CellType.Buildable},{'V',CellType.Village},{'W',CellType.Blocked},{'~',CellType.Water} };

        public static ReadResult Read(string txt)
        {
            var isu = new List<Issue>();
            var pr = ToonParser.Parse(txt);
            if (!pr.Ok) { isu.Add(Mk("V-F03","error","",pr.Error.Message)); return Bad(isu); }
            var rt = pr.Value; var doc = new StageDocument{Schema="pmf.stage/1"};
            var se = rt.Entries.TryGetValue("schema",out var sn)?sn:null;
            var sv = (se as ToonReader.ScalarNode)?.Value as string;
            if (sv != "pmf.stage/1")
            { var actual = sv ?? "(없음)"; isu.Add(Mk("V-F01","error","schema",$"schema=\"{actual}\" 다. pmf.stage/1 만 읽는다")); return Bad(isu); }
            if (!TV(rt,"name",out string nv,isu,"name")) return Bad(isu); doc.Name = nv;
            if (!TO(rt,"map",out var mn,isu,"map")) return Bad(isu); if(!RM(mn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"path",out var pn,isu,"path")) return Bad(isu); if(!RP(pn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"spawn",out var sn2,isu,"spawn")) return Bad(isu); if(!RS(sn2,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"burst",out var bn,isu,"burst")) return Bad(isu); if(!RB(bn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"economy",out var en,isu,"economy")) return Bad(isu); if(!RE(en,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"escortee",out var esn,isu,"escortee")) return Bad(isu); if(!REs(esn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"mother",out var mtn,isu,"mother")) return Bad(isu); if(!RMo(mtn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"presentation",out var prn,isu,"presentation")) return Bad(isu); if(!RPt(prn,isu,ref doc)) return Bad(isu);
            if (!TO(rt,"toggles",out var tgn,isu,"toggles")) return Bad(isu); if(!RTg(tgn,isu,ref doc)) return Bad(isu);
            return new ReadResult{Ok=true,Doc=doc,Issues=isu};
        }
        static ReadResult Bad(List<Issue> i) => new(){Ok=false,Issues=i};
        static Issue Mk(string id,string s,string p,string m)=>new(){Id=id,Severity=s,Path=p,Message=m};

        static bool TV(ToonReader.ObjectNode n,string k,out string v,List<Issue> i,string p)
        { v=""; if(!n.Entries.TryGetValue(k,out var e)){i.Add(Mk("V-F03","error",p,$"\"{k}\" 가 없다"));return false;}
          if(e is not ToonReader.ScalarNode sc||sc.Value is not string){i.Add(Mk("V-F03","error",p,$"\"{k}\" 은 문자열이어야 하는데 {(e as ToonReader.ScalarNode)?.Value}"));return false;}
          v=(string)sc.Value;return true; }

        static bool TN(ToonReader.ObjectNode n,string k,out double v,List<Issue> i,string p)
        { v=0; if(!n.Entries.TryGetValue(k,out var e)){i.Add(Mk("V-F03","error",p,$"\"{k}\" 가 없다"));return false;}
          if(e is not ToonReader.ScalarNode sc||sc.Value is not double){i.Add(Mk("V-F03","error",p,$"\"{k}\" 은 숫자여야 하는데 {(e as ToonReader.ScalarNode)?.Value}"));return false;}
          v=(double)sc.Value;return true; }

        static bool TB(ToonReader.ObjectNode n,string k,out bool v,List<Issue> i,string p)
        { v=false; if(!n.Entries.TryGetValue(k,out var e)){i.Add(Mk("V-F03","error",p,$"\"{k}\" 가 없다"));return false;}
          if(e is not ToonReader.ScalarNode sc||sc.Value is not bool){i.Add(Mk("V-F03","error",p,$"\"{k}\" 은 불리언이어야 하는데 {(e as ToonReader.ScalarNode)?.Value}"));return false;}
          v=(bool)sc.Value;return true; }

        static bool TO(ToonReader.ObjectNode n,string k,out ToonReader.ObjectNode v,List<Issue> i,string p)
        { v=null; if(!n.Entries.TryGetValue(k,out var e)){i.Add(Mk("V-F03","error",p,$"\"{k}\" 가 없다"));return false;}
          if(e is not ToonReader.ObjectNode o){i.Add(Mk("V-F03","error",p,$"\"{k}\" 은 객체여야 하는데 {e.Kind}"));return false;}
          v=o;return true; }

        static bool TT(ToonReader.ObjectNode n,string k,out ToonReader.TableNode v,List<Issue> i,string p)
        { v=null; if(!n.Entries.TryGetValue(k,out var e)){i.Add(Mk("V-F03","error",p,$"\"{k}\" 가 없다"));return false;}
          if(e is not ToonReader.TableNode t){i.Add(Mk("V-F03","error",p,$"\"{k}\" 은 표여야 하는데 {e.Kind}"));return false;}
          v=t;return true; }
        static bool RM(ToonReader.ObjectNode mn, List<Issue> i, ref StageDocument doc)
        {
            if (!TN(mn,"width",out double w,i,"map"))return false; if (!TN(mn,"height",out double h,i,"map"))return false;
            int wi=(int)w, he=(int)h; var cells=new CellType[wi*he];
            if (!TT(mn,"rows",out var rt,i,"map"))return false;
            if (rt.Rows.Count!=he) { i.Add(Mk("V-M01","error","map.rows",$"{he}행인데 {rt.Rows.Count}행")); return false; }
            for (int r=0;r<he;r++)
            {
                var row=rt.Rows[r]; if (row.Count!=1||row[0] is not string s)
                { i.Add(Mk("V-F03","error",$"map.rows[{r}]","문자열 1열이어야 함")); return false; }
                if (s.Length!=wi) { i.Add(Mk("V-M01","error",$"map.rows[{r}]",$"길이 {wi}인데 {s.Length}")); return false; }
                int y=he-1-r;
                for (int x=0;x<wi;x++)
                {
                    if (!C2C.TryGetValue(s[x],out CellType cv))
                    { i.Add(Mk("V-M02","error",$"map.rows[{r}]",$"알 수 없는 문자 '{s[x]}' ({r+1}행 {x+1}열). 쓸 수 있는 문자: _ . R B V W ~")); return false; }
                    cells[y*wi+x]=cv;
                }
            }
            doc.Map=new StageDocument.MapDef{Width=wi,Height=he,Cells=cells};
            return true;
        }

        static bool RP(ToonReader.ObjectNode pn, List<Issue> i, ref StageDocument doc)
        {
            if (!TT(pn,"nodes",out var nt,i,"path"))return false; var nds=new StageDocument.PathNodeDef[nt.Rows.Count];
            for (int j=0;j<nt.Rows.Count;j++)
            {
                var r=nt.Rows[j]; if (r.Count<4||r[0] is not string||r[3] is not string)
                { i.Add(Mk("V-F03","error",$"path.nodes[{j}]","id,x,y,role")); return false; }
                double x=0,y=0;
                if (r[1] is double xd) x=xd; else if (r[1] is int xi) x=xi;
                if (r[2] is double yd) y=yd; else if (r[2] is int yi) y=yi;
                nds[j]=new StageDocument.PathNodeDef{Id=(string)r[0],Role=(string)r[3],X=(int)x,Y=(int)y};
            }
            if (!TT(pn,"edges",out var et,i,"path"))return false; var eds=new StageDocument.PathEdgeDef[et.Rows.Count];
            for (int j=0;j<et.Rows.Count;j++)
            {
                var r=et.Rows[j]; if (r.Count<5||r[0] is not string||r[1] is not string||r[2] is not string||r[3] is not bool||r[4] is not bool)
                { i.Add(Mk("V-F03","error",$"path.edges[{j}]","from,to,allowed,bidirectional,shortcut")); return false; }
                eds[j]=new StageDocument.PathEdgeDef{From=(string)r[0],To=(string)r[1],Allowed=(string)r[2],Bidirectional=(bool)r[3],Shortcut=(bool)r[4]};
            }
            doc.Path=new StageDocument.PathDef{Nodes=nds,Edges=eds};
            return true;
        }

        static bool RS(ToonReader.ObjectNode sn, List<Issue> i, ref StageDocument doc)
        {
            var sd=new StageDocument.SpawnDef();
            if (!TN(sn,"volleyCount",out double vc,i,"spawn"))return false; sd.VolleyCount=(int)vc;
            if (!TN(sn,"volleySpacing",out double vs,i,"spawn"))return false; sd.VolleySpacing=(float)vs;
            if (!TN(sn,"restSeconds",out double rs,i,"spawn"))return false; sd.RestSeconds=(float)rs;
            if (!TN(sn,"telegraphSeconds",out double ts,i,"spawn"))return false; sd.TelegraphSeconds=(float)ts;
            if (!TT(sn,"table",out var tbl,i,"spawn"))return false;
            var rows=new StageDocument.SpawnTableRow[tbl.Rows.Count];
            for (int j=0;j<tbl.Rows.Count;j++)
            {
                var r=tbl.Rows[j]; if (r.Count<2||r[0] is not string||r[1] is not double)
                { i.Add(Mk("V-F03","error",$"spawn.table[{j}]","enemy,weight")); return false; }
                rows[j]=new StageDocument.SpawnTableRow{Enemy=(string)r[0],Weight=(int)(double)r[1]};
            }
            sd.Table=rows;
            if (!TT(sn,"healthByProgress",out var hp,i,"spawn"))return false;
            var hpArr=new StageDocument.HealthPoint[hp.Rows.Count];
            for (int j=0;j<hp.Rows.Count;j++)
            {
                var r=hp.Rows[j]; if (r.Count<2||r[0] is not double||r[1] is not double)
                { i.Add(Mk("V-F03","error",$"spawn.healthByProgress[{j}]","t,mul")); return false; }
                hpArr[j]=new StageDocument.HealthPoint{T=(float)(double)r[0],Mul=(float)(double)r[1]};
            }
            sd.HealthByProgress=hpArr; doc.Spawn=sd; return true;
        }

        static bool RB(ToonReader.ObjectNode bn, List<Issue> i, ref StageDocument doc)
        {
            var bd=new StageDocument.BurstDef();
            if (!TN(bn,"duration",out double du,i,"burst"))return false; bd.Duration=(float)du;
            if (!TN(bn,"volleyCount",out double vc,i,"burst"))return false; bd.VolleyCount=(int)vc;
            if (!TN(bn,"restSeconds",out double rs,i,"burst"))return false; bd.RestSeconds=(float)rs;
            if (!TN(bn,"recoverySpeedMultiplier",out double rsm,i,"burst"))return false; bd.RecoverySpeedMultiplier=(float)rsm;
            if (!TN(bn,"recoverySeconds",out double rse,i,"burst"))return false; bd.RecoverySeconds=(float)rse;
            if (!bn.Entries.TryGetValue("triggerNodeIds",out var te))
            { i.Add(Mk("V-F03","error","burst","\"triggerNodeIds\" 가 없다")); return false; }
            if (te is ToonReader.ArrayNode arr)
            {
                var list=new List<string>(); foreach(var item in arr.Items){if(item is string s)list.Add(s);else{i.Add(Mk("V-F03","error","burst.triggerNodeIds","문자열 배열이어야 함"));return false;}}
                bd.TriggerNodeIds=list.ToArray();
            }
            else if(te is ToonReader.ScalarNode sc&&sc.Value is string ss)
            { bd.TriggerNodeIds=string.IsNullOrEmpty(ss)?new string[0]:new[]{ss}; }
            else { i.Add(Mk("V-F03","error","burst.triggerNodeIds","배열이어야 함")); return false; }
            doc.Burst=bd; return true;
        }
        static bool RE(ToonReader.ObjectNode en, List<Issue> i, ref StageDocument doc)
        {
            var ed=new StageDocument.EconomyDef();
            if (!TN(en,"startingResource",out double sr,i,"economy"))return false; ed.StartingResource=(int)sr;
            if (!TN(en,"shortcutCost",out double sc,i,"economy"))return false; ed.ShortcutCost=(int)sc;
            if (!TT(en,"difficulties",out var dt,i,"economy"))return false;
            var dr=new StageDocument.DifficultyRow[dt.Rows.Count];
            for (int j=0;j<dt.Rows.Count;j++)
            {
                var r=dt.Rows[j]; if (r.Count<4||r[0] is not string||r[1] is not string||r[2] is not double||r[3] is not double)
                { i.Add(Mk("V-F03","error",$"economy.difficulties[{j}]","difficulty,displayName,killReward,resourcePerSecond")); return false; }
                dr[j]=new StageDocument.DifficultyRow{Difficulty=(string)r[0],DisplayName=(string)r[1],KillReward=(float)(double)r[2],ResourcePerSecond=(float)(double)r[3]};
            }
            ed.Difficulties=dr; doc.Economy=ed; return true;
        }

        static bool REs(ToonReader.ObjectNode en, List<Issue> i, ref StageDocument doc)
        {
            if (!TN(en,"speed",out double sp,i,"escortee"))return false;
            if (!TN(en,"maxHealth",out double mh,i,"escortee"))return false;
            doc.Escortee=new StageDocument.EscorteeDef{Speed=(float)sp,MaxHealth=(float)mh}; return true;
        }

        static bool RMo(ToonReader.ObjectNode mn, List<Issue> i, ref StageDocument doc)
        {
            if (!TN(mn,"speed",out double sp,i,"mother"))return false;
            if (!TN(mn,"spawnDelay",out double sd,i,"mother"))return false;
            if (!TB(mn,"followsPath",out bool fp,i,"mother"))return false;
            doc.Mother=new StageDocument.MotherDef{Speed=(float)sp,SpawnDelay=(float)sd,FollowsPath=fp}; return true;
        }

        static bool RPt(ToonReader.ObjectNode pn, List<Issue> i, ref StageDocument doc)
        {
            var pd=new StageDocument.PresentationDef();
            if (!TN(pn,"uiSlowMotionScale",out double us,i,"presentation"))return false; pd.UiSlowMotionScale=(float)us;
            if (!TN(pn,"shotLineSeconds",out double sl,i,"presentation"))return false; pd.ShotLineSeconds=(float)sl;
            if (!TN(pn,"magicMissileSpeed",out double mm,i,"presentation"))return false; pd.MagicMissileSpeed=(float)mm;
            if (!TN(pn,"hitFlashSeconds",out double hf,i,"presentation"))return false; pd.HitFlashSeconds=(float)hf;
            if (!TN(pn,"debrisCount",out double dc,i,"presentation"))return false; pd.DebrisCount=(int)dc;
            if (!TN(pn,"debrisSeconds",out double ds,i,"presentation"))return false; pd.DebrisSeconds=(float)ds;
            if (!TB(pn,"healthBarHideWhenFull",out bool hb,i,"presentation"))return false; pd.HealthBarHideWhenFull=hb;
            if (!TN(pn,"masterVolume",out double mv,i,"presentation"))return false; pd.MasterVolume=(float)mv;
            doc.Presentation=pd; return true;
        }

        static bool RTg(ToonReader.ObjectNode tn, List<Issue> i, ref StageDocument doc)
        {
            if (!TB(tn,"alliesCanDieWhileMarching",out bool ac,i,"toggles"))return false;
            if (!TB(tn,"enemiesTargetAllies",out bool ea,i,"toggles"))return false;
            doc.Toggles=new StageDocument.ToggleDef{AlliesCanDieWhileMarching=ac,EnemiesTargetAllies=ea}; return true;
        }
    }
}