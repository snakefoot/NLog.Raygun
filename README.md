# NLog.Raygun [![NuGet](https://img.shields.io/nuget/v/NLog.Raygun.svg)](https://www.nuget.org/packages/NLog.Raygun)
[NLog](http://nlog-project.org/) extension for pushing application exceptions to [Raygun](https://raygun.com/)

## Configuration

You will need to add Raygun as a target within your NLog configuration file.

### NLog Configuration

Your `NLog.config` should look something like this:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <!-- Add the assembly -->
    <add assembly="NLog.Raygun"/>
  </extensions>
  <targets>
    <!-- Set up the target (Avoid using async=true or AsyncWrapper) -->
    <target name="RaygunTarget" 
            type="Raygun" 
            ApiKey="" 
            Tags="" 
            IncludeEventProperties="true" 
            IncludeBreadcrumbMessages="false"
            IgnoreFormFieldNames="" 
            IgnoreCookieNames="" 
            IgnoreServerVariableNames="" 
            IgnoreHeaderNames="" 
            IgnoreQueryParameterNames=""
            UserIdentityInfo="" 
            UseExecutingAssemblyVersion="false" 
            ApplicationVersion="" 
            layout="${uppercase:${level}} ${message} ${exception:format=ToString,StackTrace}${newline}">
      <contextproperty name="RenderedLogMessage" layout="${message}" />
      <contextproperty name="LogMessageTemplate" layout="${message:raw=true}" />
      <contextproperty name="Logger" layout="${logger}" />
      <contextproperty name="ThreadId" layout="${threadid}" />
    </target>
  </targets>
  <rules>
    <!-- Set up the logger. -->
    <logger name="*" minlevel="Error" writeTo="RaygunTarget" />
  </rules>
</nlog>
```

### Settings

| Setting        | Description |
| :------------- | :---------- |
| ApiKey | Your Raygun application API key |
| Tags | Tags you want to send in with every exception
| IncludeEventProperties | Include properties from NLog LogEvent. Default is ```true```
| IncludeScopeProperties | Include properties from NLog ScopeContext. Default is ```false```
| IncludeMdlc | Include properties from NLog Mapped Diagnostic Logical Context (MDLC). Default is ```false```
| IncludeBreadcrumbMessages | Convert informational LogEvents without Exceptions into Breadcrumbs. Default is ```false```
| IsRawDataIgnored | RawData from web requests is ignored. Default is ```false```
| IgnoreFormFieldNames | Form fields you wish to ignore, eg passwords and credit cards
| IgnoreCookieNames | Cookies you wish to ignore, eg user tokens
| IgnoreServerVariableNames | Server variables you wish to ignore, eg sessions
| IgnoreHeaderNames | HTTP request headers to ignore, eg API keys
| IgnoreQueryParameterNames | HTTP request query parameters to ignore
| IgnoreSensitiveFieldNames | Remove sensitive information from any of the above collections (Fields, Cookies, Headers, QueryParameters, RawData)
| UserIdentityInfo | Explicitly defines lookup of user identity for Raygun events, ie. NLog layout renderer `${windows-identity}`
| UseExecutingAssemblyVersion | Attempt to get the executing assembly version, or root ASP.Net assembly version for Raygun events. Default is ```false```
| ApplicationVersion | Explicitly defines an application version for Raygun events. This will be ignored if UseExecutingAssemblyVersion is set to true and returns a value

## Tags

You can add tags per exception by putting a List<string> of tags into your Exception.Data array using the `Tags` key.

```csharp
var e = new Exception("Test Exception");
e.Data["Tags"] = new List<string> { "Tester123" }; 
```
