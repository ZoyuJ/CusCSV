using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Try {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Hello World!");
      //A a = new A();
      //var Mems =  a.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(E => E.MemberType == MemberTypes.Field).ToArray();
      //Array.ForEach(Mems, E => {
      //  Console.WriteLine(E.Name);
      //  Console.WriteLine(E.ReflectedType);
      //  Console.WriteLine(E.DeclaringType);
      //  Console.WriteLine(E.GetType());
      //});

      HistroyEntry<StudentAnswerQuestion> Answer = new HistroyEntry<StudentAnswerQuestion>(new StudentAnswerQuestion() {
        Choose = 1,
        IsCorrect = true,
        Member = new ClassMember() {
          CognizeId = 4,
          MemberId = "c4",
          NameOrDeviceName = "c4s",
          Online = true
        },
        Question = new TeacherDefindQuestion() {
          CorrentKey = 1,
          Id = "1223445",
          Score = 20,
          Target = ResourceTarget.Client,
          Title = "qqqq",
          Type = ResourceType.Question
        }
      });
      var Str = JsonConvert.SerializeObject(Answer);
      ITraceWriter traceWriter = new MemoryTraceWriter();
      var NewAns = JsonConvert.DeserializeObject<HistroyEntry<StudentAnswerQuestion>>(Str, new JsonSerializerSettings() { TraceWriter = traceWriter });
      Console.WriteLine(traceWriter);
      Console.WriteLine(NewAns.Content.Choose);


      Console.ReadKey();
    }
  }

  public class A {
    public string As;
    public B Ab;
  }
  public class B { }

  public class HistroyEntry<T> {
    public readonly DateTime TimeFrame;
    public readonly T Content;

    public HistroyEntry() { }
    public HistroyEntry(T Obj) {
      TimeFrame = DateTime.Now;
      Content = Obj;
    }
    public HistroyEntry(DateTime TimeFrame, T Content) {
      this.TimeFrame = TimeFrame;
      this.Content = Content;
    }
  }
  public class StudentAnswerQuestion {
    public TeacherDefindQuestion Question { get; set; }
    public ClassMember Member { get; set; }
    public int Choose { get; set; }
    public bool IsCorrect { get; set; }
    public enum SortBy {
      None = 0,
      CurrentFirst = 1,
      WrongFirst = 2,
      NameCharSortMinFirst = 3,
      NameCharSortMaxFirst = 4,
    }

  }
  public class TeacherDefindQuestion : ClassResource {
    public int CorrentKey { get; set; }
    public int Score { get; set; }
    public int OptionCount { get; set; }
    public static char[] OptionValueToOptionNames(int Value, int Len) {
      var Res = new List<char>();
      for (int i = 0; i < Len; i++) {
        var Checkint = Convert.ToInt32(Math.Pow(2, i));
        if ((Checkint & Value) != 0) {
          Res.Add((char)(65 + i));
        }
      }
      return Res.ToArray();
    }
    public static char[] OptionValueToOptionNames(int Value) {
      var Res = new List<char>();
      int i = 0;
      while (Value > 0) {
        var Checkint = Convert.ToInt32(Math.Pow(2, i));
        if ((Checkint & Value) != 0) {
          Res.Add((char)(65 + i));
          Value -= Checkint;
        }
        i++;
      }
      return Res.ToArray();
    }
    public static int[] OptionValueToOptionValues(int Value, int Len) {
      var Res = new List<int>();
      for (int i = 0; i < Len; i++) {
        var Checkint = Convert.ToInt32(Math.Pow(2, i));
        if ((Checkint & Value) != 0) {
          Res.Add(Checkint);
        }
      }
      return Res.ToArray();
    }
    public static int[] OptionValueToOptionValues(int Value) {
      var Res = new List<int>();
      int i = 0;
      while (Value > 0) {
        var Checkint = Convert.ToInt32(Math.Pow(2, i));
        if ((Checkint & Value) != 0) {
          Res.Add(Checkint);
          Value -= Checkint;
        }
        i++;
      }
      return Res.ToArray();
    }
    public static int OptionNamesToOptionValues(char[] Names) {
      int Res = 0;
      for (int i = 0; i < Names.Length; i++) {
        Res += Convert.ToInt32(Math.Pow(2, Names[i] - 65));
      }
      return Res;
    }
    public static char OptionValueToOptionName(int Value) => (char)(Convert.ToInt32(Math.Log(Value, 2)) + 65);
    public static int OptionNameToOptionValue(char Name) => Convert.ToInt32(Math.Pow(2, (Name - 65)));
  }
  public class ClassResource : IComparable<ClassResource> {
    public ResourceTarget Target { get; set; }
    public ResourceType Type { get; set; }
    public string Id { get; set; }
    public string Title { get; set; }

    public int CompareTo(ClassResource other) {
      return Type.CompareTo(other.Type) + Id.CompareTo(other.Id);
    }
  }
  [Flags]
  public enum ResourceTarget {
    Disable = 0,
    Main = 1,
    Group = 2,
    Client = 4,
  }
  public enum ResourceType {
    TemperaryResource = 1, //发送文件,获取屏幕,发送截图
    Question = 2, //题目
    ThridPartResource = 3, //启动其他文件
  }
  public interface ICompar<T> where T : class, ICompar<T> { bool ItsMe(T Other); }
  [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
  public class ClassMember : ICompar<ClassMember> {
    public const int CognizedId_Main = 0x01, CognizedId_Group = 0x02, CognizedId_Client = 0x04;
    public bool ItsMe(ClassMember Other) {
      return CognizeId == Other.CognizeId && MemberId == Other.MemberId;
    }
    public string NameOrDeviceName { get; set; }
    public int CognizeId { get; set; }
    public string MemberId { get; set; }
    public bool Online { get; set; }
  }

}
