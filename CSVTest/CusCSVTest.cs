namespace CSVTest
{
  using System;
  using System.Collections.Generic;
  using System.Text;

  using Xunit;
  using CusCSV;
  using System.Linq;
  using KatKits;
  using System.IO;

  public class CusCSVTest
  {
    [CSVTable(TableName = "TestTable")]
    [CSVTable(TableName = "TestTable2")]
    public class CSVTestClass
    {
      [CSVColumn(TableName = "TestTable", ColumnName = "C1", Order = 0)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "W1", Order = 0)]
      public int P1 { get; set; }
      [CSVColumn(TableName = "TestTable", ColumnName = "C2", Order = 1)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "W2", Order = 1)]
      public string P2 { get; set; }
      [CSVColumn(TableName = "TestTable", ColumnName = "C3", Order = 3)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "CW3", Order = 3)]
      public string P3 { get; set; }
      [CSVColumn(TableName = "TestTable", ColumnName = "C4", Order = 4)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "CW4", Order = 4)]
      public string P4 { get; set; }
      [CSVColumn(TableName = "TestTable", ColumnName = "C5", Order = 5)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "CW5", Order = 5)]
      public string P5 { get; set; }
      [CSVColumn(TableName = "TestTable", ColumnName = "C6", Order = 6)]
      [CSVColumn(TableName = "TestTable2", ColumnName = "CW6", Order = 2)]
      public string P6 { get; set; }
    }
    [Theory]
    [InlineData("", null)]
    [InlineData("1", null)]
    [InlineData("\"\"\"1\"", null)]
    [InlineData("\"1\"\"\"", null)]
    [InlineData("1\n\r1", null)]
    [InlineData("\"1\r\n1\"", null)]
    [InlineData("aaa,bbb,ccc", null)]
    [InlineData("aaa,bbb,ccc\n\r", null)]
    [InlineData("\"aaa\",\"bbb\",ccc", "aaa,bbb,ccc")]
    [InlineData("\"aaa\",\"bbb\",ccc\n\r", "aaa,bbb,ccc\n\r")]
    [InlineData("\"aaa\",b \n\r bb,ccc", "aaa,b \n\r bb,ccc")]
    [InlineData("\"aaa\",\"b \r\n bb\",ccc", "aaa,\"b \r\n bb\",ccc")]

    [InlineData("aaa,\"b \"\" bb\",ccc\r\naaa,\"b \"\" bb\",ccc\r\naaa,\"b \"\" bb\",ccc", null)]
    [InlineData("aaa,\"b \"\"\r\n bb\",ccc\r\naaa,\"b \"\"\r\n bb\",ccc\r\naaa,\"b \"\"\r\n bb\",ccc", null)]
    public void ReadFromCSVText(string Text, string AlterN)
    {
      if (AlterN == null) AlterN = Text;
      var Tb = new CSVTable();
      Tb.ParseFromText(Text);
      Assert.Equal(AlterN, Tb.ToCSVString());
    }
    [Theory]
    [InlineData("", null)]
    [InlineData("1", null)]
    [InlineData("\"\"\"1\"", null)]
    [InlineData("\"1\"\"\"", null)]
    [InlineData("1\n\r1", null)]
    [InlineData("\"1\r\n1\"", null)]
    [InlineData("aaa,bbb,ccc", null)]
    [InlineData("aaa,bbb,ccc\n\r", null)]
    [InlineData("\"aaa\",\"bbb\",ccc", "aaa,bbb,ccc")]
    [InlineData("\"aaa\",\"bbb\",ccc\n\r", "aaa,bbb,ccc\n\r")]
    [InlineData("\"aaa\",b \n\r bb,ccc", "aaa,b \n\r bb,ccc")]
    [InlineData("\"aaa\",\"b \r\n bb\",ccc", "aaa,\"b \r\n bb\",ccc")]
    [InlineData("\"aaa\",\"b \"\" bb\",ccc", "aaa,\"b \"\" bb\",ccc")]
    [InlineData("aaa,\"b \"\" bb\",ccc\r\naaa,\"b \"\" bb\",ccc\r\naaa,\"b \"\" bb\",ccc", null)]
    [InlineData("aaa,\"b \"\"\r\n bb\",ccc\r\naaa,\"b \"\"\r\n bb\",ccc\r\naaa,\"b \"\"\r\n bb\",ccc", null)]
    public void ReadFromCSVStream(string Text,string AlterN)
    {
      if (AlterN == null) AlterN = Text;
      var Tb = new CSVTable();
      Tb.ParseFromStream(new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Text))));
      Assert.Equal(AlterN, Tb.ToCSVString());
    }

    //[Fact]
    //public void TestTableCreate()
    //{
    //  var Tb = new CSVTable(typeof(CSVTestClass), "TestTable");
    //  Assert.Equal("TestTable", Tb.TableName);
    //  Assert.Equal(6, Tb.Columns.Count);
    //  var H = Tb.Columns.First;
    //  Assert.Equal("C1", H.Value.Text);
    //  Assert.Equal(typeof(int), H.Value.DataType);
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C2", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C3", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C4", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C5", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C6", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //}
    //[Fact]
    //public void TestTableCreateFull()
    //{
    //  var Tb = new CSVTable(typeof(CSVTestClass), "TestTable");
    //  Assert.Equal("TestTable", Tb.TableName);
    //  Assert.Equal(6, Tb.Columns.Count);
    //  var H = Tb.Columns.First;
    //  Assert.Equal("C1", H.Value.Text);
    //  Assert.Equal(typeof(int), H.Value.DataType);
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C2", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C3", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C4", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C5", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //  H = H.Next;
    //  if (H != null)
    //  {
    //    Assert.Equal("C6", H.Value.Text);
    //    Assert.Equal(typeof(string), H.Value.DataType);
    //  }
    //}

  }
  //[CSVTable(TableName ="Full")]
  //public class TestClass
  //{
  //  [CSVColumn(ColumnName = "This is Int32", Order = 3,TableName="Full")]
  //  public int INT { get; set; }
  //  [CSVColumn(ColumnName = "This is Int64", Order = 4,TableName="Full")]
  //  public long LONG { get; set; }
  //  [CSVColumn(ColumnName = "This is INT8", Order = 1,TableName="Full")]
  //  public sbyte SBYTE { get; set; }
  //  [CSVColumn(ColumnName = "This is INT16", Order = 2,TableName="Full")]
  //  public short SHORT { get; set; }
  //  [CSVColumn(ColumnName = "This is UInt32", Order = 5,TableName="Full")]
  //  public uint UINT { get; set; }
  //  [CSVColumn(ColumnName = "This is UInt64", Order = 6,TableName="Full")]
  //  public ulong ULONG { get; set; }
  //  [CSVColumn(ColumnName = "This is UINT8", Order = 7,TableName="Full")]
  //  public byte BYTE { get; set; }
  //  [CSVColumn(ColumnName = "This is UINT16", Order = 8,TableName="Full")]
  //  public short USHORT { get; set; }
  //  [CSVColumn(ColumnName = "This is SINGLE", Order = 9,TableName="Full")]
  //  public float SINGLE { get; set; }
  //  [CSVColumn(ColumnName = "This is DOUBLE", Order = 10,TableName="Full")]
  //  public double DOUBLE { get; set; }
  //  [CSVColumn(ColumnName = "This is DECIMAL", Order = 10,TableName="Full")]
  //  public decimal DECIMAL { get; set; }
  //  [CSVColumn(ColumnName = "This is DATETIME", Order = 10,TableName="Full")]
  //  public DateTime DATETIME { get; set; }
  //  [CSVColumn(ColumnName = "This is GUID", Order = 10,TableName="Full")]
  //  public Guid GUID { get; set; }
  //  [CSVColumn(ColumnName = "This is TIMESPAN", Order = 10,TableName="Full")]
  //  public TimeSpan TIMESPAN { get; set; }
  //  [CSVColumn(ColumnName = "This is DATETIMEOFFSET", Order = 10,TableName="Full")]
  //  public DateTimeOffset DATETIMEOFFSET { get; set; }

  //  [CSVColumn(ColumnName = "This is Nullable Int32", Order = 3,TableName="Full")]
  //  public int? NBINT { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable Int64", Order = 4,TableName="Full")]
  //  public long? NBLONG { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable INT8", Order = 1,TableName="Full")]
  //  public sbyte? NBSBYTE { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable INT16", Order = 2,TableName="Full")]
  //  public short? NBSHORT { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable UInt32", Order = 5,TableName="Full")]
  //  public uint? NBUINT { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable UInt64", Order = 6,TableName="Full")]
  //  public ulong? NBULONG { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable UINT8", Order = 7,TableName="Full")]
  //  public byte? NBBYTE { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable UINT16", Order = 8,TableName="Full")]
  //  public short? NBUSHORT { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable SINGLE", Order = 9,TableName="Full")]
  //  public float? NBSINGLE { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable DOUBLE", Order = 10,TableName="Full")]
  //  public double? NBDOUBLE { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable DECIMAL", Order = 10,TableName="Full")]
  //  public decimal? NBDECIMAL { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable DATETIME", Order = 10,TableName="Full")]
  //  public DateTime? NBDATETIME { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable GUID", Order = 10,TableName="Full")]
  //  public Guid? NBGUID { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable TIMESPAN", Order = 10,TableName="Full")]
  //  public TimeSpan? NBTIMESPAN { get; set; }
  //  [CSVColumn(ColumnName = "This is Nullable DATETIMEOFFSET", Order = 10,TableName="Full")]
  //  public DateTimeOffset? NBDATETIMEOFFSET { get; set; }

  //  [CSVColumn(ColumnName = "This is STRING", Order = 10,TableName="Full")]
  //  public string STRING { get; set; }
  //}

}
