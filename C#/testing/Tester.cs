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
    #pragma warning disable 0649
    [Serializable]
    struct TestPoint
    {
      public TestPoint (double x, double y)
      {
        this.X = x;
        this.Y = y;
      }

      public double X;
      public double Y;
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

      public double RelativeAreaDiff (List<List<IntPoint>> actual, List<List<IntPoint>>expected)
      {
        var expectedArea = expected.Sum (path => Clipper.Area (path));
        var difference = new List<List<IntPoint>> ();
        var clipper = new Clipper ();
        clipper.AddPaths (actual, PolyType.ptSubject, true);
        clipper.AddPaths (expected, PolyType.ptClip, true);
        clipper.Execute (ClipType.ctXor, difference, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
        var differenceArea = difference.Sum (path => Clipper.Area (path));
        return Math.Abs (differenceArea) / Math.Abs (expectedArea);
      }

      public void Run ()
      {
        var s = ToClipper (scale, subject);
        var c = ToClipper (scale, clip);
        var clipperResult = operation.Run (s, c);
        var diff = RelativeAreaDiff (clipperResult, ToClipper (scale, expected));
        if (diff < 0.0000001)
          Console.WriteLine ("test passed (diff: " + diff + ") \\o/ ");
        else
          Console.WriteLine ("test failed (diff: " + diff + ") :(");
      }
    }
    #pragma warning restore 0649

    public static void RunTest (string fileName)
    {
      Assembly assembly = Assembly.GetExecutingAssembly ();
      using (var polyStream = new StreamReader (assembly.GetManifestResourceStream (fileName))) {
        string file = polyStream.ReadToEnd ();
        var serializer = new JavaScriptSerializer ();
        var test = serializer.Deserialize<ClipperTest> (file);
        Console.WriteLine ("running " + fileName);
        test.Run ();
      }
    }

    public static void Run ()
    {
      RunTest ("Testing.simpleUnionTest.json");
      RunTest ("Testing.simpleIntersectionTest.json");
      RunTest ("Testing.simpleDifferenceTest.json");
      RunTest ("Testing.simpleXorTest.json");
    }
  }
}

