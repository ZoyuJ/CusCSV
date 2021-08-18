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
    public readonly CSVTable Table;
    public readonly LinkedList<CSVField> Fields;

    public object this[CSVColumn Column] { get=>Fields.First(E=>E.Equals(Column)).Text; set=> Fields.First(E => E.Equals(Column)).Set(value); }
    public object this[int Index] { get => Fields.Skip(Index).FirstOrDefault(); set => Fields.Skip(Index).First().Set(value); }
    public object this[string Name] { get=>Fields.First(E=>E.Text == Name).Text; set=> Fields.First(E => E.Text == Name).Set(value); }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1"></param>
    /// <param name="Enclosed"></param>
    /// <param name="StrBd"></param>
    /// <returns>0:need more char,1:create new filed,2:create next row</returns>
    internal int NextChar(char Char1,CSVColumn Column, ref bool Enclosed, ref StringBuilder StrBd)
    {
      switch (Fields.Last.Value.NextChar(Char1, ref Enclosed, ref StrBd))
      {
        case 1:
          Fields.AddLast(new CSVField(this.Table, this, Column));
          return 1;
        case 2:
          return 2;
      }
      return 0;
    }

    public override string ToString()
      => $"{string.Join(",", Fields)}";

  }
}
