
namespace CusCSV
{
  using CusCSV.Exceptions;

  using KatKits;

  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Reflection;
  using System.Text;
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
  public class CSVTableAttribute : Attribute
  {
    public const char COMMA = ',';
    public const char QUOTE = '\"';
    public const char RETUEN = '\r';
    public const char LINE_FEED = '\n';

    public string TableName { get; set; } = "default";
    public Type TypeHandle { get; set; }

    internal CSVColumnAttribute[] _Columns;
    internal readonly static Dictionary<Type, CSVTableAttribute[]> __AttributesCache = new Dictionary<Type, CSVTableAttribute[]>();

    public static CSVTableAttribute FetchAttributes<T>(string TableName = "default") => FetchAttributes(typeof(T), TableName);
    public static CSVTableAttribute FetchAttributes(Type Target, string TableName = "default")
    {
      var Type = Target;
      if (__AttributesCache.TryGetValue(Type, out var Tables))
      {
        return Tables.First(E => E.TableName == TableName);
      }
      else
      {
        Tables = Type.GetCustomAttributes<CSVTableAttribute>().ToArray();
        __AttributesCache.Add(Type, Tables);
        var AllCols = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(E => E.CanWrite && E.CanRead).Where(Encoder => Encoder.PropertyType.IsBasicDataType())
          .SelectMany(E =>
          {
            var ColAttrs = E.GetCustomAttributes<CSVColumnAttribute>().ToArray();
            ColAttrs.ForEach(A =>
            {
              A.Property = E;
              A.ColumnName = A.ColumnName ?? E.PropertyType.Name;
            });
            return ColAttrs;
          })
          .ToArray();

        Tables.ForEach(_Table => _Table._Columns = AllCols.Where(E => E.TableName == _Table.TableName).OrderBy(E => E.Order).ToArray());
        return Tables.First(E => E.TableName == TableName);
      }

    }
  }
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
  public class CSVColumnAttribute : Attribute
  {
    public string TableName;
    public string ColumnName;
    public int Order;
    internal bool AllowNull;
    public PropertyInfo Property { get; internal set; }

  }
  //internal class ICSVTypeHandle
  //{
  //  public string TableName;
  //  public Type TaregtType;
  //  public Func<DateTime, string> DateTimeToString;
  //  public Func<DateTimeOffset, string> DateTimeOffsetToString;
  //  public Func<Guid, string> GuidToString;
  //  public Func<TimeSpan, string> TimeSpanToString;
  //}
  public class CSVTable
  {
    public readonly string TableName;

    public readonly LinkedList<CSVColumn> Columns = new LinkedList<CSVColumn>();
    public readonly LinkedList<CSVRow> Rows = new LinkedList<CSVRow>();

    //public CSVTable(string TableName)
    //{
    //  var Attrs = CSVTableAttribute.FetchAttributes<T>(TableName);
    //  Attrs._Columns.ForEach(E => Columns.AddLast(new CSVColumn() { DataType = E.Property.PropertyType, Text = E.ColumnName }));
    //}
    public CSVTable(Type TargetType, string TableName = "default")
    {
      var Attrs = CSVTableAttribute.FetchAttributes(TargetType, TableName);
      Attrs._Columns.ForEach(E => Columns.AddLast(new CSVColumn() { DataType = E.Property.PropertyType, Text = E.ColumnName }));
      this.TableName = TableName;
    }
    public CSVTable()
    {

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1"></param>
    /// <param name="Column"></param>
    /// <param name="Escaped"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need more char,2:create next row,3:done</returns>
    internal int NextChar(int Char1, CSVColumn Column, ref bool Escaped, ref StringBuilder StrBd)
    {
      switch (Rows.Last.Value.NextChar(Char1, Column, ref Escaped, ref StrBd))
      {
        case 1:
          return 1;
        case 2:
          Rows.AddLast(new CSVRow(this));
          Rows.Last.Value.Fields.AddLast(new CSVField(this, Rows.Last.Value, Column));
          return 2;
        case 3:
          return 3;
      }
      return 0;
    }
    public void ParseFromStream(TextReader Reader)
    {
      this.Columns.Clear();
      this.Rows.Clear();
      var StrBd = new StringBuilder();
      var Escaped = true;
      int ColCount = 0;
      this.Columns.AddLast(new CSVColumn());
      var Column = this.Columns.Last;
      this.Rows.AddLast(new CSVRow(this));
      this.Rows.Last.Value.Fields.AddLast(new CSVField(this, Rows.First.Value, this.Columns.First.Value));
      bool HasColumns = false;
      var CharInt = 0;
      while ((CharInt = Reader.Read()) != -1)
      {
        switch (this.NextChar(CharInt, Column.Value, ref Escaped, ref StrBd))
        {
          case 1:
            if (HasColumns)
            {
              Column = Column.Next;
            }
            else
            {
              Columns.AddLast(new CSVColumn());
              Column = Columns.Last;
            }
            ColCount++;
            break;
          case 2:
            Column = this.Columns.First;
            HasColumns = true;
            ColCount = 0;
            break;
        }
      }
      _ = this.NextChar(-1, Column.Value, ref Escaped, ref StrBd);
    }

    public void ParseFromText(string CSVText)
    {
      this.Columns.Clear();
      this.Rows.Clear();
      var StrBd = new StringBuilder();
      var Escaped = true;
      int ColCount = 0;
      this.Columns.AddLast(new CSVColumn());
      var Column = this.Columns.Last;
      this.Rows.AddLast(new CSVRow(this));
      this.Rows.Last.Value.Fields.AddLast(new CSVField(this, Rows.First.Value, this.Columns.First.Value));
      bool HasColumns = false;
      for (int i = 0; i < CSVText.Length; i++)
      {
        switch (this.NextChar((char)CSVText[i], Column.Value, ref Escaped, ref StrBd))
        {
          case 1:
            if (HasColumns)
            {
              Column = Column.Next;
            }
            else
            {
              Columns.AddLast(new CSVColumn());
              Column = Columns.Last;
            }
            ColCount++;
            break;
          case 2:
            Column = this.Columns.First;
            HasColumns = true;
            ColCount = 0;
            break;
        }
      }
      _ = this.NextChar(-1, Column.Value, ref Escaped, ref StrBd);
    }


    public string ToCSVString() =>
      Rows.Count == 0 ? "" : $"{string.Join("\r\n", Rows.Select(E => E.ToCSVString()))}";
    public string ToCSVString(bool WithHeader) =>
      WithHeader ? $"{string.Join(",", Columns)}\r\n{ToString()}" : ToString();
  }
}
