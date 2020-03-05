using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MessagePack;

namespace Data.Database {
    [MessagePackObject]
    public abstract class BasicSaveable<T> {

        [NotNull]
        [ItemNotNull]
        public  List<RowValue> GetRowForDatabase()
        {
            RowBuilder rb = new RowBuilder();
            object o = this;
            rb.Add("MessagePack", LZ4MessagePackSerializer.Serialize((T)o));
            SetAdditionalFieldsForRow(rb);
            return rb.RowValues;
        }

        public void SetFieldListToSave([NotNull] Action<string, SqliteDataType> addField)
        {
            addField("MessagePack", SqliteDataType.Blob);
            SetFieldListToSaveOtherThanMessagePack(addField);
        }

        protected abstract void SetAdditionalFieldsForRow([NotNull] RowBuilder rb);
        protected abstract void SetFieldListToSaveOtherThanMessagePack([NotNull] Action<string, SqliteDataType> addField);

    }
}