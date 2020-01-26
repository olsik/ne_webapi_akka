using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;

namespace Logging2
{
    public enum LogLevel { None = 0, Low = 1, LowWithEvent = 2, Medium = 3, Hi = 4, Critical = 5 };
    [Flags]
    public enum LogDestination { None = 0, Db = 1, LogFile = 2, Screen = 4, Event = 8 };
    public enum LogEventType { None, Communication, Database, Internal, Integration, Filesystem };
    public enum SaveToFileMode { RegularMode = 0, OneRowMode = 1, };
    public enum MessageSerializeType { DateAndMes, Xml, };

    public delegate bool D_OuptutPoint(LogMessage2 LogMes);
    public delegate void D_LoggingRelatedAsseblies(OutputPoints2 OutPoints, string companyName = null);

    interface ILog
    {
        // LogLevel MinLogLevel {get;set;}
        // LogDestination DefaultDestination {get;set;}

        void Logging(LogMessage2 LogMes, OutputPoints2 OutPoints = null);
        void Logging(OutputPoints2 OutPoints, string Mes,
            string ClientId = null, LogLevel? Level = null, LogEventType? EventType = null, LogDestination? Destination = null);
        void Logging(IList<LogMessage2> LogMesCollection, OutputPoints2 OutPoints = null);
        void LoggingAndClear(IList<LogMessage2> LogMesCollection, bool WithoutDelimiterLine, OutputPoints2 OutPoints = null);
        void Logging(IList<string> StrLogMessages, OutputPoints2 OutPoints);
        LogMessage2 StrLogMes_to_LogMessage(string StrLogMessage);
        IList<Assembly> GetRelevantAssemblyList(string CompanyName);
        void LoggingRelatedAsseblies(OutputPoints2 OutPoints, string CompanyName);
    }
}