using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace CSV2 {
  using CSVColumnMap = System.Collections.Generic.Dictionary<CSV2.CSVEntryAttribute, System.Reflection.MemberInfo>;
  using CSVColumnList = System.Collections.Generic.List<CSV2.CSVEntryAttribute>;
  using CSVColumnSortedMap = System.Tuple<System.Collections.Generic.IReadOnlyDictionary<CSV2.CSVEntryAttribute, System.Reflection.MemberInfo>, System.Collections.Generic.IReadOnlyList<CSV2.CSVEntryAttribute>>;
  public abstract class CSVEntryAttribute : Attribute, IComparable<CSVEntryAttribute> {
    public string HeaderText { get; set; }
    public int Order { get; set; }
    public string TableName { get; set; } = "default";
    public CSVEntryAttribute(string Header) { HeaderText = Header; }

    public int CompareTo([AllowNull] CSVEntryAttribute other) {
      return other == null ? 0 : Order.CompareTo(other.Order);
    }
  }
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVEntryCollectionAttribute : CSVEntryAttribute {
    public CSVEntryCollectionAttribute(string PrependHeaderText) : base(PrependHeaderText) { }
    public string AttachTableName { get; set; } = "default";

  }
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVEnmerableEntriesAttribute : CSVEntryAttribute {
    public string PrependText { get; set; }
    public string AttachTableName { get; set; } = "default";
    public CSVEnmerableEntriesAttribute(string HeaderText) : base(HeaderText) { }
  }
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVSimpleEntryAttribute : CSVEntryAttribute {
    public bool EveryItemAppendIndex = false;
    public bool EveryItemPrependHeaderText = false;
    public int IndexInHeaderTextStartAt = 1;
    public CSVSimpleEntryAttribute(string HeaderText) : base(HeaderText) { }
  }
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
  public class CSVTableAttribute : Attribute {
    public string Name { get; set; } = "default";
    public char JoinChar { get; set; } = ',';
    public bool IsHorizatal { get; set; } = true;
  }

  public class CSVEntryHeader {
    public string HeaderText { get; set; }
    public readonly CSVEntryAttribute EntryInfo;
    public CSVEntryHeader NextEntry { get; set; }
    public CSVEntryHeader PerviousEntry { get; set; }
    public readonly List<object> Datas = new List<object>();

    public readonly Func<object, object> GetValue;
    public readonly Action<object, object> SetValue;

    public void InsertNext(CSVEntryHeader Header) {
      var __Next = Header.NextEntry;
      while (__Next != null) {
        __Next = __Next.NextEntry;
      }
      __Next.NextEntry = NextEntry;
      NextEntry = Header;
    }
    public void InsertPervious(CSVEntryHeader Header) {
      var __Pervious = Header.PerviousEntry;
      while (__Pervious != null) {
        __Pervious = __Pervious.PerviousEntry;
      }
      __Pervious.PerviousEntry = PerviousEntry;
      Header.NextEntry = this;
    }



  }




  public partial class CSVConvert {

    public static void CollectHeaders(Type TargetType, string TableName, out CSVEntryHeader FirstHeader) {
      FirstHeader = new CSVEntryHeader();

    }

    static void FindAttributesInType(Type TargetType, string TableName, out List<AttributeAndMember> Members) {

      Members = new List<AttributeAndMember>();
      var AllMembers = TargetType.GetMembers(BindingFlags.Instance | BindingFlags.Public).ToArray();
      for (int i = 0; i < AllMembers.Length; i++) {
        var Attr = AllMembers[i].GetCustomAttributes<CSVEntryAttribute>(false).Where(E => E.TableName == TableName).FirstOrDefault();
        if (Attr != null) {
          Members.Add(new AttributeAndMember(Attr, AllMembers[i]));
        }
      }
    }

  }

  public class AttributeAndMember : IComparable<AttributeAndMember> {
    public readonly CSVEntryAttribute Attribute;
    public readonly MemberInfo Member;
    public AttributeAndMember(CSVEntryAttribute Atribute, MemberInfo Member) {
      this.Attribute = Attribute;
      this.Member = Member;
    }

    public int CompareTo([AllowNull] AttributeAndMember other) {
      return Attribute.CompareTo(other.Attribute);
    }
  }

  public class CSVHeaderCollection<T> {
    public void CollectHeaders<T>(in string TableName, ref CSVColumnMap Map, ref CSVColumnList Sorted) {
      CollectHeaders(typeof(T), TableName, ref Map, ref Sorted);
    }
    public void CollectHeaders(Type MemberType, in string TableName, ref CSVColumnMap Map, ref CSVColumnList Sorted) {
      var _Map = new Dictionary<CSVEntryAttribute, MemberInfo>();
      var _Sorted = new List<CSVEntryAttribute>();
      var AllMembers = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(E => E.MemberType == MemberTypes.Field || E.MemberType == MemberTypes.Property).ToArray();
      for (int i = 0; i < AllMembers.Length; i++) {

      }
    }

  }


  public partial class CSVConvert {
    public static string ToCSV<T>(IEnumerable<T> Ins, string Name = "default", in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      StringBuilder CSV = new StringBuilder();
      ToCSV(Ins, ref CSV, Name, SerializFuncs);
      return CSV.ToString();
    }
    public static void ToCSV<T>(IEnumerable<T> Ins, ref StringBuilder CSVBuilder, string Name = "default", in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      var Attr = typeof(T).GetCustomAttributes<CSVTableAttribute>(false).Where(E => E.Name == Name).FirstOrDefault();
      if (Attr != null) {
        CSVColumnMap Map = new Dictionary<CSVEntryAttribute, MemberInfo>() as CSVColumnMap;
        CSVColumnList Sorted = new List<CSVEntryAttribute>() as CSVColumnList;
        ToHeadersRow<T>(ref CSVBuilder, Attr, ref Map, ref Sorted);
        foreach (var item in Ins) {
          CSVBuilder.AppendLine();
          ToCSVRowLv1(item, ref CSVBuilder, in Attr, Map, Sorted, SerializFuncs: SerializFuncs);
        }
        return;
      }
      throw new InvalidOperationException("No CSV Fig");

    }
    static void ToHeadersRow<T>(ref StringBuilder SB, CSVTableAttribute Table, ref CSVColumnMap Map, ref CSVColumnList Sorted, in bool AppendNewLine = false) where T : new() {
      CollectHeaders<T>(Table, ref Map, ref Sorted);
      if (SB != null) {
        for (int i = 0; i < Sorted.Count; i++) {
          var HeaderText = Sorted[i].HeaderText ?? Map[Sorted[i]].Name;
          for (int j = 0; j < HeaderText.Length; j++) {
            if (HeaderText[j] == Table.JoinChar) SB.Append('\\');
            SB.Append(HeaderText[j]);
          }
          SB.Append(Table.JoinChar);
        }
        SB.Remove(SB.Length - 1, 1);
        if (AppendNewLine) SB.AppendLine();
      }
    }
    static string ToHeadersRowString(CSVTableAttribute Table, in CSVColumnList Sorted) => string.Join(Table.JoinChar, Sorted.Select(E => E.HeaderText));
    static void ToCSVRowLv1<T>(T Ins, ref StringBuilder SB, in CSVTableAttribute Table, in CSVColumnMap Map, in CSVColumnList Sorted, in bool AppendNewLine = false, in IReadOnlyDictionary<string, Func<object, string>> SerializFuncs = null) where T : new() {
      for (int i = 0; i < Sorted.Count; i++) {
        switch (Map[Sorted[i]]) {
          case PropertyInfo Prop:

            var PContent = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText)) ? (Prop.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Prop.GetValue(Ins) ?? "");
            for (int j = 0; j < PContent.Length; j++) {
              if (PContent[j] == Table.JoinChar) SB.Append('\\');
              SB.Append(PContent[j]);
            }
            SB.Append(Table.JoinChar);
            break;
          case FieldInfo Field:
            var FContent = (SerializFuncs == null || !SerializFuncs.ContainsKey(Sorted[i].HeaderText)) ? (Field.GetValue(Ins) ?? "").ToString() : SerializFuncs[Sorted[i].HeaderText](Field.GetValue(Ins) ?? "");
            for (int j = 0; j < FContent.Length; j++) {
              if (FContent[j] == Table.JoinChar) SB.Append('\\');
              SB.Append(FContent[j]);
            }
            SB.Append(Table.JoinChar);
            break;
          default:
            break;
        }
      }
      SB.Remove(SB.Length - 1, 1);
      if (AppendNewLine) SB.AppendLine();
    }



  }
  public partial class CSVConvert {
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
      CSVColumnMap Map = new Dictionary<CSVEntryAttribute, MemberInfo>() as CSVColumnMap;
      CSVColumnList Sorted = new List<CSVEntryAttribute>() as CSVColumnList;
      CollectHeaders<T>(Table, ref Map, ref Sorted);
      var DataItor = Data.GetEnumerator();
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
              CacheBackSlash[CacheBackSlash.Length - 1] = Table.JoinChar;
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
    static T FromCSVRow<T>(string[] RowContents, in CSVColumnMap Map, in CSVColumnList Sorted, in IReadOnlyDictionary<string, Func<string, object>> DeserializFuncs = null) where T : new() {
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

  //public class CSVPazzle {
  //  public List<string> Header { get; set; }
  //  public List<List<string>> Columns { get; set; }

  //  public void InsertColumn(string HeaderText, int Index) {
  //    Header.Insert(Index, HeaderText);
  //    Rows.ForEach(E => {
  //      E.Insert(Index, "");
  //    });
  //  }




  //}
  public class CSVPazzleNode {
    public CSVPazzleNode Left { get; set; }
    public CSVPazzleNode Right { get; set; }
    public CSVPazzleNode Top { get; set; }
    public CSVPazzleNode Bottom { get; set; }
  }

  public class CSVColumnTarget : IComparable<CSVColumnTarget> {
    public CSVEntryAttribute ColumnInfo { get; set; }
    public MemberInfo Member { get; set; }

    public int CompareTo(CSVColumnTarget other) {
      return other.ColumnInfo.CompareTo(ColumnInfo);
    }
  }

  public class CSVConverter<T1, T2> : Dictionary<Type, Dictionary<string, IReadOnlyList<CSVColumnTarget>>> {
    public CSVConverter() {

    }


  }

  public class CSVHeaderNotExistException : Exception { }

}
