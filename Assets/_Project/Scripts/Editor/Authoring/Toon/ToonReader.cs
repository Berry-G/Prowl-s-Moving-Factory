/**
 * 목적: TOON 텍스트 전처리와 기본 값 해석 (주석 제거, 들여쓰기-깊이, 인용 이스케이프, 분할).
 * 왜 이 구조인가: ToonParser 와 분리했다 — 전처리·값 해석을 단위 테스트로 따로 검증할 수 있다.
 * 바꾸면 안 되는 것: 주석 제거 pre-pass, 들여쓰기 2칸 단위, null 금지, 조용히 무시 금지.
 * 근거: SDD-05 §3 [D-05-03], SDD-02 §6 [D-02-07]
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PMF.EditorTools.Authoring.Toon
{
    public static class ToonReader
    {
        public class Error { public int Line; public string Message; }
        public abstract class Node { public int Line; }
        public class ScalarNode : Node { public object Value; }
        public class ArrayNode : Node { public List<object> Items = new(); }
        public class TableNode : Node { public List<string> Fields = new(); public List<List<object>> Rows = new(); }
        public class ObjectNode : Node { public Dictionary<string, Node> Entries = new(); }
        public class ParseResult<T> { public bool Ok; public T Value; public Error Error; public int NextIdx; }
        static ParseResult<T> R<T>(T v, int n = -1) => new() { Ok = true, Value = v, NextIdx = n };
        static ParseResult<T> E<T>(int l, string m) => new() { Ok = false, Error = new Error { Line = l, Message = m } };
        internal struct Ln { public int Line, Depth; public string Body; }
        class Px : Exception { public int Line; public Px(int l, string m) : base(m) { Line = l; } }

        internal static ParseResult<List<Ln>> Pre(string text)
        {
            var o = new List<Ln>(); var r = text.Split('\n');
            for (int i = 0; i < r.Length; i++)
            {
                var t = r[i].TrimEnd(); if (t.EndsWith("\r")) t = t[..^1];
                int ln = i + 1; if (t.TrimEnd() == "") continue; if (Regex.IsMatch(t, @"^\s*#")) continue;
                if (t.Contains('\t')) return E<List<Ln>>(ln, "탭 금지");
                int ind = t.Length - t.TrimStart().Length; if (ind % 2 != 0) return E<List<Ln>>(ln, $"들여쓰기 홀수({ind})");
                o.Add(new Ln { Line = ln, Depth = ind / 2, Body = t.Trim() });
            }
            return R(o);
        }

        internal static string[] Sp(string s)
        {
            var l = new List<string>(); var c = new StringBuilder(); bool q = false;
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (q) { if (ch == '\\') { c.Append(ch); if (++i < s.Length) c.Append(s[i]); } else { if (ch == '"') q = false; c.Append(ch); } }
                else { if (ch == '"') { q = true; c.Append(ch); } else if (ch == ',') { l.Add(c.ToString().Trim()); c.Clear(); } else c.Append(ch); }
            }
            l.Add(c.ToString().Trim()); return l.ToArray();
        }

        internal static object Pv(string t, int ln)
        {
            if (t == "true") return true; if (t == "false") return false; if (t == "null") throw new Px(ln, "null 금지");
            if (t.StartsWith("\""))
            {
                if (!t.EndsWith("\"") || t.Length < 2) throw new Px(ln, "닫히지 않은 인용");
                return Ue(t[1..^1], ln);
            }
            if (Regex.IsMatch(t, @"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$")
                && double.TryParse(t, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double n))
                return n;
            if (t == "") throw new Px(ln, "빈 값은 \"\" 으로");
            return t;
        }

        static string Ue(string s, int ln)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != '\\' || i + 1 >= s.Length) { sb.Append(s[i]); continue; }
                var c = s[++i];
                switch (c)
                {
                    case '\\': sb.Append('\\'); break; case '"': sb.Append('"'); break;
                    case 'n': sb.Append('\n'); break; case 'r': sb.Append('\r'); break; case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length || !Regex.IsMatch(s.Substring(i + 1, 4), @"^[0-9a-fA-F]+$"))
                            throw new Px(ln, "\\uXXXX 오류");
                        sb.Append((char)int.Parse(s.Substring(i + 1, 4), System.Globalization.NumberStyles.HexNumber));
                        i += 4; break;
                    default: throw new Px(ln, $"알 수 없는 이스케이프 \\{c}");
                }
            }
            return sb.ToString();
        }
    }
}