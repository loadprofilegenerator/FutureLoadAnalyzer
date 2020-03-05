using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Common {
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class FlaException : Exception {
        public FlaException()
        {
        }

        public FlaException([NotNull] string message) : base(message)
        {
        }

        public FlaException([NotNull] string message, [NotNull] Exception innerException) : base(message, innerException)
        {
        }

        protected FlaException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}