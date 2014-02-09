using System;
using System.Web.Script.Serialization;
using System.IO;
using System.Reflection;
using ClipperLib;
using System.Collections.Generic;
using System.Linq;

namespace Testing
{
  public class Tester
  {
    //http://social.msdn.microsoft.com/Forums/en-US/2ff3016c-bd61-4fec-8f8c-7b6c070123fa/c-compare-two-lists-of-objects?forum=csharplanguage
    //(I can't believe I have to do that, I think I'm missing something in the defautl lib)
    public class ListComparer<T> : IEqualityComparer<List<T>>
    {
      readonly IEqualityComparer<T> itemComparer;

      public ListComparer ()
      {
        itemComparer = EqualityComparer<T>.Default;
      }

      public bool Equals (List<T> x, List<T> y)
      {
        return x.SequenceEqual (y, itemComparer);
      }

      public int GetHashCode (List<T> obj)
      {
        const int somePrimeNumber = 37;

        int result = 1;
        foreach (T item in obj.Take(5)) {
          result = (somePrimeNumber * result) + itemComparer.GetHashCode (item);
        }

        return result;
      }
    }
    #pragma warning disable 0649
    [Serializable]
    struct TestPoint
    {
      public TestPoint (double X, double Y)
      {
        this.X = X;
        this.Y = Y;
      }

      public double X;
      public double Y;

      public override bool Equals (Object obj)
      {
        return obj is TestPoint && this == (TestPoint)obj;
      }

      public override int GetHashCode ()
      {
        return X.GetHashCode () ^ Y.GetHashCode ();
      }

      public static bool operator == (TestPoint a, TestPoint b)
      {
        return a.X == b.X && a.Y == b.Y;
      }

      public static bool operator != (TestPoint a, TestPoint b)
      {
        return !(a == b);
      }
    }
    [Serializable]
    class TestOperation
    {
      public ClipType type = ClipType.ctUnion;
      public PolyFillType subjectFill = PolyFillType.pftEvenOdd;
      public PolyFillType clipFill = PolyFillType.pftEvenOdd;
      public bool subjectClosed = true;

      public List<List<IntPoint>> Run (List<List<IntPoint>> subject, List<List<IntPoint>> clip)
      {
        var clipper = new Clipper ();
        clipper.AddPaths (subject, PolyType.ptSubject, subjectClosed);
        clipper.AddPaths (clip, PolyType.ptClip, true);
        var solution = new List<List<IntPoint>> ();
        clipper.Execute (type, solution, subjectFill, clipFill);
        return solution;
      }
    }

    [Serializable]
    class ClipperTest
    {
      public List<List<TestPoint>> subject;
      public List<List<TestPoint>> clip;
      public double scale;
      public List<List<TestPoint>> expected;
      public TestOperation operation;

      public static List<List<TestPoint>> FromClipper (double scale, List<List<IntPoint>> data)
      {
        return data.ConvertAll (path => path.ConvertAll (point => new TestPoint (point.X / scale, point.Y / scale)));
      }
      public static List<List<IntPoint>> ToClipper (double scale, List<List<TestPoint>> data)
      {
        return data.ConvertAll (path => path.ConvertAll (point => new IntPoint (point.X * scale, point.Y * scale)));
      }

      public void Run ()
      {
        var s = ToClipper (scale, subject);
        var c = ToClipper (scale, clip);
        var result = FromClipper (scale, operation.Run (s, c));
        Console.WriteLine ("result:");
        Console.WriteLine (new JavaScriptSerializer ().Serialize (result));
        Console.WriteLine ("expected:");
        Console.WriteLine (new JavaScriptSerializer ().Serialize (expected));
        var testPassed = result.SequenceEqual (expected, new ListComparer<TestPoint> ());
        if (testPassed)
          Console.WriteLine ("test passed \\o/");
        else {
          Console.WriteLine ("test failed :(");
          Console.WriteLine (testPassed);
          Console.WriteLine (new JavaScriptSerializer ().Serialize (result.Except (expected)));
        }
      }
    }
    #pragma warning restore 0649

    public static void Run ()
    {
      Assembly assembly = Assembly.GetExecutingAssembly ();
      using (var polyStream = new StreamReader (assembly.GetManifestResourceStream ("Testing.simpleTest.json"))) {
        string file = polyStream.ReadToEnd ();
        var serializer = new JavaScriptSerializer ();
        var test = serializer.Deserialize<ClipperTest> (file);
        Console.Write (file);
        Console.WriteLine ("running");
        test.Run ();
        Console.WriteLine ("done!");
      }
    }
  }
}

