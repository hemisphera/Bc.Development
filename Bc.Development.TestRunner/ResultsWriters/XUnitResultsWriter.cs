using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Bc.Development.TestRunner.ResultsWriters
{
  /// <summary>
  /// Writes the test results as XUnit XML.
  /// </summary>
  public class XUnitResultsWriter : FileResultsWriter
  {
    /// <summary>
    /// </summary>
    public XUnitResultsWriter(string filename)
    {
      Filename = filename;
    }

    /// <inheritdoc />
    public override async Task Write(IEnumerable<CommandLineTestToolCodeunit> codeunits)
    {
      await Task.Run(() =>
      {
        using (var xw = new XmlTextWriter(Stream, Encoding.UTF8))
        {
          xw.WriteStartDocument();
          xw.WriteStartElement("assemblies");
          foreach (var codeunit in codeunits)
          {
            WriteAssembly(xw, codeunit);
          }

          xw.WriteEndElement();
        }
      });
    }


    private void WriteAssembly(XmlTextWriter xw, CommandLineTestToolCodeunit codeunit)
    {
      var total = codeunit.Methods.Count;
      var passed = codeunit.Methods.Count(r => r.Result == TestResult.Success);
      var failed = codeunit.Methods.Count(r => r.Result == TestResult.Failure);

      xw.WriteStartElement("assembly");
      xw.WriteAttributeString("name", $"{codeunit.CodeunitId} {codeunit.Name}");
      xw.WriteAttributeString("test-framework", "PS Test Runner");
      xw.WriteAttributeString("run-date", codeunit.StartTime.ToString("yyyy-MM-dd"));
      xw.WriteAttributeString("run-time", codeunit.StartTime.ToString("HH':'mm':'ss"));
      xw.WriteAttributeString("total", XmlConvert.ToString(total));
      xw.WriteAttributeString("passed", XmlConvert.ToString(passed));
      xw.WriteAttributeString("failed", XmlConvert.ToString(failed));
      xw.WriteAttributeString("skipped", XmlConvert.ToString(total - passed - failed));
      xw.WriteAttributeString("time", XmlConvert.ToString(codeunit.Duration.TotalSeconds));

      xw.WriteStartElement("collection");
      xw.WriteAttributeString("name", codeunit.Name);
      xw.WriteAttributeString("total", XmlConvert.ToString(total));
      xw.WriteAttributeString("passed", XmlConvert.ToString(passed));
      xw.WriteAttributeString("failed", XmlConvert.ToString(failed));
      xw.WriteAttributeString("skipped", XmlConvert.ToString(total - passed - failed));
      xw.WriteAttributeString("time", XmlConvert.ToString(codeunit.Duration.TotalSeconds));

      foreach (var result in codeunit.Methods)
      {
        WriteResult(xw, codeunit, result);
      }

      xw.WriteEndElement(); // collection
      xw.WriteEndElement(); // assembly
    }

    private static void WriteResult(XmlTextWriter xw, CommandLineTestToolCodeunit codeunit, CommandLineTestToolMethod method)
    {
      var resultString = "Skipped";
      if (method.Result == TestResult.Success) resultString = "Pass";
      if (method.Result == TestResult.Failure) resultString = "Fail";

      xw.WriteStartElement("test");
      xw.WriteAttributeString("name", $"{codeunit.Name}:{method.MethodName}");
      xw.WriteAttributeString("method", method.MethodName);
      xw.WriteAttributeString("time", XmlConvert.ToString(method.Duration.TotalSeconds));
      xw.WriteAttributeString("result", resultString);
      xw.WriteEndElement(); // test
    }
  }
}