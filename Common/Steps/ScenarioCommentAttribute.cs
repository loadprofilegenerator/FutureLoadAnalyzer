using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Common.Steps {
    [AttributeUsage(AttributeTargets.All)]
    public class ScenarioCommentAttribute : Attribute {
        private ScenarioCategory _category;
        [NotNull] private string _text;

        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized")]
        public ScenarioCommentAttribute([NotNull] string text, ScenarioCategory category)
        {
            Text = text;
            Category = category;
        }

        public ScenarioCategory Category {
            get => _category;

            set => _category = value;
        }

        [NotNull]
        public string Text {
            get => _text;

            set => _text = value.Replace("\n", " ").Replace("\r", " ").Replace("  ", " ");
        }
    }
}