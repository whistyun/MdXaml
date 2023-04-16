using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace MdXaml.Plugins
{
    public class SyntaxManager
    {
        public static readonly SyntaxManager Plain = new()
        {
            EnableNoteBlock = false,
            EnableTableBlock = false,
            EnableRuleExt = false,
            EnableTextAlignment = false,
            EnableStrikethrough = false,
            EnableListMarkerExt = false,
            EnableTextileInline = false,
        };
        public static readonly SyntaxManager Standard = new()
        {
            EnableNoteBlock = false,
            EnableTableBlock = true,
            EnableRuleExt = false,
            EnableTextAlignment = false,
            EnableStrikethrough = false,
            EnableListMarkerExt = false,
            EnableTextileInline = false,
        };
        public static readonly SyntaxManager MdXaml = new();


        public bool EnableNoteBlock { set; get; } = true;
        public bool EnableTableBlock { set; get; } = true;
        public bool EnableRuleExt { set; get; } = true;
        public bool EnableTextAlignment { set; get; } = true;
        public bool EnableStrikethrough { set; get; } = true;

        public bool EnableListMarkerExt { set; get; } = true;
        public bool EnableTextileInline { get; set; } = true;

        public void And(SyntaxManager manager)
        {
            EnableNoteBlock &= manager.EnableNoteBlock;
            EnableTableBlock &= manager.EnableTableBlock;
            EnableRuleExt &= manager.EnableRuleExt;
            EnableTextAlignment &= manager.EnableTextAlignment;
            EnableStrikethrough &= manager.EnableStrikethrough;
            EnableListMarkerExt &= manager.EnableListMarkerExt;
            EnableTextileInline &= manager.EnableTextileInline;
        }

        public SyntaxManager Clone()
            => new SyntaxManager()
            {
                EnableNoteBlock = EnableNoteBlock,
                EnableTableBlock = EnableTableBlock,
                EnableRuleExt = EnableRuleExt,
                EnableTextAlignment = EnableTextAlignment,
                EnableStrikethrough = EnableStrikethrough,
                EnableListMarkerExt = EnableListMarkerExt,
                EnableTextileInline = EnableTextileInline,
            };
    }
}
