using System;

using CSV;

namespace Try {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");



      Console.ReadKey();
    }
  }

  [CSVTable()]
  class A {
    [CSVColumn("A1Propt")]
    public string A1 { get; set; }
    [CSVColumn("A1Field")]
    public string A2;
  }

}
