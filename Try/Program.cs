using System;
using System.Linq;
using System.Reflection;

namespace Try {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");
      A a = new A();
      var Mems =  a.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(E => E.MemberType == MemberTypes.Field).ToArray();
      Array.ForEach(Mems, E => {
        Console.WriteLine(E.Name);
        Console.WriteLine(E.ReflectedType);
        Console.WriteLine(E.DeclaringType);
        Console.WriteLine(E.GetType());
      });

      Console.ReadKey();
    }
  }

  public class A {
    public string As;
    public B Ab;
  }
  public class B { }

}
