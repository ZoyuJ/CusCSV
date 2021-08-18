namespace CusCSV
{
  using CusCSV.Exceptions;

  using KatKits;

  using System;
  using System.Text;

  public class CSVField
  {
    public CSVField(CSVTable Table, CSVRow Row, CSVColumn Column)
    {
      this.Table = Table;
      this.Row = Row;
      this.Column = Column;
    }
    public readonly CSVTable Table;
    public readonly CSVRow Row;
    public readonly CSVColumn Column;

    public string Text { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1"></param>
    /// <param name="Enclosed"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need nore char,1:create new field,2:create new line</returns>
    internal int NextChar(char Char1, ref bool Enclosed, ref StringBuilder StrBd)
    {
      if (Char1 == '\"')
      {
        Enclosed = !Enclosed;
      }
      if (Enclosed)
      {
        if (Char1 == ',')
        {
          //next field
          Text = StrBd.ToString();
          StrBd.Clear();
          return 1;
        }
        else if (Char1 == '\r' && StrBd.Length > 0 && StrBd[StrBd.Length - 1] == '\n')
        {
          //next line
          StrBd.Remove(StrBd.Length - 1, 1);
          Text = StrBd.ToString();
          StrBd.Clear();
          return 2;
        }
      }

      if (!(Char1 == '\"' && (StrBd.Length > 0 && StrBd[StrBd.Length - 1] == '\"')))
        //normal char
        StrBd.Append(Char1);

      return 0;
    }
    public void Set(object Obj)
    {
      if (Obj.GetType().IsBasicDataType())
      {

      }
    }
    public object Get()
    {
      if (Column.DataType == null) throw new CSVFieldWithUnknowDataTypeException(Column.Text, Text);
      if (Column.DataType.Equals(typeof(string))) return Text;
      if (string.IsNullOrEmpty(Text)) return null;
      Type Target = Column.DataType;
      if (Column.DataType.IsNullableType())
      {
        Target = Nullable.GetUnderlyingType(Column.DataType);
      }
      else if (Column.DataType.IsEnum)
      {
        return Enum.Parse(Column.DataType, Text);
      }
      if (Target.IsPrimitive)
      {
        return Convert.ChangeType(Text, Target);
      }
      else
      {
        if (Target.Equals(typeof(decimal)))
          return decimal.Parse(Text);
        else if (Target.Equals(typeof(DateTime)))
          return DateTime.Parse(Text);
        else if (Target.Equals(typeof(Guid)))
          return Guid.Parse(Text);
        else if (Target.Equals(typeof(TimeSpan)))
          return TimeSpan.Parse(Text);
        else if (Target.Equals(typeof(DateTimeOffset)))
          return DateTimeOffset.Parse(Text);
      }
      return null;
    }
    public T GetValue<T>()
    {
      return (T)Get();
    }

    public override string ToString() => Text;
    public string ToCSVString()
    {
      StringBuilder StrBd = new StringBuilder(Text);
      bool HasToEnclosed = false;
      for (int i = StrBd.Length - 1; i >= 0; i--)
      {
        if (StrBd[i] == '\"')
        {
          StrBd.Insert(i, '\"');
        }
        else
        {
          if (!HasToEnclosed
              && (
                StrBd[i] == ','
                || (StrBd[i] == '\r' && i > 0 && StrBd[i - 1] == '\n')
              )
            )
            HasToEnclosed = true;
        }
      }
      if (HasToEnclosed)
      {
        StrBd.Insert(0, '\"');
        StrBd.Append('\"');
      }
      return StrBd.ToString();
    }
  }


}
