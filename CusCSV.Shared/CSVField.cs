namespace CusCSV
{
  using CusCSV.Exceptions;

  using KatKits;

  using System;
  using System.Text;

  public class CSVField
  {
    internal CSVField() {

    }
    internal CSVField(CSVColumn Column)
    {
      this.Column = Column;
    }
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
    internal CSVReaderStatus NextChar(int Char1, ref bool Escaped, ref StringBuilder StrBd)
    {
      switch (CSVExpress. CharByChar(Char1,ref Escaped,ref StrBd))
      {
        case CSVReaderStatus.MoreChar:
        default:
          return CSVReaderStatus.MoreChar;
        case CSVReaderStatus.NewField:
          Text = StrBd.ToString();
          StrBd.Clear();
          return CSVReaderStatus.NewField;
        case CSVReaderStatus.NewRow:
          Text = StrBd.ToString();
          StrBd.Clear();
          return CSVReaderStatus.NewRow;
        case CSVReaderStatus.EndOfText:
          Text = StrBd.ToString();
          StrBd.Clear();
          return CSVReaderStatus.EndOfText;
      }
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
