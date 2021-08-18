
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
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1"></param>
    /// <param name="Enclosed"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need more char,2:create next row</returns>
    internal int NextChar(char Char1, CSVColumn Column, ref bool Enclosed, ref StringBuilder StrBd)
    {
      switch (Rows.Last.Value.NextChar(Char1, Column, ref Enclosed, ref StrBd))
      {
        case 1:
          return 1;
        case 2:
          Rows.AddLast(new CSVRow(this));
          return 2;
      }
      return 0;
    }
    public void ReadFromStream(TextReader Reader)
    {
      var StrBd = new StringBuilder();
      var Enclosed = false;
      char Char1;
      int Next = 0;
      var Column = this.Columns.First;
      int ColCount = 0;
      while ((Char1 = (char)Reader.Read()) != -1)
      {
        Next = this.NextChar((char)Char1, Column.Value, ref Enclosed, ref StrBd);
        if (Next == 1)
        {
          Column = Column.Next;
          if (Column == null)
            throw new ColumnOutOfRangeException(ColCount, this.Columns.Count);
          ColCount++;
        }
        else if (Next == 2)
        {
          Column = this.Columns.First;
          ColCount = 0;
        }
      }
      if (Next == 0)
      {
        throw new DamagedCSVFileException(Rows.Count);
      }
      if (Next == 2)
      {
        this.Rows.RemoveLast();
      }


    }
    public IEnumerable<CSVRow> ReadRowFromStream(TextReader Reader)
    {
      var StrBd = new StringBuilder();
      var Enclosed = false;
      char Char1;
      int Next = 0;
      int ColCount = 0;
      var Column = this.Columns.First;
      while ((Char1 = (char)Reader.Read()) != -1)
      {
        Next = this.NextChar((char)Char1, Column.Value, ref Enclosed, ref StrBd);
        if (Next == 1)
        {
          Column = Column.Next;
          if (Column == null)
            throw new ColumnOutOfRangeException(ColCount, this.Columns.Count);
          ColCount++;
        }
        else if (Next == 2)
        {
          Column = this.Columns.First;
          yield return Rows.Last.Previous.Value;
          ColCount = 0;
        }
      }
      if (Next == 0)
      {
        throw new DamagedCSVFileException(Rows.Count);
      }
      if (Next == 2)
      {
        this.Rows.RemoveLast();
      }


    }



    public override string ToString() =>
  $"{string.Join("\n\r", Rows)}";
    public string ToString(bool WithHeader) =>
      WithHeader ? $"{string.Join(",", Columns)}\n\r{ToString()}" : ToString();
  }
}
