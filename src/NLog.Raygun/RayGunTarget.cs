using System;
using System.Collections.Generic;
using Mindscape.Raygun4Net;
#if !NET45
using Mindscape.Raygun4Net.AspNetCore;
#endif
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.Raygun
{
  [Target("RayGun")]
  public class RayGunTarget : TargetWithContext
  {
    [RequiredParameter]
    public string ApiKey
    {
      get { return (_apiKey as SimpleLayout)?.Text ?? _apiKey?.ToString(); }
      set { _apiKey = value; }
    }
    private Layout _apiKey;

    public string Tags { get; set; }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the Form data of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Form on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    public string IgnoreFormFieldNames { get; set; }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the cookies of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Cookies on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    public string IgnoreCookieNames { get; set; }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the server variables of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the ServerVariables on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    public string IgnoreServerVariableNames { get; set; }

    /// <summary>
    /// Adds a list of keys to ignore when attaching the headers of an HTTP POST request. This allows
    /// you to remove sensitive data from the transmitted copy of the Headers on the HttpRequest by specifying the keys you want removed.
    /// This method is only effective in a web context.
    /// </summary>
    public string IgnoreHeaderNames { get; set; }

    /// <summary>
    /// Specifies whether or not RawData from web requests is ignored when sending reports to Raygun.io.
    /// The default is false which means RawData will be sent to Raygun.io.
    /// </summary>
    public bool IsRawDataIgnored { get { return _isRawDataIgnored ?? false; } set { _isRawDataIgnored = value; } }
    private bool? _isRawDataIgnored;

    /// <summary>
    /// Explicitly defines lookup of user-identity for Raygun events.
    /// </summary>
    public Layout UserIdentityInfo { get; set; }

    /// <summary>
    /// Legacy parameter kept alive to avoid breaking change. NLog will not load configuration if trying to configure unknown properties
    /// </summary>
    [Obsolete("No longer supported. Instead consider using the UserIdentityInfo property")]
    public bool UseIdentityNameAsUserId { get; set; }

    /// <summary>
    /// Attempt to get the entry assembly version
    /// </summary>
    public bool UseExecutingAssemblyVersion { get; set; }

    /// <summary>
    /// Explicitly defines an application version for Raygun events.
    /// NOTE: This value will be ignored if UseExecutingAssemblyVersion is set to true and returns a value.
    /// </summary>
    public string ApplicationVersion
    {
      get { return (_applicationVersion as SimpleLayout)?.Text ?? _applicationVersion?.ToString(); }
      set { _applicationVersion = value; }
    }
    private Layout _applicationVersion;

    private RaygunClient _raygunClient;

    public RayGunTarget()
    {
      IncludeEventProperties = true;
      OptimizeBufferReuse = true;
      Layout = "${message}";
      _applicationVersion = "${assembly-version:cached=true:type=File}";
    }

    protected override void InitializeTarget()
    {
      if (ContextProperties.Count == 0)
      {
        ContextProperties.Add(new TargetPropertyWithContext("RenderedLogMessage", Layout));
        ContextProperties.Add(new TargetPropertyWithContext("LogMessageTemplate", "${message:raw=true}"));
      }

      base.InitializeTarget();
    }

    protected override void CloseTarget()
    {
      base.CloseTarget();
      _raygunClient = null;
    }

    protected override void Write(AsyncLogEventInfo logEvent)
    {
      try
      {
        _raygunClient = _raygunClient ?? (_raygunClient = CreateRaygunClient());

        Exception exception = ExtractException(logEvent.LogEvent);
        var tags = ExtractTags(logEvent.LogEvent, exception);
        var contextProperties = GetAllProperties(logEvent.LogEvent);
        contextProperties.Remove("Tags");
        contextProperties.Remove("tags");
        var userCustomData = new UserCustomDictionary(contextProperties);

        if (exception == null)
        {
          string layoutLogMessage = RenderLogEvent(Layout, logEvent.LogEvent);
          exception = new RaygunException(layoutLogMessage);
        }

        string userIdentityInfo = RenderLogEvent(UserIdentityInfo, logEvent.LogEvent);
#if NET45
        var userIdentity = string.IsNullOrEmpty(userIdentityInfo) ? null : new Mindscape.Raygun4Net.Messages.RaygunIdentifierMessage(userIdentityInfo);
        _raygunClient.SendInBackground(exception, tags, userCustomData, userIdentity);
        logEvent.Continuation(null);
#else
        var userIdentity = string.IsNullOrEmpty(userIdentityInfo) ? null : new RaygunIdentifierMessage(userIdentityInfo);
        _raygunClient.SendInBackground(exception, tags, userCustomData, userIdentity).ContinueWith((t,s) => SendCompleted(t.Exception, (AsyncContinuation)s), logEvent.Continuation);
#endif
      }
      catch (Exception ex)
      {
        InternalLogger.Error(ex, "RayGun(Name={0}): Failed to send logevent.", Name);
        logEvent.Continuation(ex);
      }
    }

    private static void SendCompleted(Exception taskException, AsyncContinuation continuation)
    {
      if (taskException != null)
      {
        InternalLogger.Error(taskException, "RayGun: Failed sending logevent.");
      }
      continuation(taskException);
    }

    private static Exception ExtractException(LogEventInfo logEvent)
    {
      if (logEvent.Exception != null)
      {
        return logEvent.Exception;
      }

      if (logEvent.Parameters != null && logEvent.Parameters.Length > 0)
      {
        return logEvent.Parameters[0] as Exception;
      }

      return null;
    }

    private List<string> ExtractTags(LogEventInfo logEvent, Exception exception)
    {
      List<string> tags = new List<string>();

      // Try and get tags off the exception data, if they exist
      if (exception != null && exception.Data != null)
      {
        if (exception.Data.Contains("Tags"))
        {
          object tagsData = exception.Data["Tags"];
          tags.AddRange(ParseTagsData(tagsData));
        }
        if (exception.Data.Contains("tags"))
        {
          object tagsData = exception.Data["tags"];
          tags.AddRange(ParseTagsData(tagsData));
        }
      }

      if (logEvent.Properties.Count > 0)
      {
        if (logEvent.Properties.ContainsKey("Tags"))
        {
          object tagsData = logEvent.Properties["Tags"];
          tags.AddRange(ParseTagsData(tagsData));
        }
        if (logEvent.Properties.ContainsKey("tags"))
        {
          object tagsData = logEvent.Properties["tags"];
          tags.AddRange(ParseTagsData(tagsData));
        }
      }

      if (!string.IsNullOrWhiteSpace(Tags))
      {
        var tagsData = SplitValues(Tags);
        tags.AddRange(tagsData);
      }

      return tags;
    }

    private RaygunClient CreateRaygunClient()
    {
      RaygunClient client = null;
      string apiKey = _apiKey?.Render(LogEventInfo.CreateNullEvent()) ?? string.Empty;
      if (!string.IsNullOrEmpty(apiKey))
      {
        client = new RaygunClient(apiKey);
      }
      else
      {
#if NET45
        client = new RaygunClient();
#else
        throw new ArgumentException("NLog RaygunTarget requires valid ApiKey property", nameof(ApiKey));
#endif
      }

      if (UseExecutingAssemblyVersion)
      {
        client.ApplicationVersion = GetExecutingAssemblyVersion();
      }

      if (string.IsNullOrEmpty(client.ApplicationVersion))
      {
        if (_applicationVersion != null)
        {
          client.ApplicationVersion = _applicationVersion.Render(LogEventInfo.CreateNullEvent());
        }
        else
        {
          client.ApplicationVersion = GetExecutingAssemblyVersion();
        }
      }

      if (IgnoreFormFieldNames != null)
        client.IgnoreFormFieldNames(SplitValues(IgnoreFormFieldNames));
      if (IgnoreCookieNames != null)
        client.IgnoreCookieNames(SplitValues(IgnoreCookieNames));
      if (IgnoreHeaderNames != null)
        client.IgnoreHeaderNames(SplitValues(IgnoreHeaderNames));
      if (IgnoreServerVariableNames != null)
        client.IgnoreServerVariableNames(SplitValues(IgnoreServerVariableNames));
      if (_isRawDataIgnored.HasValue)
        client.IsRawDataIgnored = _isRawDataIgnored.Value;
      return client;
    }

    private static IEnumerable<string> ParseTagsData(object tagsData)
    {
      IEnumerable<string> tagsCollection = tagsData as IEnumerable<string>;
      if (tagsCollection != null)
      {
        return tagsCollection;
      }
      else if (tagsData is string)
      {
        return SplitValues((string)tagsData);
      }
      else
      {
        return System.Linq.Enumerable.Empty<string>();
      }
    }

    private static string[] SplitValues(string input)
    {
      if (!string.IsNullOrWhiteSpace(input))
      {
        return input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
      }

      return new string[0];
    }

    private static string GetExecutingAssemblyVersion()
    {
      try
      {
        var assembly = System.Reflection.Assembly.GetEntryAssembly();
        return assembly != null ? assembly.GetName().Version.ToString() : null;
      }
      catch (Exception)
      {
        return null;
      }
    }
  }
}