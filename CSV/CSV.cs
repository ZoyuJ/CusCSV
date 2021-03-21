using System;
using System.Collections.Generic;
using System.Text;

namespace CSV {
  using System;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Text;


  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVColumnAttribute : Attribute, IComparable<CSVColumnAttribute> {
    public string HeaderText { get; set; }
    public int Order { get; set; }
    public string TableName { get; set; } = "default";
    public CSVColumnAttribute(string Header) { HeaderText = Header; }

    public int CompareTo([AllowNull] CSVColumnAttribute other) {
      return other == null ? 0 : Order.CompareTo(other.Order);
    }
  }
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
  public class CSVTableAttribute : Attribute {
    public string Name { get; set; } = "default";
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

    public static string ToCSV<T>(IEnumerable<T> Ins, string Name = "default", in bool WriteHeader = true, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      StringBuilder CSV = new StringBuilder();
      ToCSV(Ins, ref CSV, Name, WriteHeader, SerializFuncs);
      return CSV.ToString();
    }
    public static void ToCSV<T>(IEnumerable<T> Ins, ref StringBuilder CSVBuilder, string Name = "default", in bool WriteHeader = true, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
      if (Attr != null) {
        ToHeadersRow<T>(ref CSVBuilder, Attr, out var Map, out var Sorted, WriteHeader, false);
        foreach (var item in Ins) {
          CSVBuilder.AppendLine();
          ToCSVRowLv1(item, ref CSVBuilder, in Attr, Map, Sorted, SerializFuncs: SerializFuncs);
        }
        return;
      }
      throw new InvalidOperationException("No CSV Fig");

    }
    public static void ToCSVHeader<T>(ref StringBuilder CSVBuilder, string Name = "default", in bool AppendNewLine = false) where T : new() {
      var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
      ToHeadersRow<T>(ref CSVBuilder, Attr, out _, out _, true, AppendNewLine);
    }
    static void ToHeadersRow<T>(ref StringBuilder SB, CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted, in bool WriteHeader = true, in bool AppendNewLine = false) where T : new() {
      CollectHeaders<T>(Table, out Map, out Sorted);
      if (WriteHeader && SB != null) {
        for (int i = 0; i < Sorted.Count; i++) {
          var HeaderText = Sorted[i].HeaderText ?? Map[Sorted[i]].Name;
          for (int j = 0; j < HeaderText.Length; j++) {
            if (HeaderText[j] == CSVTableAttribute.Separator) SB.Append('\\');
            SB.Append(HeaderText[j]);
          }
          SB.Append(CSVTableAttribute.Separator);
        }
        SB.Remove(SB.Length - 1, 1);
        if (AppendNewLine) SB.AppendLine();
      }
    }
    static string ToHeadersRowString(CSVTableAttribute Table, in IReadOnlyList<CSVColumnAttribute> Sorted) => string.Join(CSVTableAttribute.Separator, Sorted.Select(E => E.HeaderText));
    static void ToCSVRowLv1<T>(T Ins, ref StringBuilder SB, in CSVTableAttribute Table, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted, in bool AppendNewLine = false, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      for (int i = 0; i < Sorted.Count; i++) {
        switch (Map[Sorted[i]]) {
          case PropertyInfo Prop:
            bool HasNotCustomizSerialFuncP = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText));
            var PContent = HasNotCustomizSerialFuncP ? (Prop.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Prop.GetValue(Ins) ?? "");
            if (!HasNotCustomizSerialFuncP) {
              SB.Append(PContent);
            }
            else {
              for (int j = 0; j < PContent.Length; j++) {
                if (PContent[j] == CSVTableAttribute.Separator) SB.Append('\\');
                SB.Append(PContent[j]);
              }
            }
            SB.Append(CSVTableAttribute.Separator);
            break;
          case FieldInfo Field:
            bool HasNotCustomizSerialFuncF = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText));
            var FContent = HasNotCustomizSerialFuncF ? (Field.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Field.GetValue(Ins) ?? "");
            if (!HasNotCustomizSerialFuncF) {
              SB.Append(FContent);
            }
            else {
              for (int j = 0; j < FContent.Length; j++) {
                if (FContent[j] == CSVTableAttribute.Separator) SB.Append('\\');
                SB.Append(FContent[j]);
              }
            }
            SB.Append(CSVTableAttribute.Separator);
            break;
        }
      }
      SB.Remove(SB.Length - 1, 1);
      if (AppendNewLine) SB.AppendLine();
    }

    static readonly Dictionary<Type, Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>> ReflactCache = new Dictionary<Type, Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>>();
    static bool GetHeaderMapCache<T>(in CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
      if (ReflactCache.TryGetValue(typeof(T), out var D)) {
        if (D.TryGetValue(Table.Name, out var Is)) {
          Map = Is.Item1;
          Sorted = Is.Item2;
          return true;
        }
      }
      Map = null;
      Sorted = null;
      return false;
    }
    static void SetHeaderMapCache<T>(in CSVTableAttribute Table, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
      if (ReflactCache.TryGetValue(typeof(T), out var D)) {
        if (D.TryGetValue(Table.Name, out var Is)) {
          Is.Item1 = Map;
          Is.Item2 = Sorted;
        }
        else {
          D[Table.Name] = (Map, Sorted);
        }
      }
      else {
        D = new Dictionary<string, (IReadOnlyDictionary<CSVColumnAttribute, MemberInfo>, IReadOnlyList<CSVColumnAttribute>)>();
        D.Add(Table.Name, (Map, Sorted));
        ReflactCache[typeof(T)] = D;
      }
    }
    static void CollectHeaders<T>(CSVTableAttribute Table, out IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, out IReadOnlyList<CSVColumnAttribute> Sorted) where T : new() {
      if (GetHeaderMapCache<T>(Table, out Map, out Sorted)) return;
      var _Map = new Dictionary<CSVColumnAttribute, MemberInfo>();
      var _Sorted = new List<CSVColumnAttribute>();
      var AllMembers = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).ToArray();
      for (int i = 0; i < AllMembers.Length; i++) {
        var ColAttrs = AllMembers[i].GetCustomAttributes<CSVColumnAttribute>(false);
        var ColAttr = ColAttrs.Where(EE => EE.TableName == Table.Name).FirstOrDefault();
        if (ColAttr != null) {
          _Map[ColAttr] = AllMembers[i];
          _Sorted.Add(ColAttr);
        }
      }
      _Sorted.Sort();
      Map = _Map;
      Sorted = _Sorted;
      SetHeaderMapCache<T>(Table, Map, Sorted);
    }
    public static void ClearReflectCache() { ReflactCache.Clear(); }

    [Obsolete("Not Support", true)]
    static IEnumerable<string> EveryLine(string Data, string LineSpread) {
      int StartIndex = 0;
      int Len = 0;
      int LineSpreadLen = LineSpread.Length;
      if (LineSpreadLen == 1) {
        char _LineSpread = LineSpread[0];
        for (int i = 0; i < Data.Length; i++) {
          if (Data[i] == _LineSpread) {
            Len = i - StartIndex;
            yield return Data.Substring(StartIndex, Len);
            StartIndex = i + 1;
          }
        }
      }
      else {
        char _LineSpread = LineSpread[0];
        for (int i = 0; i < Data.Length; i++) {
          if (Data[i] == _LineSpread) {
            Len = i - StartIndex;
            yield return Data.Substring(StartIndex, Len);
            StartIndex = i + LineSpreadLen;
          }
        }
      }
    }
    static IEnumerable<string> EveryLine(string Data) {
      return EveryLine(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Data))));
    }
    static IEnumerable<string> EveryLine(TextReader Data) {
      string Line = Data.ReadLine();
      while (Line != null) {
        yield return Line;
        Line = Data.ReadLine();
      }
      yield break;
    }




    public static IEnumerable<T> FromCSV<T>(string Data, string Name = "default") where T : new() {
      var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
      if (Attr == null) throw new InvalidOperationException("No CSV Fig");
      return FromCSV<T>(Attr, EveryLine(Data), Name);
    }
    public static IEnumerable<T> FromCSV<T>(TextReader Data, string Name = "default") where T : new() {
      var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
      if (Attr == null) throw new InvalidOperationException("No CSV Fig");
      return FromCSV<T>(Attr, EveryLine(Data), Name);
    }
    static IEnumerable<T> FromCSV<T>(CSVTableAttribute Table, IEnumerable<string> Data, string Name = "default") where T : new() {
      CollectHeaders<T>(Table, out var Map, out var Sorted);
      var DataItor = Data.GetEnumerator();
      //DataItor.Reset();
      if (MoveToHeader(ToHeadersRowString(Table, Sorted), DataItor)) {
        while (DataItor.MoveNext())
          yield return FromCSVRow<T>(EveryItem(DataItor.Current, Table).ToArray(), Map, Sorted);
        DataItor.Dispose();
        yield break;
      }
      else {
        throw new CSVHeaderNotExistException();
      }
    }
    static bool MoveToHeader(in string HeaderRowStr, IEnumerator<string> DataItor) {
      while (DataItor.MoveNext()) {
        if (DataItor.Current.Trim(' ', '\n', '\r') == HeaderRowStr) return true;
      }
      return false;
    }
    static IEnumerable<string> EveryItem(string Data, CSVTableAttribute Table) {
      int StartIndex = 0;
      StringBuilder CacheBackSlash = new StringBuilder();
      for (int i = 0; i < Data.Length; i++) {
        if (Data[i] == ',') {
          var RawItem = Data.Substring(StartIndex, i - StartIndex);
          StartIndex = i + 1;
          if (RawItem.Length != 0) {
            CacheBackSlash.Append(RawItem);
            if (CacheBackSlash[CacheBackSlash.Length - 1] == '\\') {
              CacheBackSlash[CacheBackSlash.Length - 1] = Table.Separator;
              continue;
            }
          }
          yield return CacheBackSlash.ToString();
          CacheBackSlash.Clear();
        }
      }
      int LastLen = Data.Length - StartIndex;
      yield return LastLen <= 0 ? "" : Data.Substring(StartIndex, LastLen);
    }
    static T FromCSVRow<T>(string[] RowContents, in IReadOnlyDictionary<CSVColumnAttribute, MemberInfo> Map, in IReadOnlyList<CSVColumnAttribute> Sorted, in IReadOnlyDictionary<string, Func<string, object>> DeserializFuncs = null) where T : new() {
      var Data = new T();
      for (int i = 0; i < Sorted.Count; i++) {
        if (!string.IsNullOrEmpty(RowContents[i])) {
          if (Map.TryGetValue(Sorted[i], out var Member)) {
            switch (Member) {
              case PropertyInfo Prop:
                Prop.SetValue(Data, (DeserializFuncs == null || !DeserializFuncs.ContainsKey(Sorted[i].HeaderText)) ? ConvertDynamic(RowContents[i], Prop.PropertyType) : DeserializFuncs[Sorted[i].HeaderText](RowContents[i]));
                break;
              case FieldInfo Field:
                Field.SetValue(Data, (DeserializFuncs == null || !DeserializFuncs.ContainsKey(Sorted[i].HeaderText)) ? ConvertDynamic(RowContents[i], Field.FieldType) : DeserializFuncs[Sorted[i].HeaderText](RowContents[i]));
                break;
            }
          }
        }
      }
      return Data;
    }
    static object ConvertDynamic(string Value, Type T) {
      if (string.IsNullOrEmpty(Value)) return null;
      TypeConverter TCr = TypeDescriptor.GetConverter(T);
      if (TCr.CanConvertFrom(Value.GetType()))
        return TCr.ConvertFromString(Value);
      else
        return Convert.ChangeType(Value, T);
    }


  }

  public partial class CSVConvert {

  }

  //internal class CSVStringBuilder {
  //  public CSVStringBuilder(CSVTableAttribute TableAttr) {
  //    _TableAttr = TableAttr;
  //    _StringBuilder = new StringBuilder();

  //  }
  //  private readonly StringBuilder _StringBuilder;
  //  private readonly CSVTableAttribute _TableAttr;

  //  public void AppendChar(char Char) {
  //    if (Char == _TableAttr.Separator) {
  //      _StringBuilder.Append('\\');
  //    }
  //    _StringBuilder.Append(Char);
  //  }
  //  public void NewLine() {
  //    _StringBuilder.Append(_TableAttr.NewLine);
  //  }
  //  public void AppendLine(string Line) {
  //    foreach (var Char in Line) {
  //      AppendChar(Char);
  //    }
  //    NewLine();
  //  }

  //  public static implicit operator StringBuilder(CSVStringBuilder CSVB) => CSVB._StringBuilder;

  //}

  public class CSVHeaderNotExistException : Exception { }



}
