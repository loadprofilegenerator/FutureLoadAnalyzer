using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Common {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class HarmlessFlaException : Exception {
        public HarmlessFlaException()
        {
        }

        public HarmlessFlaException([NotNull] string message) : base(message)
        {
        }

        public HarmlessFlaException([NotNull] string message, [NotNull] Exception innerException) : base(message, innerException)
        {
        }

        protected HarmlessFlaException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}