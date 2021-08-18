namespace CusCSV.Exceptions
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  public class DamagedCSVFileException:Exception
  {
    public readonly int AtRow;
    public readonly int AtColumn;
    public DamagedCSVFileException(int AtRow) : base("CSV file was damaged")
    {
      this.AtRow = AtRow;
    }
  }
}
