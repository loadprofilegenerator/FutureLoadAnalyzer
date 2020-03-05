using System;
using Common.Steps;
using JetBrains.Annotations;
using Newtonsoft.Json;
using SQLite;

namespace BurgdorfStatistics.Logging {
    [NPoco.PrimaryKey(nameof(ID))]
    public class LogMessage {
        public LogMessage(MessageType messageType, [JetBrains.Annotations.NotNull] string message, [JetBrains.Annotations.NotNull] string stepName, Stage dstStage, [CanBeNull] object o)
        {
            MessageType = messageType;
            Message = message;
            StepName = stepName;
            DstStage = dstStage;
            SourceObj = o;
            Time = DateTime.Now.ToLongTimeString();
        }

        public MessageType MessageType { get; set; }

        [JetBrains.Annotations.NotNull]
        public string MessageTypeDesc {
            get => MessageType.ToString();
            // ReSharper disable once ValueParameterNotUsed
#pragma warning disable S108 // Nested blocks of code should not be left empty
#pragma warning disable S3237 // "value" parameters should be used
            set { }
#pragma warning restore S3237 // "value" parameters should be used
#pragma warning restore S108 // Nested blocks of code should not be left empty
        }

        [JetBrains.Annotations.NotNull]
        public string Time { get; set; }

        [PrimaryKey]
        [AutoIncrement]
        [UsedImplicitly]
        public int ID { get; set; }

        [JetBrains.Annotations.NotNull]
        public string Message { get; set; }

        [JetBrains.Annotations.NotNull]
        public string StepName { get; set; }

        public Stage DstStage { get; set; }

        [CanBeNull]
        [JsonIgnore]
        [Ignore]
        [NPoco.Ignore]
        public object SourceObj { get; }

        [JetBrains.Annotations.NotNull]
        public string JsonObject {
            get => JsonConvert.SerializeObject(SourceObj, Formatting.Indented);
            // ReSharper disable once ValueParameterNotUsed
            // ReSharper disable once EmptyStatement
#pragma warning disable S3237 // "value" parameters should be used
#pragma warning disable S108 // Nested blocks of code should not be left empty
            set { }
#pragma warning restore S108 // Nested blocks of code should not be left empty
#pragma warning restore S3237 // "value" parameters should be used
        }
    }
}