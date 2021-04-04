namespace CSV {
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Diagnostics.CodeAnalysis;
  using System.Linq;
  using System.Reflection;
  using System.Text;

  using static CSV.CSVKits;

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

    internal static object ConvertDynamic(string Value, Type T) {
      if (string.IsNullOrEmpty(Value)) return null;
      TypeConverter TCr = TypeDescriptor.GetConverter(T);
      if (TCr.CanConvertFrom(Value.GetType()))
        return TCr.ConvertFromString(Value);
      else
        return Convert.ChangeType(Value, T);
    }


    public static string ToCSV(object Instance, string Table = CSVTableAttribute.DefaultTableName) {
      return null;
    }
    public static object FromCSV(string CSVText, string Table = CSVTableAttribute.DefaultTableName) {

      return new Table();
    }
    public static IEnumerable<object> FromCSV(string CSVText, Type Model, string Table = CSVTableAttribute.DefaultTableName) {

      return Enumerable.Empty<object>();
    }
    public static IEnumerable<T> FromCSV<T>(string CSVText, string Table = CSVTableAttribute.DefaultTableName) {
      return FromCSV(CSVText, typeof(T), Table).Cast<T>();
    }



  }

  internal static class CSVKits {
    internal static class MemberTypeValidor {
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
    internal static IOrderedEnumerable<CSVColumnAttribute> AllValidMember(this Type This, string Table)
      => This.GetFields(BindingFlags.Public | BindingFlags.Instance)
           .Cast<MemberInfo>()
           .Concat(This.GetProperties(BindingFlags.Public | BindingFlags.Instance))
           .Select(E => (E, E.GetCustomAttributes<CSVColumnAttribute>().FirstOrDefault(E => E.TableName == Table)))
           .Where(E => E.Item2 != null && MemberTypeValidor.Validor(E.E))
           .Select(E => {
             E.Item2.Member = E.E;
             if (string.IsNullOrEmpty(E.Item2.HeaderText)) E.Item2.HeaderText = E.E.Name;
             return E.Item2;
           })
           .OrderBy(E => E.Order);

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

    public object ToObject(Type Model) {

      return null;
    }

  }
  class Header : Row {

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
        _ColAttrs = ModelType.AllValidMember(TargetTable).ToList();

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
