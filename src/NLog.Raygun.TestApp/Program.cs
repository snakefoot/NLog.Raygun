using System;
using System.Collections.Generic;

namespace NLog.Raygun.TestApp
{
  internal class Program
  {
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    
    private static void Main(string[] args)
    {
      Console.WriteLine("Sending Message to RayGun...");

      Logger.Info("This is a test!");

      try
      {
        var e = new Exception("Test Exception");
        e.Data["Tags"] = new List<string> { "Tester123" };

        throw e;
      }
      catch (Exception exception)
      {
        Logger.Error(exception);
      }

      Console.WriteLine("Finished...");
      Console.Read();
    }
  }
}