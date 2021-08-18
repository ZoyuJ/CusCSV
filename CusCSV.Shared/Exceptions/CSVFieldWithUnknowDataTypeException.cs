namespace CusCSV.Exceptions
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  public class CSVFieldWithUnknowDataTypeException:Exception
  {
    public readonly string Column;
    public readonly string FieldText;
    public CSVFieldWithUnknowDataTypeException(string Column, string FieldText):base("Can not change string to unknown type")
    {
      this.Column = Column;
      this.FieldText = FieldText;
    }
  }
}
