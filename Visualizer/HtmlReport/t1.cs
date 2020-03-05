using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace
namespace System.Web.UI
// ReSharper restore CheckNamespace
{
    /// <summary>
    ///  Extensions for HtmlTextWriter
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static partial class HtmlWriterTextTagExtensions
    {
        [ItemNotNull] [NotNull] private static readonly Stack<Tag> _tags = new Stack<Tag>();

        /// <summary>
        ///  Opens a Unknown Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Unknown([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("Unknown", atts);
        }

        /// <summary>
        ///  Opens a A Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter A([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("a", atts);
        }

        /// <summary>
        ///  Opens a Acronym Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Acronym([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("acronym", atts);
        }

        /// <summary>
        ///  Opens a Address Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Address([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("address", atts);
        }

        /// <summary>
        ///  Opens a Area Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Area([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("area", atts);
        }

        /// <summary>
        ///  Opens a B Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter B([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("b", atts);
        }

        /// <summary>
        ///  Opens a Base Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Base([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("base", atts);
        }

        /// <summary>
        ///  Opens a Basefont Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Basefont([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("basefont", atts);
        }

        /// <summary>
        ///  Opens a Bdo Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Bdo([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("bdo", atts);
        }

        /// <summary>
        ///  Opens a Bgsound Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Bgsound([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("bgsound", atts);
        }

        /// <summary>
        ///  Opens a Big Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Big([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("big", atts);
        }

        /// <summary>
        ///  Opens a Blockquote Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Blockquote([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("blockquote", atts);
        }

        /// <summary>
        ///  Opens a Body Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Body([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("body", atts);
        }

        /// <summary>
        ///  Opens a Br Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Br([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("br", atts);
        }

        /// <summary>
        ///  Opens a Button Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Button([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("button", atts);
        }

        /// <summary>
        ///  Opens a Caption Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Caption([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("caption", atts);
        }

        /// <summary>
        ///  Opens a Center Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Center([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("center", atts);
        }

        /// <summary>
        ///  Opens a Cite Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Cite([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("cite", atts);
        }

        /// <summary>
        ///  Opens a Code Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Code([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("code", atts);
        }

        /// <summary>
        ///  Opens a Col Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Col([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("col", atts);
        }

        /// <summary>
        ///  Opens a Colgroup Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Colgroup([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("colgroup", atts);
        }

        /// <summary>
        ///  Opens a Dd Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Dd([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("dd", atts);
        }

        /// <summary>
        ///  Opens a Del Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Del([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("del", atts);
        }

        /// <summary>
        ///  Opens a Dfn Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Dfn([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("dfn", atts);
        }

        /// <summary>
        ///  Opens a Dir Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Dir([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("dir", atts);
        }

        /// <summary>
        ///  Opens a Div Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Div([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("div", atts);
        }

        /// <summary>
        ///  Opens a Dl Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Dl([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("dl", atts);
        }

        /// <summary>
        ///  Opens a Dt Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Dt([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("dt", atts);
        }

        /// <summary>
        ///  Opens a Em Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Em([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("em", atts);
        }

        /// <summary>
        ///  Opens a Embed Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Embed([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("embed", atts);
        }

        /// <summary>
        ///  Opens a Fieldset Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Fieldset([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("fieldset", atts);
        }

        /// <summary>
        ///  Opens a Font Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Font([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("font", atts);
        }

        /// <summary>
        ///  Opens a Form Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Form([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("form", atts);
        }

        /// <summary>
        ///  Opens a Frame Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Frame([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("frame", atts);
        }

        /// <summary>
        ///  Opens a Frameset Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Frameset([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("frameset", atts);
        }

        /// <summary>
        ///  Opens a H1 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H1([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h1", atts);
        }

        /// <summary>
        ///  Opens a H2 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H2([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h2", atts);
        }

        /// <summary>
        ///  Opens a H3 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H3([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h3", atts);
        }

        /// <summary>
        ///  Opens a H4 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H4([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h4", atts);
        }

        /// <summary>
        ///  Opens a H5 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H5([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h5", atts);
        }

        /// <summary>
        ///  Opens a H6 Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter H6([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("h6", atts);
        }

        /// <summary>
        ///  Opens a Head Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Head([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("head", atts);
        }

        /// <summary>
        ///  Opens a Hr Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Hr([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("hr", atts);
        }

        /// <summary>
        ///  Opens a Html Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Html([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("html", atts);
        }

        /// <summary>
        ///  Opens a I Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter I([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("i", atts);
        }

        /// <summary>
        ///  Opens a Iframe Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Iframe([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("iframe", atts);
        }

        /// <summary>
        ///  Opens a Img Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Img([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("img", atts);
        }

        /// <summary>
        ///  Opens a Input Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Input([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("input", atts);
        }

        /// <summary>
        ///  Opens a Ins Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Ins([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("ins", atts);
        }

        /// <summary>
        ///  Opens a Isindex Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Isindex([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("isindex", atts);
        }

        /// <summary>
        ///  Opens a Kbd Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Kbd([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("kbd", atts);
        }

        /// <summary>
        ///  Opens a Label Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Label([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("label", atts);
        }

        /// <summary>
        ///  Opens a Legend Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Legend([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("legend", atts);
        }

        /// <summary>
        ///  Opens a Li Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Li([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("li", atts);
        }

        /// <summary>
        ///  Opens a Link Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Link([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("link", atts);
        }

        /// <summary>
        ///  Opens a Map Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Map([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("map", atts);
        }

        /// <summary>
        ///  Opens a Marquee Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Marquee([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("marquee", atts);
        }

        /// <summary>
        ///  Opens a Menu Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Menu([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("menu", atts);
        }

        /// <summary>
        ///  Opens a Meta Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Meta([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("meta", atts);
        }

        /// <summary>
        ///  Opens a Nobr Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Nobr([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("nobr", atts);
        }

        /// <summary>
        ///  Opens a Noframes Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Noframes([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("noframes", atts);
        }

        /// <summary>
        ///  Opens a Noscript Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Noscript([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("noscript", atts);
        }

        /// <summary>
        ///  Opens a Object Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Object([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("object", atts);
        }

        /// <summary>
        ///  Opens a Ol Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Ol([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("ol", atts);
        }

        /// <summary>
        ///  Opens a Option Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Option([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("option", atts);
        }

        /// <summary>
        ///  Opens a P Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter P([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("p", atts);
        }

        /// <summary>
        ///  Opens a Param Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Param([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("param", atts);
        }

        /// <summary>
        ///  Opens a Pre Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Pre([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("pre", atts);
        }

        /// <summary>
        ///  Opens a Q Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Q([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("q", atts);
        }

        /// <summary>
        ///  Opens a Rt Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Rt([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("rt", atts);
        }

        /// <summary>
        ///  Opens a Ruby Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Ruby([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("ruby", atts);
        }

        /// <summary>
        ///  Opens a S Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter S([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("s", atts);
        }

        /// <summary>
        ///  Opens a Samp Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Samp([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("samp", atts);
        }

        /// <summary>
        ///  Opens a Script Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Script([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("script", atts);
        }

        /// <summary>
        ///  Opens a Select Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Select([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("select", atts);
        }

        /// <summary>
        ///  Opens a Small Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Small([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("small", atts);
        }

        /// <summary>
        ///  Opens a Span Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Span([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("span", atts);
        }

        [NotNull]
        public static HtmlTextWriter Main([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("main", atts);
        }

        /// <summary>
        ///  Opens a Strike Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Strike([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("strike", atts);
        }

        /// <summary>
        ///  Opens a Strong Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Strong([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("strong", atts);
        }

        /// <summary>
        ///  Opens a Style Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Style([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("style", atts);
        }

        /// <summary>
        ///  Opens a Sub Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Sub([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("sub", atts);
        }

        /// <summary>
        ///  Opens a Sup Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Sup([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("sup", atts);
        }

        /// <summary>
        ///  Opens a Table Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Table([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("table", atts);
        }

        /// <summary>
        ///  Opens a Tbody Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Tbody([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("tbody", atts);
        }

        /// <summary>
        ///  Opens a Td Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Td([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("td", atts);
        }

        /// <summary>
        ///  Opens a Textarea Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Textarea([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("textarea", atts);
        }

        /// <summary>
        ///  Opens a Tfoot Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Tfoot([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("tfoot", atts);
        }

        /// <summary>
        ///  Opens a Th Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Th([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("th", atts);
        }

        /// <summary>
        ///  Opens a Thead Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Thead([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("thead", atts);
        }

        /// <summary>
        ///  Opens a Title Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Title([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("title", atts);
        }

        /// <summary>
        ///  Opens a Tr Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Tr([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("tr", atts);
        }

        /// <summary>
        ///  Opens a Tt Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Tt([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("tt", atts);
        }

        /// <summary>
        ///  Opens a U Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter U([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("u", atts);
        }

        /// <summary>
        ///  Opens a Ul Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Ul([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("ul", atts);
        }

        /// <summary>
        ///  Opens a Var Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Var([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("var", atts);
        }

        /// <summary>
        ///  Opens a Wbr Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Wbr([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("wbr", atts);
        }

        /// <summary>
        ///  Opens a Xml Html tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Xml([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("xml", atts);
        }


        /// <summary>
        ///  Closes the most recently opened tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter EndTag([NotNull] this HtmlTextWriter writer)
        {
            WritePreceeding(writer);
            writer.RenderEndTag();
            return writer;
        }

        /// <summary>
        ///  Writes  content to the current tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter WriteContent([NotNull] this HtmlTextWriter writer, [NotNull] string content)
        {
            WritePreceeding(writer);
            writer.Write(content);
            return writer;
        }

        /// <summary>
        ///  Writes the specified objects properties as tag attributes
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Attributes([NotNull] this HtmlTextWriter writer, [CanBeNull] object att)
        {
            _tags.Peek().AttributeContainer = att;

            return writer;
        }

        /// <summary>
        ///  Writes to the underlying writer any current content
        /// </summary>
        private static void WritePreceeding([NotNull] HtmlTextWriter writer)
        {
            if (_tags.Count > 0)
            {
                var popped = _tags.Pop();

                if (popped.AttributeContainer != null)
                {
                    if (popped.AttributeContainer is string[] attrArr) {
                        foreach (var pair in attrArr) {
                            var split = pair.Split('=');
                            writer.AddAttribute(split[0], split[1]);
                        }
                    }
                    else {
                        var props = popped.AttributeContainer.GetType().GetProperties();
                        foreach (var p in props) {
                            var val = p.GetValue(popped.AttributeContainer, null).ToString();
                            writer.AddAttribute(p.Name, val);
                        }
                    }
                }

                writer.RenderBeginTag(popped.TagName);
            }
        }

        /// <summary>
        ///  Helper class to store a tag and it's attributes on the stack
        /// </summary>
        private class Tag
        {
            [NotNull]
            public string TagName { get; set; }
            [CanBeNull]
            public object AttributeContainer { get; set; }

            public Tag([NotNull] string tagName, [CanBeNull] object container = null)
            {
                TagName = tagName;
                AttributeContainer = container;
            }
        }
    }
}
