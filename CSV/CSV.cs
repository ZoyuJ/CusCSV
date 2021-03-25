namespace CSV {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Reflection;
  using System.Text;


  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVColumnAttribute : Attribute, IComparable<CSVColumnAttribute> {
    public string HeaderText { get; set; }
    public int Order { get; set; }
    public string TableName { get; set; } = CSVTableAttribute.DefaultTableName;
    public CSVColumnAttribute(string Header) { HeaderText = Header; }

    public int CompareTo([AllowNull] CSVColumnAttribute other) {
      return other == null ? 0 : Order.CompareTo(other.Order);
    }
    internal MemberInfo Member { get; set; }

    internal int PreAllocIndex { get; set; }
  }
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
  public class CSVTableAttribute : Attribute {
    public const string DefaultTableName = "default";
    public string Name { get; set; } = DefaultTableName;
    public const char Separator = ',';
    public const string NewLine = "\r\n";
  }
  /*
    RFC 4180 https://tools.ietf.org/html/rfc4180
     xx1,yy1,zz1 \r\n xx2,yy2,zz2                         => [{xx1,yy1,zz1},{xx2,yy2,zz2}]
     "xx1","yy1","zz1" \r\n "xx2","yy2","zz2"             => [{xx1,yy1,zz1},{xx2,yy2,zz2}]
     "xx1","y \r\n , "y1","zz1" \r\n "xx2","yy2","zz2"    => [{xx1,y \r\n \, y1,zz1},{xx2,yy2,zz2}]
     "x""x1","yy1","zz1" \r\n "xx2","yy2","zz2"           => [{x\"x1,yy1,zz1},{xx2,yy2,zz2}]
  */

  public partial class CSVConvert {

    //public static string ToCSV<T>(IEnumerable<T> Ins, string Name = "default", in bool WriteHeader = true, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
    //  StringBuilder CSV = new StringBuilder();
    //  ToCSV(Ins, ref CSV, Name, WriteHeader, SerializFuncs);
    //  return CSV.ToString();
    //}
    //public static void ToCSV<T>(IEnumerable<T> Ins, ref StringBuilder CSVBuilder, string Name = "default", in bool WriteHeader = true, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
    //  var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
    //  if (Attr != null) {
    //    ToHeadersRow<T>(ref CSVBuilder, Attr, out var Map, out var Sorted, WriteHeader, false);
    //    foreach (var item in Ins) {
    //      CSVBuilder.AppendLine();
    //      ToCSVRowLv1(item, ref CSVBuilder, in Attr, Map, Sorted, SerializFuncs: SerializFuncs);
    //    }
    //    return;
    //  }
    //  throw new InvalidOperationException("No CSV Fig");

    //}
    //public static void ToCSVHeader<T>(ref StringBuilder CSVBuilder, string Name = "default", in bool AppendNewLine = false) where T : new() {
    //  var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
    //  ToHeadersRow<T>(ref CSVBuilder, Attr, out _, out _, true, AppendNewLine);
    //}
    //internal static void ToHeadersRow<T>(ref StringBuilder SB, CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted, in bool WriteHeader = true, in bool AppendNewLine = false) where T : new() {
    //  CollectHeaders<T>(Table, out Map, out Sorted);
    //  if (WriteHeader && SB != null) {
    //    for (int i = 0; i < Sorted.Count; i++) {
    //      var HeaderText = Sorted[i].HeaderText ?? Map[Sorted[i]].Name;
    //      for (int j = 0; j < HeaderText.Length; j++) {
    //        if (HeaderText[j] == CSVTableAttribute.Separator) SB.Append('\\');
    //        SB.Append(HeaderText[j]);
    //      }
    //      SB.Append(CSVTableAttribute.Separator);
    //    }
    //    SB.Remove(SB.Length - 1, 1);
    //    if (AppendNewLine) SB.AppendLine();
    //  }
    //}
    //internal static string ToHeadersRowString(CSVTableAttribute Table, in IReadOnlyList<CSVColumnAttribute> Sorted) => string.Join(CSVTableAttribute.Separator, Sorted.Select(E => E.HeaderText));
    //internal static void ToCSVRowLv1<T>(T Ins, ref StringBuilder SB, in CSVTableAttribute Table, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted, in bool AppendNewLine = false, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
    //  for (int i = 0; i < Sorted.Count; i++) {
    //    switch (Map[Sorted[i]]) {
    //      case PropertyInfo Prop:
    //        bool HasNotCustomizSerialFuncP = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText));
    //        var PContent = HasNotCustomizSerialFuncP ? (Prop.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Prop.GetValue(Ins) ?? "");
    //        if (!HasNotCustomizSerialFuncP) {
    //          SB.Append(PContent);
    //        }
    //        else {
    //          for (int j = 0; j < PContent.Length; j++) {
    //            if (PContent[j] == CSVTableAttribute.Separator) SB.Append('\\');
    //            SB.Append(PContent[j]);
    //          }
    //        }
    //        SB.Append(CSVTableAttribute.Separator);
    //        break;
    //      case FieldInfo Field:
    //        bool HasNotCustomizSerialFuncF = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText));
    //        var FContent = HasNotCustomizSerialFuncF ? (Field.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Field.GetValue(Ins) ?? "");
    //        if (!HasNotCustomizSerialFuncF) {
    //          SB.Append(FContent);
    //        }
    //        else {
    //          for (int j = 0; j < FContent.Length; j++) {
    //            if (FContent[j] == CSVTableAttribute.Separator) SB.Append('\\');
    //            SB.Append(FContent[j]);
    //          }
    //        }
    //        SB.Append(CSVTableAttribute.Separator);
    //        break;
    //    }
    //  }
    //  SB.Remove(SB.Length - 1, 1);
    //  if (AppendNewLine) SB.AppendLine();
    //}

    //internal static readonly Dictionary<Type, Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>> ReflactCache = new Dictionary<Type, Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>>();
    //internal static bool GetHeaderMapCache<T>(in CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
    //  if (ReflactCache.TryGetValue(typeof(T), out var D)) {
    //    if (D.TryGetValue(Table.Name, out var Is)) {
    //      Map = Is.Item1;
    //      Sorted = Is.Item2;
    //      return true;
    //    }
    //  }
    //  Map = null;
    //  Sorted = null;
    //  return false;
    //}
    //internal static void SetHeaderMapCache<T>(in CSVTableAttribute Table, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
    //  if (ReflactCache.TryGetValue(typeof(T), out var D)) {
    //    if (D.TryGetValue(Table.Name, out var Is)) {
    //      Is.Item1 = Map;
    //      Is.Item2 = Sorted;
    //    }
    //    else {
    //      D[Table.Name] = (Map, Sorted);
    //    }
    //  }
    //  else {
    //    D = new Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>();
    //    D.Add(Table.Name, (Map, Sorted));
    //    ReflactCache[typeof(T)] = D;
    //  }
    //}
    //internal static void CollectHeaders<T>(CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
    //  if (GetHeaderMapCache<T>(Table, out Map, out Sorted)) return;
    //  var _Map = new Dictionary<CSVColumnAttribute, MemberInfo>();
    //  var _Sorted = new List<CSVColumnAttribute>();
    //  var AllMembers = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).ToArray();
    //  for (int i = 0; i < AllMembers.Length; i++) {
    //    var ColAttrs = AllMembers[i].GetCustomAttributes<CSVColumnAttribute>(false);
    //    var ColAttr = ColAttrs.Where(EE => EE.TableName == Table.Name).FirstOrDefault();
    //    if (ColAttr != null) {
    //      _Map[ColAttr] = AllMembers[i];
    //      _Sorted.Add(ColAttr);
    //    }
    //  }
    //  _Sorted.Sort();
    //  Map = _Map;
    //  Sorted = _Sorted;
    //  SetHeaderMapCache<T>(Table, Map, Sorted);
    //}
    //public static void ClearReflectCache() { ReflactCache.Clear(); }

    //[Obsolete("Not Support", true)]
    //internal static IEnumerable<string> EveryLine(string Data, string LineSpread) {
    //  int StartIndex = 0;
    //  int Len = 0;
    //  int LineSpreadLen = LineSpread.Length;
    //  if (LineSpreadLen == 1) {
    //    char _LineSpread = LineSpread[0];
    //    for (int i = 0; i < Data.Length; i++) {
    //      if (Data[i] == _LineSpread) {
    //        Len = i - StartIndex;
    //        yield return Data.Substring(StartIndex, Len);
    //        StartIndex = i + 1;
    //      }
    //    }
    //  }
    //  else {
    //    char _LineSpread = LineSpread[0];
    //    for (int i = 0; i < Data.Length; i++) {
    //      if (Data[i] == _LineSpread) {
    //        Len = i - StartIndex;
    //        yield return Data.Substring(StartIndex, Len);
    //        StartIndex = i + LineSpreadLen;
    //      }
    //    }
    //  }
    //}
    //internal static IEnumerable<string> EveryLine(string Data) {
    //  return EveryLine(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))));
    //}
    //internal static IEnumerable<string> EveryLine(TextReader Data) {
    //  string Line = Data.ReadLine();
    //  while (Line != null) {
    //    yield return Line;
    //    Line = Data.ReadLine();
    //  }
    //  yield break;
    //}




    //public static IEnumerable<T> FromCSV<T>(string Data, string Name = "default") where T : new() {
    //  var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
    //  if (Attr == null) throw new InvalidOperationException("No CSV Fig");
    //  return FromCSV<T>(Attr, EveryLine(Data), Name);
    //}
    //public static IEnumerable<T> FromCSV<T>(TextReader Data, string Name = "default") where T : new() {
    //  var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
    //  if (Attr == null) throw new InvalidOperationException("No CSV Fig");
    //  return FromCSV<T>(Attr, EveryLine(Data), Name);
    //}
    //internal static IEnumerable<T> FromCSV<T>(CSVTableAttribute Table, IEnumerable<string> Data, string Name = "default") where T : new() {
    //  CollectHeaders<T>(Table, out var Map, out var Sorted);
    //  var DataItor = Data.GetEnumerator();
    //  //DataItor.Reset();
    //  if (MoveToHeader(ToHeadersRowString(Table, Sorted), DataItor)) {
    //    while (DataItor.MoveNext())
    //      yield return FromCSVRow<T>(EveryItem(DataItor.Current, Table).ToArray(), Map, Sorted);
    //    DataItor.Dispose();
    //    yield break;
    //  }
    //  else {
    //    throw new CSVHeaderNotExistException();
    //  }
    //}
    //internal static bool MoveToHeader(in string HeaderRowStr, IEnumerator<string> DataItor) {
    //  while (DataItor.MoveNext()) {
    //    if (DataItor.Current.Trim(' ', '\n', '\r') == HeaderRowStr) return true;
    //  }
    //  return false;
    //}
    //internal static IEnumerable<string> EveryItem(string Data, CSVTableAttribute Table) {
    //  int StartIndex = 0;
    //  StringBuilder CacheBackSlash = new StringBuilder();
    //  for (int i = 0; i < Data.Length; i++) {
    //    if (Data[i] == ',') {

    //      var RawItem = Data.Substring(StartIndex, i - StartIndex);
    //      StartIndex = i + 1;
    //      if (RawItem.Length != 0) {
    //        CacheBackSlash.Append(RawItem);
    //        if (CacheBackSlash[CacheBackSlash.Length - 1] == '\\') {
    //          CacheBackSlash[CacheBackSlash.Length - 1] = CSVTableAttribute.Separator;
    //          continue;
    //        }
    //      }
    //      yield return CacheBackSlash.ToString();
    //      CacheBackSlash.Clear();
    //    }
    //  }
    //  int LastLen = Data.Length - StartIndex;
    //  yield return LastLen <= 0 ? "" : Data.Substring(StartIndex, LastLen);
    //}
    //internal static T FromCSVRow<T>(string[] RowContents, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted, in IReadOnlyDictionary<string, Func<string, object>> DeserializFuncs = null) where T : new() {
    //  var Data = new T();
    //  for (int i = 0; i < Sorted.Count; i++) {
    //    if (!string.IsNullOrEmpty(RowContents[i])) {
    //      if (Map.TryGetValue(Sorted[i], out var Member)) {
    //        switch (Member) {
    //          case PropertyInfo Prop:
    //            Prop.SetValue(Data, (DeserializFuncs == null || !DeserializFuncs.ContainsKey(Sorted[i].HeaderText)) ? ConvertDynamic(RowContents[i], Prop.PropertyType) : DeserializFuncs[Sorted[i].HeaderText](RowContents[i]));
    //            break;
    //          case FieldInfo Field:
    //            Field.SetValue(Data, (DeserializFuncs == null || !DeserializFuncs.ContainsKey(Sorted[i].HeaderText)) ? ConvertDynamic(RowContents[i], Field.FieldType) : DeserializFuncs[Sorted[i].HeaderText](RowContents[i]));
    //            break;
    //        }
    //      }
    //    }
    //  }
    //  return Data;
    //}

    internal static object ConvertDynamic(string Value, Type T) {
      if (string.IsNullOrEmpty(Value)) return null;
      TypeConverter TCr = TypeDescriptor.GetConverter(T);
      if (TCr.CanConvertFrom(Value.GetType()))
        return TCr.ConvertFromString(Value);
      else
        return Convert.ChangeType(Value, T);
    }




  }

  class Field {
    private readonly StringBuilder _Chars;
    public int Count { get; private set; }
    public int Offset { get; private set; }
    private bool Enclosed { get; set; } = true;

    public Field() { Offset = 0; Count = -1; _Chars = new StringBuilder(); }
    public Field(int Offset) : this() { this.Offset = Offset; }

    public Field NextChar(char Char1, out bool Eof) {
      Eof = false;
      Count += 1;
      if (Char1 == '\"') {
        Enclosed = !Enclosed;
      }
      if (Enclosed) {
        if (Char1 == ',') {
          return new Field(Offset + Count);
        }
        else if (Char1 == '\r' && _Chars[_Chars.Length - 1] == '\n') {
          Eof = true;
          _Chars.Remove(_Chars.Length - 1, 1);
          return null;
        }
      }
      else {
        if (_Chars.Length == 0 || _Chars[_Chars.Length - 1] != '\"') _Chars.Append(Char1);
      }
      return null;
    }
    private string _Raw;
    public string Raw {
      get {
        if (_Raw == null) _Raw = _Chars.ToString();
        return _Raw;
      }
    }
    public override string ToString() => Raw;
  }
  class Row {
    protected readonly List<Field> _Fields;
    public int Count { get => _Fields.Where(E => E.Count > 0).Sum(E => E.Count); }
    public int Offset { get => _Fields[0].Offset; }
    public Row() { }
    public Row(int Offset) { _Fields = new List<Field>() { new Field(Offset) }; }
    public Row NextChar(char Char1) {
      var NextField = _Fields[_Fields.Count - 1].NextChar(Char1, out var Eof);
      if (Eof) {
        return new Row(Offset + Count);
      }
      else if (NextField != null) {
        _Fields.Add(NextField);
      }
      return null;
    }
    public Field this[int ColumnIndex] {
      get => _Fields[ColumnIndex];
    }

  }
  class Header : Row {
    internal class MemberTypeValidor {
      private static readonly HashSet<Type> BasicTypes = new HashSet<Type>(new Type[] {
          typeof(DateTime),typeof(string),typeof(bool),
          typeof(sbyte),typeof(short),typeof(int),typeof(long),
          typeof(byte),typeof(ushort),typeof(uint),typeof(ulong),
          typeof(float),typeof(decimal),typeof(double),
        });
      public static bool Validor(PropertyInfo Property)
        => Property.CanWrite
                  && (
                      Property.PropertyType.IsEnum
                    || BasicTypes.Contains(Property.PropertyType)
                    || BasicTypes.Contains(
                      Property.PropertyType.IsArray
                      ? Property.PropertyType.GetElementType()
                      : typeof(IList).IsAssignableFrom(Property.PropertyType)
                        ? Property.PropertyType.GetGenericArguments()[0]
                        : typeof(object))
                    );

      public static bool Validor(FieldInfo Field)
              => !Field.IsInitOnly
                  && (
                      Field.FieldType.IsEnum
                    || BasicTypes.Contains(Field.FieldType)
                    || BasicTypes.Contains(
                      Field.FieldType.IsArray
                      ? Field.FieldType.GetElementType()
                      : typeof(IList).IsAssignableFrom(Field.FieldType)
                        ? Field.FieldType.GetGenericArguments()[0]
                        : typeof(object))
                    );

      public static bool Validor(MemberInfo Member) => Member is PropertyInfo P ? Validor(P) : Member is FieldInfo F ? Validor(F) : false;

      public static bool ValidorDictValue(Type VType) =>
        BasicTypes.Contains(VType.IsArray ? VType.GetElementType() : typeof(IList).IsAssignableFrom(VType) ? VType.GetGenericArguments()[0]
                           : VType);

      public static bool IsBasicType(Type Type) => Type.IsEnum || BasicTypes.Contains(Type);

    }
    internal List<CSVColumnAttribute> _ColAttrs;
    public void MapToModel(Type ModelType, string TargetTable = CSVTableAttribute.DefaultTableName) {
      if (MemberTypeValidor.IsBasicType(ModelType)) {
        //no header
      }
      else if (ModelType.IsArray || typeof(IList).IsAssignableFrom(ModelType)) {
        //no header
      }
      else if (typeof(IDictionary).IsAssignableFrom(ModelType)) {
        //no key header
        MapToModel(ModelType.GetGenericArguments()[1]);
      }
      else {
        _ColAttrs = ModelType.GetFields(BindingFlags.Public | BindingFlags.Instance)
          .Cast<MemberInfo>()
          .Concat(ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
          .Select(E => (E, E.GetCustomAttributes<CSVColumnAttribute>().FirstOrDefault(E => E.TableName == TargetTable)))
          .Where(E => E.Item2 != null && MemberTypeValidor.Validor(E.E))
          .Select(E => {
            E.Item2.Member = E.E;
            if (string.IsNullOrEmpty(E.Item2.HeaderText)) E.Item2.HeaderText = E.E.Name;
            return E.Item2;
          })
          .OrderBy(E => E.Order)
          .ToList();

        if (_Fields != null && _Fields.Count > 0) {
          for (int i = _Fields[0].Raw == "" ? 1 : 0, j = 0; i < _Fields.Count; i++) {
            if (_Fields[i].Raw == "") {
              _ColAttrs.Insert(j, _ColAttrs[j - 1]);
            }
          }
        }
      }

    }



  }
  class Table {
    public readonly Header Header;
    private readonly List<Row> _Rows;
    public readonly bool HasHeader;

    public Table() { Header = null; _Rows = new List<Row>() { new Row() }; }
    public Table(bool HasHeader) {
      this.HasHeader = HasHeader;
      Header = new Header();
      _Rows = new List<Row>();
    }

    internal void NextChar(char Char1) {
      if (HasHeader && _Rows.Count == 0) {
        var NextRow = Header.NextChar(Char1);
        if (NextRow != null) _Rows.Add(NextRow);
      }
      else {
        var NextRow = _Rows[_Rows.Count - 1].NextChar(Char1);
        if (NextRow != null) {
          _Rows.Add(NextRow);
        }
      }
    }

    public Field this[int RowIndex, int ColumnIndex] {
      get => this._Rows[RowIndex][ColumnIndex];
    }



  }


  public class CSVHeaderNotExistException : Exception { }



}
