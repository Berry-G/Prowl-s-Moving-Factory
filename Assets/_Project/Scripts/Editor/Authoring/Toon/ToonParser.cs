/**
 * 목적: 전처리된 TOON 줄을 재귀적으로 파싱해 ObjectNode 트리로 만든다.
 * 왜 이 구조인가: ToonReader.Pre/Sp/Pv 로 줄 분류와 값 해석을 외부에 위임했다.
 *   Parser 는 구조(들여쓰기 깊이로 객체·표·배열·스칼라 분기)만 담당한다.
 * 바꾸면 안 되는 것: 표 행 수 N 강제, 키 중복 거부, 들여쓰기 깊이 오류 거부, 조용히 무시 금지.
 * 근거: SDD-05 §3 [D-05-03], SDD-02 §6 [D-02-07]
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PMF.EditorTools.Authoring.Toon
{
    public static class ToonParser
    {
        static readonly string RT = @"^([\w.]+)\[(\d+)\]\{(.+)\}:$";
        static readonly string RA = @"^([\w.]+)\[(\d+)\]:\s*(.*)$";
        static readonly string RO = @"^([\w.]+):$";
        static readonly string RS = @"^([\w.]+):\s*(.*)$";

        static ToonReader.ParseResult<ToonReader.ObjectNode> E_(int l, string m)
            => new() { Ok = false, Error = new ToonReader.Error { Line = l, Message = m } };
        static ToonReader.ParseResult<ToonReader.ObjectNode> R_(ToonReader.ObjectNode v, int n = -1)
            => new() { Ok = true, Value = v, NextIdx = n };

        static ToonReader.ParseResult<ToonReader.ObjectNode> Po(int depth, int start, List<ToonReader.Ln> lines)
        {
            var node = new ToonReader.ObjectNode { Line = start < lines.Count ? lines[start].Line : 0 };
            int k = start;
            while (k < lines.Count && lines[k].Depth == depth)
            {
                var L = lines[k]; if (L.Depth > depth) return E_(L.Line, "들여쓰기 너무 깊음");
                var tm = Regex.Match(L.Body, RT);
                if (tm.Success)
                {
                    string key = tm.Groups[1].Value; int n = int.Parse(tm.Groups[2].Value);
                    var flds = new List<string>(tm.Groups[3].Value.Split(',', StringSplitOptions.RemoveEmptyEntries));
                    for (int fi = 0; fi < flds.Count; fi++) flds[fi] = flds[fi].Trim();
                    if (flds.Count == 0) return E_(L.Line, $"표 {key} 필드명 없음");
                    var rows = new List<List<object>>();
                    for (int ri = 0; ri < n; ri++)
                    {
                        int idx = k + 1 + ri;
                        if (idx >= lines.Count || lines[idx].Depth != depth + 1)
                            return E_(L.Line, $"표 {key} {n}행인데 {ri}행");
                        var cells = ToonReader.Sp(lines[idx].Body);
                        if (cells.Length != flds.Count) return E_(lines[idx].Line, $"{flds.Count}열인데 {cells.Length}열");
                        var pv = new List<object>();
                        foreach (var cell in cells) pv.Add(ToonReader.Pv(cell, lines[idx].Line));
                        rows.Add(pv);
                    }
                    int nxt = k + 1 + n;
                    if (nxt < lines.Count && lines[nxt].Depth == depth + 1) return E_(lines[nxt].Line, $"표 {key} {n}행인데 더 있음");
                    if (node.Entries.ContainsKey(key)) return E_(L.Line, $"키 {key} 중복");
                    node.Entries[key] = new ToonReader.TableNode { Fields = flds, Rows = rows, Line = L.Line };
                    k = nxt; continue;
                }
                var am = Regex.Match(L.Body, RA);
                if (am.Success)
                {
                    string key = am.Groups[1].Value; int n = int.Parse(am.Groups[2].Value); string rest = am.Groups[3].Value;
                    var items = n == 0 ? new List<object>() : new List<object>(Array.ConvertAll(ToonReader.Sp(rest), v => ToonReader.Pv(v, L.Line)));
                    if (items.Count != n) return E_(L.Line, $"배열 {key} {n}개인데 {items.Count}개");
                    if (node.Entries.ContainsKey(key)) return E_(L.Line, $"키 {key} 중복");
                    node.Entries[key] = new ToonReader.ArrayNode { Items = items, Line = L.Line }; k++; continue;
                }
                var om = Regex.Match(L.Body, RO);
                if (om.Success)
                {
                    string key = om.Groups[1].Value;
                    if (node.Entries.ContainsKey(key)) return E_(L.Line, $"키 {key} 중복");
                    var r2 = Po(depth + 1, k + 1, lines);
                    if (!r2.Ok) return r2;
                    if (r2.Value.Entries.Count == 0) return E_(L.Line, "빈 객체");
                    node.Entries[key] = r2.Value; k = r2.NextIdx; continue;
                }
                var sm = Regex.Match(L.Body, RS);
                if (sm.Success)
                {
                    string key = sm.Groups[1].Value; string rv = sm.Groups[2].Value;
                    if (node.Entries.ContainsKey(key)) return E_(L.Line, $"키 {key} 중복");
                    node.Entries[key] = new ToonReader.ScalarNode { Value = ToonReader.Pv(rv, L.Line), Line = L.Line };
                    k++; continue;
                }
                return E_(L.Line, $"해석 불가: {L.Body}");
            }
            return R_(node, k);
        }

        public static ToonReader.ParseResult<ToonReader.ObjectNode> Parse(string text)
        {
            var pre = ToonReader.Pre(text);
            if (!pre.Ok) return E_(pre.Error.Line, pre.Error.Message);
            if (pre.Value.Count == 0) return E_(0, "파일이 비었다");
            var r = Po(0, 0, pre.Value);
            if (!r.Ok) return r;
            if (r.NextIdx < pre.Value.Count) return E_(pre.Value[r.NextIdx].Line, "루트 깊이 아님");
            return r;
        }
    }
}