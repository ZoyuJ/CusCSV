namespace CusCSV
{
  using System;
  using System.Collections.Generic;
  using System.Data;
  using System.IO;
  using System.Linq;
  using System.Text;

  internal enum CSVReaderStatus
  {
    MoreChar = 0,
    NewField = 1,
    NewRow = 2,
    EndOfText = 3,
  }

  public static class CSVExpress
  {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Char1">end of stream=-1</param>
    /// <param name="Escaped">init=true</param>
    /// <param name="StrBd">init=""</param>
    /// <returns>0:need nore char,1:create new field,2:create new line</returns>
    internal static CSVReaderStatus CharByChar(int Char1, ref bool Escaped, ref StringBuilder StrBd)
    {
      if (Char1 == -1)
      {
        if (StrBd.Length > 2 && StrBd[0] == CSVTableAttribute.QUOTE && StrBd[StrBd.Length - 1] == CSVTableAttribute.QUOTE)
        {
          StrBd.Remove(0, 1);
          StrBd.Remove(StrBd.Length - 1, 1);
        }
        StrBd.Replace("\"\"", "\"");
        return CSVReaderStatus.EndOfText;
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
            StrBd.Remove(StrBd.Length - 1, 1);
          }
          StrBd.Replace("\"\"", "\"");
          return CSVReaderStatus.NewField;
        }
        else if (CH == CSVTableAttribute.LINE_FEED && StrBd.Length > 0 && StrBd[StrBd.Length - 1] == CSVTableAttribute.RETURN)
        {
          //next line
          StrBd.Remove(StrBd.Length - 1, 1);
          if (StrBd.Length > 2 && StrBd[0] == CSVTableAttribute.QUOTE && StrBd[StrBd.Length - 1] == CSVTableAttribute.QUOTE)
          {
            StrBd.Remove(0, 1);
            StrBd.Remove(StrBd.Length - 1, 1);
          }
          StrBd.Replace("\"\"", "\"");
          return CSVReaderStatus.NewRow;
        }
        else
        {
          StrBd.Append(CH);
        }
      }

      return CSVReaderStatus.MoreChar;


    }

    public static LinkedList<CSVColumn> ReadHeaders(TextReader Reader)
    {
      var Header = new LinkedList<CSVColumn>();
      var CharInt = 0;
      var Escaped = true;
      var StrBd = new StringBuilder();
      while ((CharInt = Reader.Read()) != -1)
      {
        switch (CharByChar(CharInt, ref Escaped, ref StrBd))
        {
          case CSVReaderStatus.MoreChar:
          default:
            break;
          case CSVReaderStatus.NewField:
            Header.Last.Value.Text = StrBd.ToString();
            StrBd.Clear();
            Header.AddLast(new CSVColumn());
            break;
          case CSVReaderStatus.NewRow:
          case CSVReaderStatus.EndOfText:
            Header.Last.Value.Text = StrBd.ToString();
            StrBd.Clear();
            Header.AddLast(new CSVColumn());
            return Header;
        }
      }
      return Header;
    }
    public static IEnumerable<CSVRow> ReadRow(TextReader Reader, LinkedList<CSVColumn> Columns)
    {
      var CharInt = 0;
      var Escaped = true;
      var StrBd = new StringBuilder();
      var CurrentCol = Columns.First;
      var CSVRow = new CSVRow();
      CSVRow.Fields.AddLast(new CSVField(CurrentCol.Value));
      while ((CharInt = Reader.Read()) != -1)
      {
        switch (CharByChar(CharInt, ref Escaped, ref StrBd))
        {
          case CSVReaderStatus.MoreChar:
          default:
            break;
          case CSVReaderStatus.NewField:
            CSVRow.Fields.Last.Value.Text = StrBd.ToString();
            StrBd.Clear();
            CSVRow.Fields.AddLast(new CSVField(CurrentCol.Value));
            CurrentCol = CurrentCol.Next;
            break;
          case CSVReaderStatus.NewRow:
            CSVRow.Fields.Last.Value.Text = StrBd.ToString();
            StrBd.Clear();
            yield return CSVRow;
            CurrentCol = Columns.First;
            CSVRow = new CSVRow();
            CSVRow.Fields.AddLast(new CSVField(CurrentCol.Value));
            break;
        }
        CharByChar(-1, ref Escaped, ref StrBd);
        CSVRow.Fields.Last.Value.Text = StrBd.ToString();
        StrBd.Clear();
        yield return CSVRow;
        yield break;
      }
    }

  }
}
