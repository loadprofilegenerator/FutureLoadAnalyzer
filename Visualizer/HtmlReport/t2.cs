// ReSharper disable CheckNamespace
using JetBrains.Annotations;

namespace System.Web.UI
// ReSharper restore CheckNamespace
{
    /// <summary>
    ///  Extensions for HtmlTextWriter
    /// </summary>
    public static partial class HtmlWriterTextTagExtensions
    {
        [NotNull]
        private static HtmlTextWriter PushTag([NotNull] this HtmlTextWriter writer, [NotNull] string tagName, [CanBeNull] object atts = null)
        {
            WritePreceeding(writer);
            _tags.Push(new Tag(tagName, atts));
            return writer;
        }


        /// <summary>
        ///  Article tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Article([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("article", atts);
        }


        /// <summary>
        ///  Aside tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Aside([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("aside", atts);
        }

        /// <summary>
        ///  Audio tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Audio([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("audio", atts);
        }

        /// <summary>
        ///  Bdi tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Bdi([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("bdi", atts);
        }

        /// <summary>
        ///  Canvas tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Canvas([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("canvas", atts);
        }

        /// <summary>
        ///  Command tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Command([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("command", atts);
        }

        /// <summary>
        ///  Datalist tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Datalist([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("datalist", atts);
        }

        /// <summary>
        ///  Details tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Details([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("details", atts);
        }

        /// <summary>
        ///  FigCaption tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter FigCaption([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("figcaption", atts);
        }

        /// <summary>
        ///  Figure tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Figure([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("figure", atts);
        }

        /// <summary>
        ///  Footer tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Footer([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("footer", atts);
        }

        /// <summary>
        ///  Header tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Header([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("header", atts);
        }

        /// <summary>
        ///  hGroup tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter HGroup([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("hgroup", atts);
        }

        /// <summary>
        ///  Nav tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Nav([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("nav", atts);
        }

        /// <summary>
        ///  Section tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Section([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("section", atts);
        }

        /// <summary>
        /// Source tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Source([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("source", atts);
        }

        /// <summary>
        /// Video tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Video([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("video", atts);
        }

        /// <summary>
        /// KeyGen tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter KeyGen([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("keygen", atts);
        }

        /// <summary>
        /// Mark tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Mark([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("mark", atts);
        }

        /// <summary>
        /// Meter tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Meter([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("meter", atts);
        }

        /// <summary>
        /// Output tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Output([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("output", atts);
        }

        /// <summary>
        /// Progress tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Progress([NotNull]this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("progress", atts);
        }


        /// <summary>
        /// Summary tag
        /// </summary>
        [NotNull]
        public static HtmlTextWriter Summary([NotNull] this HtmlTextWriter writer, [CanBeNull] object atts = null)
        {
            return writer.PushTag("summary", atts);
        }

    }
}