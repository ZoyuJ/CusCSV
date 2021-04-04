namespace CSV {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Text;

#if WITHOUT_GENERATOR
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
  public class CSVColumnAttribute : Attribute, IComparable<CSVColumnAttribute> {
    public string HeaderText { get; set; }
    public int Order { get; set; }
    public string TableName { get; set; } = CSVTableAttribute.DefaultTableName;
    public CSVColumnAttribute(string Header) { HeaderText = Header; }

    public int CompareTo(CSVColumnAttribute other) {
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
#endif

  public partial class CSVConvert {


    internal static object ConvertDynamic(string Value, Type T) {
      if (string.IsNullOrEmpty(Value)) return null;
      TypeConverter TCr = TypeDescriptor.GetConverter(T);
      if (TCr.CanConvertFrom(Value.GetType()))
        return TCr.ConvertFromString(Value);
      else
        return Convert.ChangeType(Value, T);
    }

    public static Table ToCSV(string CSVText) {
      var Tb = new Table();
      int I = 0;
      while (CSVText.Length > I) {
        Tb.NextChar(CSVText[I]);
        I++;
      }
      return Tb;
    }
    public static Table ToCSV(Stream CSVText, Encoding Encoder) {
      return ToCSV(new StreamReader(CSVText, Encoder));
    }
    public static Table ToCSV(StreamReader CSVText) {
      var Tb = new Table();
      var Line = CSVText.ReadLine();
      while (Line != null) {
        for (int i = 0; i < Line.Length; i++) {
          Tb.NextChar(Line[i]);
        }
      }
      return Tb;
    }



  }
  public class Field {
    private StringBuilder _Chars;
    public int TextCount { get; private set; }
    public int TextOffset { get; private set; }
    private bool Enclosed { get; set; } = true;

    public bool MustEnclosed { get; private set; } = false;

    public Field() { TextOffset = 0; TextCount = -1; _Chars = new StringBuilder(); }
    public Field(int Offset) : this() { this.TextOffset = Offset; }
    internal Field(string ValueText) {
      SetTextValue(ValueText);
    }


    public Field NextChar(char Char1, out bool Eof) {
      Eof = false;
      TextCount += 1;
      if (Char1 == '\"') {
        Enclosed = !Enclosed;
      }
      if (Enclosed) {
        if (Char1 == ',') {
          return new Field(TextOffset + TextCount);
        }
        else if (Char1 == '\r' && _Chars.Length > 0 && _Chars[_Chars.Length - 1] == '\n') {
          Eof = true;
          _Chars.Remove(_Chars.Length - 1, 1);
          return null;
        }
      }

      if (!(Char1 == '\"' && (_Chars.Length > 0 && _Chars[_Chars.Length - 1] == '\"'))) _Chars.Append(Char1);

      return null;
    }
    public void SetTextValue(string ValueString) {
      _Chars = new StringBuilder(ValueString);

      for (int i = _Chars.Length - 1; i >= 0; i--) {
        if (_Chars[i] == '\"') {
          _Chars.Insert(i, '\"');
        }
        else if (!MustEnclosed && (_Chars[i] == ',' || (_Chars[i] == '\r' && i > 0 && _Chars[i - 1] == '\n'))) MustEnclosed = true;
      }
      if (MustEnclosed) {
        _Chars.Insert(0, '\"');
        _Chars.Append('\"');
      }
    }
    public void SetValue(object Value) {
      switch (Value) {
        case DateTime dt:
          SetTextValue(dt.ToString());
          break;
        default:
          SetTextValue(Value.ToString());
          break;
      }
    }
    private string _Raw;
    public string Raw {
      get {
        if (_Raw == null) _Raw = _Chars.ToString();
        return _Raw;
      }
    }
    public override string ToString() => Raw.Trim('\"');
  }
  public class Row : IEnumerable<Field> {
    protected readonly List<Field> _Fields;
    internal int TextCount { get => _Fields.Where(E => E.TextCount > 0).Sum(E => E.TextCount); }
    internal int TextOffset { get => _Fields[0].TextOffset; }
    public Row() { _Fields = new List<Field>() { new Field(0) }; }
    public Row(int Offset) { _Fields = new List<Field>() { new Field(Offset) }; }
    public Row(IEnumerable<Field> Fields) {
      _Fields = Fields.ToList();
    }
    public Row NextChar(char Char1) {
      var NextField = _Fields[_Fields.Count - 1].NextChar(Char1, out var Eof);
      if (Eof) {
        return new Row(TextOffset + TextCount);
      }
      else if (NextField != null) {
        _Fields.Add(NextField);
      }
      return null;
    }
    public Field this[int ColumnIndex] {
      get => _Fields[ColumnIndex];
    }

    public void AppendColumn(string ValueText) => _Fields.Add(new Field(ValueText));
    public void RemoveColumn(int Index) => _Fields.RemoveAt(Index);
    public void InsertColumn(int Index, string ValueText) => _Fields.Insert(Index, new Field(ValueText));


    public override string ToString() =>
      $"{string.Join(",", _Fields)}";

    public IEnumerator<Field> GetEnumerator() => _Fields.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _Fields.GetEnumerator();
  }
  public class Header : Row {
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
    //internal List<CSVColumnAttribute> _ColAttrs;
    //public void MapToModel(Type ModelType, string TargetTable = CSVTableAttribute.DefaultTableName) {
    //  if (MemberTypeValidor.IsBasicType(ModelType)) {
    //    //no header
    //  }
    //  else if (ModelType.IsArray || typeof(IList).IsAssignableFrom(ModelType)) {
    //    //no header
    //  }
    //  else if (typeof(IDictionary).IsAssignableFrom(ModelType)) {
    //    //no key header
    //    MapToModel(ModelType.GetGenericArguments()[1]);
    //  }
    //  else {
    //    _ColAttrs = ModelType.GetFields(BindingFlags.Public | BindingFlags.Instance)
    //      .Cast<MemberInfo>()
    //      .Concat(ModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    //      .Select(E => (E, E.GetCustomAttributes<CSVColumnAttribute>().FirstOrDefault(EE => EE.TableName == TargetTable)))
    //      .Where(E => E.Item2 != null && MemberTypeValidor.Validor(E.E))
    //      .Select(E => {
    //        E.Item2.Member = E.E;
    //        if (string.IsNullOrEmpty(E.Item2.HeaderText)) E.Item2.HeaderText = E.E.Name;
    //        return E.Item2;
    //      })
    //      .OrderBy(E => E.Order)
    //      .ToList();

    //    if (_Fields != null && _Fields.Count > 0) {
    //      for (int i = _Fields[0].Raw == "" ? 1 : 0, j = 0; i < _Fields.Count; i++) {
    //        if (_Fields[i].Raw == "") {
    //          _ColAttrs.Insert(j, _ColAttrs[j - 1]);
    //        }
    //      }
    //    }
    //  }

    //}



  }
  public class Table : IEnumerable<Row> {
    public Header Header { get; protected set; }
    private readonly List<Row> _Rows;
    public bool HasHeader { get; protected set; }

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

    public void AddColumn(string HeaderText) {
      if (HasHeader) {
        Header.AppendColumn(HeaderText);
      }
      _Rows.ForEach(E => E.AppendColumn(""));
    }
    public void RemoveColumn(int Index) {
      if (HasHeader) {
        Header.RemoveColumn(Index);
      }
      _Rows.ForEach(E => E.RemoveColumn(Index));
    }
    public void InsertColumn(int Index, string HeaderText) {
      if (HasHeader) {
        Header.InsertColumn(Index, HeaderText);
      }
      _Rows.ForEach(E => E.InsertColumn(Index, ""));
    }

    public override string ToString() =>
      $"{string.Join("\n\r", _Rows)}";
    public string ToString(bool WithHeader) =>
      WithHeader ? $"{Header}\n\r{ToString()}" : ToString();

    public IEnumerator<Row> GetEnumerator() => _Rows.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _Rows.GetEnumerator();
  }

  public class CSVTableSetting {
    public string DateTimeFormate = "yyyy/MM/ddTHH:mm:ssZ";
    public bool MustEnclosed = false;
  }

}