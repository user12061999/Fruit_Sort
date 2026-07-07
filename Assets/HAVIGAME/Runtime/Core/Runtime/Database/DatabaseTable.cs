using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HAVIGAME {
    public abstract class DatabaseTable<T, TData> : Database<T> where T : DatabaseTable<T, TData> where TData : new() {
        [SerializeField, AssetPath(typeof(TextAsset))] protected string tableData;
        [SerializeField] protected TData[] database;

        public int GetCount() {
            return database.Length;
        }

        public int GetCount<TResult>() where TResult : TData {
            int count = 0;
            foreach (var item in database) {
                if (item is TResult) count++;
            }
            return count;
        }

        public TData GetDataByIndex(int index) {
            if (index < 0 || index >= database.Length) {
                return default;
            }
            else {
                return database[index];
            }
        }

        public int IndexOf(TData data) {
            return Array.IndexOf(database, data);
        }

        public IEnumerable<TData> GetAll() {
            foreach (var item in database) {
                yield return item;
            }
        }

        public IEnumerable<TResult> GetAll<TResult>() where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result) yield return result;
            }
        }

        public IEnumerable<TData> GetAll(Func<TData, bool> condition) {
            foreach (var item in database) {
                if (condition.Invoke(item)) yield return item;
            }
        }

        public IEnumerable<TData> GetAll<TResult>(Func<TResult, bool> condition) where TResult : TData {
            foreach (var item in database) {
                if (item is TResult result && condition.Invoke(result)) yield return item;
            }
        }

        public TData this[int index] {
            get {
                return database[index];
            }
        }

#if UNITY_EDITOR
        protected override void InstallDatabase() {

            string tablePath = Path.Combine(Application.dataPath, tableData.Remove(0, 7));

            string extension = Path.GetExtension(tablePath);
            string delimiter = GetDelimitertByExtension(extension);

            if (string.IsNullOrEmpty(delimiter)) {
                EditorUtility.DisplayDialog("File type is not supported!", "The current version only supports the following file types: CSV, TSV.", "Ok");
            }
            else {
                using (StreamReader streamReader = new StreamReader(tablePath)) {
                    CSVReader csvReader = new CSVReader(streamReader, delimiter);

                    if (csvReader.Read()) {
                        Dictionary<int, string> header = new Dictionary<int, string>(csvReader.FieldsCount);

                        for (int i = 0; i < csvReader.FieldsCount; i++) {
                            string value = csvReader[i];

                            if (!string.IsNullOrEmpty(value) && !value.StartsWith("#")) {
                                header.Add(i, value);
                            }
                        }

                        List<TData> datas = new List<TData>(64);

                        while (csvReader.Read()) {
                            TData element = new TData();
                            Type elementType = element.GetType();

                            FieldInfo[] fieldInfos = elementType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            MethodInfo[] methodInfos = elementType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            for (int i = 0; i < csvReader.FieldsCount; i++) {
                                string value = csvReader[i];

                                if (header.ContainsKey(i)) {
                                    foreach (FieldInfo fieldInfo in fieldInfos) {
                                        DataFieldAttribute attribute = fieldInfo.GetCustomAttribute<DataFieldAttribute>();

                                        string fileName = attribute != null ? attribute.name : fieldInfo.Name;

                                        if (fileName.Equals(header[i])) {
                                            SetValue(element, fieldInfo, value);

                                            if (!string.IsNullOrEmpty(attribute.onConvertCallback)) {
                                                foreach (MethodInfo methodInfo in methodInfos) {
                                                    if (methodInfo.Name.Equals(attribute.onConvertCallback)) {
                                                        methodInfo.Invoke(element, new object[] { value });
                                                        break;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }

                            datas.Add(element);
                        }

                        database = datas.ToArray();
                    }
                }
            }

            base.InstallDatabase();
        }

        private void SetValue(object obj, FieldInfo fieldInfo, string stringValue) {
            Type fieldType = fieldInfo.FieldType;

            if (fieldType == typeof(Int16)) {
                fieldInfo.SetValue(obj, Int16.Parse(stringValue));
            }
            else if (fieldType == typeof(Int32)) {
                fieldInfo.SetValue(obj, Int32.Parse(stringValue));
            }
            else if (fieldType == typeof(Int64)) {
                fieldInfo.SetValue(obj, Int64.Parse(stringValue));
            }
            else if (fieldType == typeof(UInt16)) {
                fieldInfo.SetValue(obj, UInt16.Parse(stringValue));
            }
            else if (fieldType == typeof(UInt32)) {
                fieldInfo.SetValue(obj, UInt32.Parse(stringValue));
            }
            else if (fieldType == typeof(UInt64)) {
                fieldInfo.SetValue(obj, UInt64.Parse(stringValue));
            }
            else if (fieldType == typeof(Boolean)) {
                fieldInfo.SetValue(obj, Boolean.Parse(stringValue));
            }
            else if (fieldType == typeof(Single)) {
                fieldInfo.SetValue(obj, Single.Parse(stringValue));
            }
            else if (fieldType == typeof(Char)) {
                fieldInfo.SetValue(obj, Char.Parse(stringValue));
            }
            else if (fieldType == typeof(Double)) {
                fieldInfo.SetValue(obj, Double.Parse(stringValue));
            }
            else if (fieldType == typeof(Byte)) {
                fieldInfo.SetValue(obj, Byte.Parse(stringValue));
            }
            else if (fieldType == typeof(Decimal)) {
                fieldInfo.SetValue(obj, Decimal.Parse(stringValue));
            }
            else if (fieldType.IsEnum) {
                fieldInfo.SetValue(obj, Enum.Parse(fieldType, stringValue));
            }
            else if (fieldType == typeof(String)) {
                fieldInfo.SetValue(obj, stringValue);
            }
        }

        private string GetDelimitertByExtension(string extension) {
            switch (extension) {
                case ".csv": return ",";
                case ".tsv":
                    return "\t";
                default: return null;
            }
        }

        private class CSVReader {

            private TextReader reader;
            private int delimLength;

            public string Delimiter { get; private set; }
            public int BufferSize { get; private set; }
            public bool TrimFields { get; private set; }


            public CSVReader(TextReader reader, string delimiter = ",", int bufferSize = 32768, bool trimFields = true) {
                this.reader = reader;
                this.delimLength = delimiter.Length;
                this.Delimiter = delimiter;
                this.BufferSize = bufferSize;
                this.TrimFields = trimFields;

                if (delimLength == 0) {
                    throw new ArgumentException("Delimiter cannot be empty.");
                }
            }

            private char[] buffer = null;
            private int bufferLength;
            private int bufferLoadThreshold;
            private int lineStartPos = 0;
            private int actualBufferLen = 0;
            private List<Field> fields = null;
            private int fieldsCount = 0;
            private int linesRead = 0;

            private int ReadBlockAndCheckEof(char[] buffer, int start, int len, ref bool eof) {
                if (len == 0)
                    return 0;
                var read = reader.ReadBlock(buffer, start, len);
                if (read < len)
                    eof = true;
                return read;
            }

            private bool FillBuffer() {
                var eof = false;
                var toRead = bufferLength - actualBufferLen;
                if (toRead >= bufferLoadThreshold) {
                    int freeStart = (lineStartPos + actualBufferLen) % buffer.Length;
                    if (freeStart >= lineStartPos) {
                        actualBufferLen += ReadBlockAndCheckEof(buffer, freeStart, buffer.Length - freeStart, ref eof);
                        if (lineStartPos > 0)
                            actualBufferLen += ReadBlockAndCheckEof(buffer, 0, lineStartPos, ref eof);
                    }
                    else {
                        actualBufferLen += ReadBlockAndCheckEof(buffer, freeStart, toRead, ref eof);
                    }
                }
                return eof;
            }

            private string GetLineTooLongMsg() {
                return String.Format("CSV line #{1} length exceedes buffer size ({0})", BufferSize, linesRead);
            }

            private int ReadQuotedFieldToEnd(int start, int maxPos, bool eof, ref int escapedQuotesCount) {
                int pos = start;
                int chIdx;
                char ch;
                for (; pos < maxPos; pos++) {
                    chIdx = pos < bufferLength ? pos : pos % bufferLength;
                    ch = buffer[chIdx];
                    if (ch == '\"') {
                        bool hasNextCh = (pos + 1) < maxPos;
                        if (hasNextCh && buffer[(pos + 1) % bufferLength] == '\"') {
                            pos++;
                            escapedQuotesCount++;
                        }
                        else {
                            return pos;
                        }
                    }
                }
                if (eof) {
                    return pos - 1;
                }
                throw new InvalidDataException(GetLineTooLongMsg());
            }

            private bool ReadDelimTail(int start, int maxPos, ref int end) {
                int pos;
                int idx;
                int offset = 1;
                for (; offset < delimLength; offset++) {
                    pos = start + offset;
                    idx = pos < bufferLength ? pos : pos % bufferLength;
                    if (pos >= maxPos || buffer[idx] != Delimiter[offset])
                        return false;
                }
                end = start + offset - 1;
                return true;
            }

            private Field GetOrAddField(int startIdx) {
                fieldsCount++;
                while (fieldsCount > fields.Count)
                    fields.Add(new Field());
                var f = fields[fieldsCount - 1];
                f.Reset(startIdx);
                return f;
            }

            public int FieldsCount {
                get {
                    return fieldsCount;
                }
            }

            public string this[int idx] {
                get {
                    if (idx < fieldsCount) {
                        var f = fields[idx];
                        return fields[idx].GetValue(buffer);
                    }
                    return null;
                }
            }

            public int GetValueLength(int idx) {
                if (idx < fieldsCount) {
                    var f = fields[idx];
                    return f.Quoted ? f.Length - f.EscapedQuotesCount : f.Length;
                }
                return -1;
            }

            public void ProcessValueInBuffer(int idx, Action<char[], int, int> handler) {
                if (idx < fieldsCount) {
                    var f = fields[idx];
                    if ((f.Quoted && f.EscapedQuotesCount > 0) || f.End >= bufferLength) {
                        var chArr = f.GetValue(buffer).ToCharArray();
                        handler(chArr, 0, chArr.Length);
                    }
                    else if (f.Quoted) {
                        handler(buffer, f.Start + 1, f.Length - 2);
                    }
                    else {
                        handler(buffer, f.Start, f.Length);
                    }
                }
            }

            public bool Read() {
            Start:
                if (fields == null) {
                    fields = new List<Field>();
                    fieldsCount = 0;
                }
                if (buffer == null) {
                    bufferLoadThreshold = Math.Min(BufferSize, 8192);
                    bufferLength = BufferSize + bufferLoadThreshold;
                    buffer = new char[bufferLength];
                    lineStartPos = 0;
                    actualBufferLen = 0;
                }

                var eof = FillBuffer();

                fieldsCount = 0;
                if (actualBufferLen <= 0) {
                    return false;
                }
                linesRead++;

                int maxPos = lineStartPos + actualBufferLen;
                int charPos = lineStartPos;

                var currentField = GetOrAddField(charPos);
                bool ignoreQuote = false;
                char delimFirstChar = Delimiter[0];
                bool trimFields = TrimFields;

                int charBufIdx;
                char ch;
                for (; charPos < maxPos; charPos++) {
                    charBufIdx = charPos < bufferLength ? charPos : charPos % bufferLength;
                    ch = buffer[charBufIdx];
                    switch (ch) {
                        case '\"':
                            if (ignoreQuote) {
                                currentField.End = charPos;
                            }
                            else if (currentField.Quoted || currentField.Length > 0) {
                                currentField.End = charPos;
                                currentField.Quoted = false;
                                ignoreQuote = true;
                            }
                            else {
                                var endQuotePos = ReadQuotedFieldToEnd(charPos + 1, maxPos, eof, ref currentField.EscapedQuotesCount);
                                currentField.Start = charPos;
                                currentField.End = endQuotePos;
                                currentField.Quoted = true;
                                charPos = endQuotePos;
                            }
                            break;
                        case '\r':
                            if ((charPos + 1) < maxPos && buffer[(charPos + 1) % bufferLength] == '\n') {
                                charPos++;
                            }
                            charPos++;
                            goto LineEnded;
                        case '\n':
                            charPos++;
                            goto LineEnded;
                        default:
                            if (ch == delimFirstChar && (delimLength == 1 || ReadDelimTail(charPos, maxPos, ref charPos))) {
                                currentField = GetOrAddField(charPos + 1);
                                ignoreQuote = false;
                                continue;
                            }
                            if (ch == ' ' && trimFields) {
                                continue;
                            }

                            if (currentField.Length == 0) {
                                currentField.Start = charPos;
                            }

                            if (currentField.Quoted) {
                                currentField.Quoted = false;
                                ignoreQuote = true;
                            }
                            currentField.End = charPos;
                            break;
                    }

                }
                if (!eof) {
                    throw new InvalidDataException(GetLineTooLongMsg());
                }
            LineEnded:
                actualBufferLen -= charPos - lineStartPos;
                lineStartPos = charPos % bufferLength;

                if (fieldsCount == 1 && fields[0].Length == 0) {
                    goto Start;
                }

                return true;
            }


            internal sealed class Field {
                internal int Start;
                internal int End;
                internal int Length {
                    get { return End - Start + 1; }
                }
                internal bool Quoted;
                internal int EscapedQuotesCount;
                string cachedValue = null;

                internal Field() {
                }

                internal Field Reset(int start) {
                    Start = start;
                    End = start - 1;
                    Quoted = false;
                    EscapedQuotesCount = 0;
                    cachedValue = null;
                    return this;
                }

                internal string GetValue(char[] buf) {
                    if (cachedValue == null) {
                        cachedValue = GetValueInternal(buf);
                    }
                    return cachedValue;
                }

                string GetValueInternal(char[] buf) {
                    if (Quoted) {
                        var s = Start + 1;
                        var lenWithoutQuotes = Length - 2;
                        var val = lenWithoutQuotes > 0 ? GetString(buf, s, lenWithoutQuotes) : String.Empty;
                        if (EscapedQuotesCount > 0)
                            val = val.Replace("\"\"", "\"");
                        return val;
                    }
                    var len = Length;
                    return len > 0 ? GetString(buf, Start, len) : String.Empty;
                }

                private string GetString(char[] buf, int start, int len) {
                    var bufLen = buf.Length;
                    start = start < bufLen ? start : start % bufLen;
                    var endIdx = start + len - 1;
                    if (endIdx >= bufLen) {
                        var prefixLen = buf.Length - start;
                        var prefix = new string(buf, start, prefixLen);
                        var suffix = new string(buf, 0, len - prefixLen);
                        return prefix + suffix;
                    }
                    return new string(buf, start, len);
                }

            }

        }

#endif
    }

    public abstract class DatabaseTable<T, TId, TData> : DatabaseTable<T, TData> where T : DatabaseTable<T, TId, TData> where TData : IIdentify<TId>, new() {
        [System.NonSerialized] private Dictionary<TId, TData> databaseDictionary;

        public override void Initialize() {
            base.Initialize();

            databaseDictionary = new Dictionary<TId, TData>(database.Length);

            foreach (TData data in GetAll()) {
                databaseDictionary[data.Id] = data;
            }
        }

        public bool ConstainsId(TId id) {
            return databaseDictionary.ContainsKey(id);
        }

        public TData GetDataById(TId id) {
            return databaseDictionary[id];
        }

        public bool TryGetDataById(TId id, out TData data) {
            return databaseDictionary.TryGetValue(id, out data);
        }
    }
}