namespace CusCSV
{
  using System;
  using System.Collections;
  using System.Collections.Generic;
  using System.Data;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Linq;
  using KatKits;

  public class CSVRow
  {
    public CSVRow(CSVTable Table)
    {
      this.Table = Table;
    }
    internal CSVRow() { }
    public readonly CSVTable Table;
    public readonly LinkedList<CSVField> Fields = new LinkedList<CSVField>();

    public object this[CSVColumn Column] { get => Fields.First(E => E.Equals(Column)).Text; set => Fields.First(E => E.Equals(Column)).Set(value); }
    public object this[int Index] { get => Fields.Skip(Index).FirstOrDefault(); set => Fields.Skip(Index).First().Set(value); }
    public object this[string Name] { get => Fields.First(E => E.Text == Name).Text; set => Fields.First(E => E.Text == Name).Set(value); }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1"></param>
    /// <param name="Column"></param>
    /// <param name="Escaped"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need more char,1:create new filed,2:create next row</returns>
    internal CSVReaderStatus NextChar(int Char1, CSVColumn Column, ref bool Escaped, ref StringBuilder StrBd)
    {
      switch (Fields.Last.Value.NextChar(Char1, ref Escaped, ref StrBd))
      {
        case CSVReaderStatus.MoreChar:
        default:
          return CSVReaderStatus.MoreChar;
        case CSVReaderStatus.NewField:
          Fields.AddLast(new CSVField(this.Table, this, Column));
          return CSVReaderStatus.NewField;
        case CSVReaderStatus.NewRow:
          return CSVReaderStatus.NewRow;
        case CSVReaderStatus.EndOfText:
          return CSVReaderStatus.EndOfText;
      }
    }

    public string ToCSVString()
      => $"{string.Join(",", Fields.Select(E => E.ToCSVString()))}";

  }
}
