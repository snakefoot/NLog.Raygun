using System;
using System.Collections.Generic;

namespace NLog.Raygun.TestApp
{
  internal class Program
  {
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    private static void Main(string[] args)
    {
      AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;

      // By default all log events are sent as Raygun error reports.
      // However log events without an exception can be recorded as a breadcrumb for the next
      // error report if `IncludeBreadcrumbMessages` is set to `true` in your Raygun target configuration.
      Logger.Info("Hello world!");

      try
      {
        var e = new Exception("A generic handled exception");
        e.Data["Tags"] = new List<string> { "NonFatal" };

        throw e;
      }
      catch (Exception exception)
      {
        Logger.Error(exception);
      }

      PerformOperation();
    }

    private static void PerformOperation()
    {
      PerformUnsafeOperation();
    }

    private static void PerformUnsafeOperation()
    {
      var zero = 0;
      var one = 1;

      var result = one / zero;
    }

    private static void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      var exception = e.ExceptionObject as Exception;
      exception.Data["Tags"] = new List<string> { "Fatal", "UnhandledException" };
      Logger.Fatal(exception);

      Console.WriteLine("A fatal error has occurred.\nPress any <Enter> to exit...");
      while (Console.ReadKey().Key != ConsoleKey.Enter) {}
    }
  }
}