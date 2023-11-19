using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows;
using MdXaml.Html.Core.Utils;
using System.Security.Cryptography.X509Certificates;
using System.Collections;

namespace MdXaml.Html.Core.Parsers.MarkdigExtensions
{
    public class GridTableParser : IBlockTagParser, IHasPriority
    {
        public int Priority => HasPriority.DefaultPriority + 1000;

        public IEnumerable<string> SupportTag => new[] { "table" };

        bool ITagParser.TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<TextElement> generated)
        {
            var rtn = TryReplace(node, manager, out var list);
            generated = list;
            return rtn;
        }

        public bool TryReplace(HtmlNode node, ReplaceManager manager, out IEnumerable<Block> generated)
        {
            var table = new Table();
            var measures = new ColWidMeasure();

            ParseColumnStyle(node, ref measures);

            var theadRows = node.SelectNodes("./thead/tr");
            if (theadRows is not null)
            {
                var group = CreateRowGroup(theadRows, manager, ref measures);
                group.Tag = manager.GetTag(Tags.TagTableHeader);
                table.RowGroups.Add(group);
            }

            var tbodyRows = new List<HtmlNode>();
            foreach (var child in node.ChildNodes)
            {
                if (string.Equals(child.Name, "tr", StringComparison.OrdinalIgnoreCase))
                    tbodyRows.Add(child);

                if (string.Equals(child.Name, "tbody", StringComparison.OrdinalIgnoreCase))
                    tbodyRows.AddRange(child.ChildNodes.CollectTag("tr"));
            }
            if (tbodyRows.Count > 0)
            {
                var group = CreateRowGroup(tbodyRows, manager, ref measures);
                group.Tag = manager.GetTag(Tags.TagTableBody);
                table.RowGroups.Add(group);

                int idx = 0;
                foreach (var row in group.Rows)
                {
                    var useTag = (++idx & 1) == 0 ? Tags.TagEvenTableRow : Tags.TagOddTableRow;
                    row.Tag = manager.GetTag(useTag);
                }
            }

            var tfootRows = node.SelectNodes("./tfoot/tr");
            if (tfootRows is not null)
            {
                var group = CreateRowGroup(tfootRows, manager, ref measures);
                group.Tag = manager.GetTag(Tags.TagTableFooter);
                table.RowGroups.Add(group);
            }


            foreach (var measure in measures)
            {
                if (measure == Length.Auto || measure is null)
                {
                    table.Columns.Add(new TableColumn());
                }
                else if (measure.Unit == Unit.Percentage)
                {
                    table.Columns.Add(new TableColumn()
                    {
                        Width = new GridLength(measure.Value, GridUnitType.Star)
                    });
                }
                else
                {
                    table.Columns.Add(new TableColumn()
                    {
                        Width = new GridLength(measure.ToPoint(), GridUnitType.Pixel)
                    });
                }
            }


            var captions = node.SelectNodes("./caption");
            if (captions is not null)
            {
                var tableSec = new Section();
                foreach (var cap in captions)
                {
                    tableSec.Blocks.AddRange(manager.ParseChildrenAndGroup(cap));
                }

                tableSec.Blocks.Add(table);
                tableSec.Tag = manager.GetTag(Tags.TagTableCaption);

                generated = new[] { tableSec };
            }
            else
            {
                generated = new[] { table };
            }

            return true;
        }

        private static void ParseColumnStyle(
            HtmlNode tableTag,
            ref ColWidMeasure measures)
        {
            var colHolder = tableTag.ChildNodes.HasOneTag("colgroup", out var colgroup) ? colgroup! : tableTag;

            int colIdx = 0;
            foreach (var col in colHolder.ChildNodes.CollectTag("col"))
            {
                int colspan = 1;

                var spanAttr = col.Attributes["span"];
                if (spanAttr is not null)
                {
                    if (int.TryParse(spanAttr.Value, out var spanCnt))
                    {
                        colspan = spanCnt;
                    }
                }

                var length = Length.Auto;
                if (col.Attributes["style"] is HtmlAttribute styleAttr)
                {
                    var mch = Regex.Match(styleAttr.Value, "width:([^;\"]+(%|em|ex|mm|cm|in|pt|pc|))");
                    if (mch.Success)
                    {
                        if (Length.TryParse(mch.Groups[1].Value, out var ind))
                        {
                            length = ind;
                        }
                    }
                }
                measures.Sets(length, colIdx, colspan);
                colIdx += colspan;
            }
        }


        private static TableRowGroup CreateRowGroup(
            IEnumerable<HtmlNode> rows,
            ReplaceManager manager,
            ref ColWidMeasure measures)
        {
            var group = new TableRowGroup();
            var counters = new List<ColspanCounter>();

            foreach (var rowTag in rows)
            {
                var row = new TableRow();

                int colIdx = 0;

                foreach (var cellTag in rowTag.ChildNodes.CollectTag("td", "th"))
                {
                    if (counters.FirstOrDefault(e => e.ColIdx == colIdx) is ColspanCounter counter)
                    {
                        colIdx += counter.ColSpan;
                    }

                    int colspan = TryParse(cellTag.Attributes["colspan"]?.Value);
                    int rowspan = TryParse(cellTag.Attributes["rowspan"]?.Value);

                    if (cellTag.Attributes["width"] is HtmlAttribute widthAttr
                     && Length.TryParse(widthAttr.Value, out var length))
                    {
                        var setLen = colspan == 1 ? length : new Length(length.Value / colspan, length.Unit);
                        measures.Sets(setLen, colIdx, colspan);
                    }
                    else
                    {
                        measures.Sets(Length.Auto, colIdx, colspan);
                    }

                    var cell = new TableCell();
                    cell.Blocks.AddRange(manager.ParseChildrenAndGroup(cellTag));
                    cell.RowSpan = rowspan;
                    cell.ColumnSpan = colspan;
                    row.Cells.Add(cell);

                    if (rowspan > 1)
                    {
                        counters.Add(new ColspanCounter(colIdx, rowspan, colspan));
                    }

                    colIdx += colspan;
                }

                group.Rows.Add(row);

                for (int idx = counters.Count - 1; idx >= 0; --idx)
                    if (counters[idx].Detent())
                        counters.RemoveAt(idx);
            }

            return group;

            static int TryParse(string? txt) => int.TryParse(txt, out var v) ? v : 1;
        }

        class ColspanCounter
        {
            public int ColIdx { get; set; }
            public int Remain { get; set; }
            public int ColSpan { get; }

            public ColspanCounter(int colIdx, int rowspan, int colspan)
            {
                ColIdx = colIdx;
                Remain = rowspan;
                ColSpan = colspan;
            }

            public bool Detent()
            {
                return --Remain == 0;
            }
        }

        class ColWidMeasure : IEnumerable<Length>
        {
            private Length[] _array = new Length[0];

            public void Sets(Length val, int idx, int len)
            {
                if (idx + len > _array.Length)
                {
                    var nary = new Length[idx + len];
                    for (var i = _array.Length; i < nary.Length; ++i)
                        nary[i] = Length.Auto;

                    Array.Copy(_array, nary, _array.Length);
                    _array = nary;
                }

                for (var i = idx; i < idx + len; ++i)
                {
                    if (_array[i] < val)
                    {
                        _array[i] = val;
                    }
                }
            }

            public IEnumerator<Length> GetEnumerator() => _array.AsEnumerable<Length>().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => _array.Length;

            public Length this[int idx]
            {
                get => _array[idx];
                set => Sets(value, idx, 1);
            }
        }
    }
}
