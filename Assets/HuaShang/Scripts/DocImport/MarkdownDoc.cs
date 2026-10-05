using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HuaShang.DocImport
{
    /// <summary>读取失败时抛出，消息写明文件、小节和找不到的内容。</summary>
    public class DocParseException : Exception
    {
        public DocParseException(string message) : base(message) { }
    }

    /// <summary>手册 Markdown 的最小读取器：按「## n.」分节，读出管道表格与正文。</summary>
    public class MarkdownDoc
    {
        public readonly string fileName;
        readonly List<string> lines;

        public MarkdownDoc(string path)
        {
            if (!File.Exists(path)) throw new DocParseException("找不到手册文件 " + path);
            fileName = Path.GetFileName(path);
            lines = new List<string>(File.ReadAllLines(path));
        }

        /// <summary>某一节的全部行。number 为「## 4.」里的 4。</summary>
        public Section SectionOf(int number)
        {
            var head = new Regex("^##\\s+" + number + "\\.");
            int start = lines.FindIndex(l => head.IsMatch(l));
            if (start < 0) throw new DocParseException(fileName + " 中没有第 " + number + " 节");
            int end = lines.FindIndex(start + 1, l => l.StartsWith("## ", StringComparison.Ordinal));
            if (end < 0) end = lines.Count;
            return new Section(fileName + " §" + number, lines.GetRange(start, end - start));
        }

        public Section Whole() => new Section(fileName, lines);
    }

    public class Section
    {
        public readonly string where;
        public readonly List<string> lines;
        public readonly string text;

        public Section(string where, List<string> lines)
        {
            this.where = where;
            this.lines = lines;
            text = string.Join("\n", lines);
        }

        /// <summary>表头第一格为 firstHeader 的表格的数据行（不含表头与分隔行）。</summary>
        public List<string[]> Table(string firstHeader)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].TrimStart().StartsWith("|", StringComparison.Ordinal)) continue;
                var header = Cells(lines[i]);
                if (header.Length == 0 || header[0] != firstHeader) continue;
                var rows = new List<string[]>();
                for (int j = i + 1; j < lines.Count && lines[j].TrimStart().StartsWith("|", StringComparison.Ordinal); j++)
                {
                    var cells = Cells(lines[j]);
                    if (IsSeparator(cells)) continue;
                    rows.Add(cells);
                }
                return rows;
            }
            throw new DocParseException(where + " 中没有表头为「" + firstHeader + "」的表");
        }

        /// <summary>首格等于 label 的那一行。</summary>
        public string[] Row(string firstHeader, string label)
        {
            foreach (var r in Table(firstHeader))
                if (r[0] == label) return r;
            throw new DocParseException(where + " 的「" + firstHeader + "」表中没有「" + label + "」行");
        }

        /// <summary>首格以 prefix 开头的那一行。</summary>
        public string[] RowStarting(string firstHeader, string prefix)
        {
            foreach (var r in Table(firstHeader))
                if (r[0].StartsWith(prefix, StringComparison.Ordinal)) return r;
            throw new DocParseException(where + " 的「" + firstHeader + "」表中没有以「" + prefix + "」开头的行");
        }

        /// <summary>在本节正文里匹配正则，返回第 group 组。</summary>
        public string Match(string pattern, int group = 1)
        {
            var m = Regex.Match(text, pattern);
            if (!m.Success) throw new DocParseException(where + " 中找不到：" + pattern);
            return m.Groups[group].Value;
        }

        public Match MatchAll(string pattern)
        {
            var m = Regex.Match(text, pattern);
            if (!m.Success) throw new DocParseException(where + " 中找不到：" + pattern);
            return m;
        }

        public double Number(string pattern, int group = 1) => Num.Parse(Match(pattern, group), where);

        static string[] Cells(string line)
        {
            var t = line.Trim();
            if (t.StartsWith("|", StringComparison.Ordinal)) t = t.Substring(1);
            if (t.EndsWith("|", StringComparison.Ordinal)) t = t.Substring(0, t.Length - 1);
            var parts = t.Split('|');
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            return parts;
        }

        static bool IsSeparator(string[] cells)
        {
            foreach (var c in cells)
                if (c.Length > 0 && !Regex.IsMatch(c, "^:?-+:?$")) return false;
            return true;
        }
    }

    /// <summary>手册里的数字：负号可能是「−」，倍数前有「×」，区间用「–」。</summary>
    public static class Num
    {
        public const string Signed = "([+\\-−]?\\d+(?:\\.\\d+)?)";
        public const string Unsigned = "(\\d+(?:\\.\\d+)?)";

        public static double Parse(string s, string where)
        {
            var t = Normalize(s);
            if (!double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                throw new DocParseException(where + " 中「" + s + "」不是数字");
            return v;
        }

        public static string Normalize(string s) => s.Trim().Replace('−', '-').Replace('＋', '+');

        /// <summary>读「a–b」区间。</summary>
        public static void Range(string cell, string where, out double min, out double max)
        {
            var m = Regex.Match(cell, "^" + Unsigned + "\\s*[–-]\\s*" + Unsigned + "$");
            if (!m.Success) throw new DocParseException(where + " 中「" + cell + "」不是区间");
            min = Parse(m.Groups[1].Value, where);
            max = Parse(m.Groups[2].Value, where);
        }

        /// <summary>单元格里第一个带符号的数。</summary>
        public static double First(string cell, string where)
        {
            var m = Regex.Match(cell, Signed);
            if (!m.Success) throw new DocParseException(where + " 中「" + cell + "」没有数字");
            return Parse(m.Groups[1].Value, where);
        }

        public static int FirstInt(string cell, string where) => (int)Math.Round(First(cell, where));
    }
}
