namespace CusCSV.Exceptions
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  class ColumnOutOfRangeException:Exception
  {
    public readonly int ColumCountInSource;
    public readonly int ColumCountInTable;
    public ColumnOutOfRangeException(int StreamColumnsCount,int TableColumsCount) : base("Columns do not match")
    {
      ColumCountInTable = TableColumsCount;
      ColumCountInSource = StreamColumnsCount;
    }
  }
}
