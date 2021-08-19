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
    /// <param name="Escaped"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need nore char,1:create new field,2:create new line</returns>
    internal int NextChar(int Char1, ref bool Escaped, ref StringBuilder StrBd)
    {
      if(Char1 == -1)
      {
        if (StrBd.Length > 2 && StrBd[0] == CSVTableAttribute.QUOTE && StrBd[StrBd.Length - 1] == CSVTableAttribute.QUOTE)
        {
          StrBd.Remove(0, 1);
          StrBd.Remove(StrBd.Length - 1, 1);
        }
        StrBd.Replace("\"\"", "\"");
        Text = StrBd.ToString();
        StrBd.Clear();
        return 3;
      }
      var CH = (char)Char1;
      if (CH == CSVTableAttribute.QUOTE)
      {
        Escaped = !Escaped;
        //return 0;
      }
      if (!Escaped)
      {
        StrBd.Append(CH);
      }
      else
      {
        if (CH == CSVTableAttribute.COMMA)
        {
          //next field
          if (StrBd.Length > 2 && StrBd[0] == CSVTableAttribute.QUOTE && StrBd[StrBd.Length - 1] == CSVTableAttribute.QUOTE)
          {
            StrBd.Remove(0, 1);
            StrBd.Remove(StrBd.Length-1, 1);
          }
          StrBd.Replace("\"\"", "\"");
          Text = StrBd.ToString();
          StrBd.Clear();
          return 1;
        }
        else if (CH == CSVTableAttribute.LINE_FEED && StrBd.Length > 0 && StrBd[StrBd.Length - 1] == CSVTableAttribute.RETUEN)
        {
          //next line
          StrBd.Remove(StrBd.Length - 1, 1);
          if (StrBd.Length > 2 && StrBd[0] == CSVTableAttribute.QUOTE && StrBd[StrBd.Length - 1] == CSVTableAttribute.QUOTE)
          {
            StrBd.Remove(0, 1);
            StrBd.Remove(StrBd.Length - 1, 1);
          }
          StrBd.Replace("\"\"", "\"");
          Text = StrBd.ToString();
          StrBd.Clear();
          return 2;
        }
        else
        {
          StrBd.Append(CH);
        }
      }

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
          HasToEnclosed = true;
        }
        else
        {
          if (!HasToEnclosed
              && (
                StrBd[i] == ','
                || (StrBd[i] == '\n' && i > 0 && StrBd[i - 1] == '\r')
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
