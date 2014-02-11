using System;
using System.Web.Script.Serialization;
using System.IO;
using System.Reflection;
using ClipperLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;

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
    class TestBooleanOperation
    {
      public ClipType type = ClipType.ctUnion;
      public List<List<TestPoint>> clip;
      public PolyFillType subjectFill = PolyFillType.pftEvenOdd;
      public PolyFillType clipFill = PolyFillType.pftEvenOdd;
      public bool subjectClosed = true;

      public List<List<IntPoint>> Run (List<List<IntPoint>> subject, double scale)
      {
        var c = ToClipper (scale, clip);
        var clipper = new Clipper ();
        clipper.AddPaths (subject, PolyType.ptSubject, subjectClosed);
        clipper.AddPaths (c, PolyType.ptClip, true);
        var solution = new List<List<IntPoint>> ();
        clipper.Execute (type, solution, subjectFill, clipFill);
        return solution;
      }
    }

    class TestOffsetOperation
    {
      public double delta;
      public double miterLimit = 2.0;
      public double arcTolerance = 0.25;
      public JoinType joinType;
      public EndType endType;

      public List<List<IntPoint>> Run (List<List<IntPoint>> subject, double scale)
      {
        var clipperOffset = new ClipperOffset (miterLimit, arcTolerance);
        clipperOffset.AddPaths (subject, joinType, endType);
        var result = new List<List<IntPoint>> ();
        clipperOffset.Execute (ref result, scale * delta);
        return result;
      }
    }

    [Serializable]
    class ClipperTest
    {
      public List<List<TestPoint>> subject;
      public double scale;
      public List<List<TestPoint>> expected;
      public TestBooleanOperation booleanOperation;
      public TestOffsetOperation offsetOperation;

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

      public static List<List<TestPoint>> FromClipper (double scale, List<List<IntPoint>> data)
      {
        return data.ConvertAll (path => path.ConvertAll (point => new TestPoint (point.X / scale, point.Y / scale)));
      }

      public static List<List<IntPoint>> ToClipper (double scale, List<List<TestPoint>> data)
      {
        return data.ConvertAll (path => path.ConvertAll (point => new IntPoint (Math.Round (point.X * scale), Math.Round (point.Y * scale))));
      }

      public void Run ()
      {
        var s = ToClipper (scale, subject);
        var clipperResult = booleanOperation != null ? booleanOperation.Run (s, scale) : offsetOperation.Run (s, scale);
        var diff = RelativeAreaDiff (clipperResult, ToClipper (scale, expected));
        if (diff < 0.0000001)
          Console.WriteLine ("test passed (relative diff: " + diff + ") \\o/ ");
        else {
          Console.WriteLine ("test failed (relative diff: " + diff + ") :(");
          Console.WriteLine (new JavaScriptSerializer ().Serialize (FromClipper (scale, clipperResult)));
        }
      }
    }
    #pragma warning restore 0649

    private static List<List<TestPoint>> FromClipper (double scale, List<List<IntPoint>> data)
    {
      return data.ConvertAll (path => path.ConvertAll (point => new TestPoint (point.X / scale, point.Y / scale)));
    }

    private static List<List<IntPoint>> ToClipper (double scale, List<List<TestPoint>> data)
    {
      return data.ConvertAll (path => path.ConvertAll (point => new IntPoint (point.X * scale, point.Y * scale)));
    }

    public static void RunTest (string fileName)
    {
      //mono fucktards: https://bugzilla.xamarin.com/show_bug.cgi?id=4242 
      Thread.CurrentThread.CurrentCulture = new CultureInfo ("en-US");
      Assembly assembly = Assembly.GetExecutingAssembly ();
      using (var polyStream = new StreamReader (assembly.GetManifestResourceStream (fileName))) {
        string file = polyStream.ReadToEnd ();
        var test = new JavaScriptSerializer ().Deserialize<ClipperTest> (file);
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
      RunTest ("Testing.offsetSquareMinus10Test.json");
      RunTest ("Testing.offsetSquare10Test.json");
      RunTest ("Testing.offsetMiterMinus10Test.json");
      RunTest ("Testing.offsetMiter10Test.json");
      RunTest ("Testing.offsetRoundMinus10Test.json");
      RunTest ("Testing.offsetRound10Test.json");
    }
  }
}

