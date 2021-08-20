namespace CusCSV
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  public class CSVColumn : IEquatable<CSVColumn>, IComparer<CSVColumn>, IComparable<CSVColumn>
  {
    internal CSVColumn() { }
    public CSVColumn(string Text)
    {
      this.Text = Text;
    }

    public string Text { get; set; } = "";
    public Type DataType { get; set; }

    public override string ToString()
    {
      StringBuilder StrBd = new StringBuilder(Text ?? "");
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
    public int CompareTo(CSVColumn other)
      => Text.CompareTo(other.Text);
    public bool Equals(CSVColumn other)
      => Text.Equals(other.Text);
    public int Compare(CSVColumn x, CSVColumn y)
      => x.Text.CompareTo(y.Text);
    public override int GetHashCode()
      => Text.GetHashCode();
    public override bool Equals(object obj)
      => this.Equals((CSVColumn)obj);
  }
}
