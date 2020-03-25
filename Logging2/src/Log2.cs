using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Logging2
{
    public class Log2 : ILog
    {
        private object m_SyncObj = new object();
        // public static OutputPoints DefaultOutPoints;
        // public LogLevel MinLogLevel{get;set;} = LogLevel.Low;
        // public LogDestination DefaultDestination{get;set;} = LogDestination.Screen | LogDestination.LogFile | LogDestination.Event;
        public static string CompanyName;
        public static D_LoggingRelatedAsseblies p_LoggingRelatedAsseblies;
        // public LogBase()
        // {
        //     p_LoggingRelatedAsseblies = new D_LoggingRelatedAsseblies(LoggingRelatedAsseblies);
        // }
        public void Logging(LogMessage2 LogMes, OutputPoints2 OutPoints = null)
        {
            OutputPoints2 op = OutPoints ?? LogMes.OP;
            if (LogMes == null || op == null)
                return;
            try
            {
                if (LogMes.Destination == null)
                    LogMes.Destination = op.DefaultDestination;

                bool Destination_Db = (LogMes.Destination & LogDestination.Db) == LogDestination.Db;
                bool Destination_LogFile = (LogMes.Destination & LogDestination.LogFile) == LogDestination.LogFile;
                bool Destination_Screen = (LogMes.Destination & LogDestination.Screen) == LogDestination.Screen;
                bool Destination_Event = (LogMes.Destination & LogDestination.Event) == LogDestination.Event;

                if (Destination_Db && op.OutputToDb != null && LogMes.Level >= op.MinLogLevel)
                    op.OutputToDb(LogMes);
                if (Destination_LogFile && op.OutputToFile != null && LogMes.Level >= op.MinLogLevel)
                    op.OutputToFile(LogMes);
                if (Destination_Screen && op.OutputToScreen != null && LogMes.Level >= op.MinLogLevel)
                    op.OutputToScreen(LogMes);
                if (Destination_Event && op.OutputToEventLog != null && LogMes.Level >= op.MinLogLevel)
                    op.OutputToEventLog(LogMes);
            }
            catch (Exception ex)
            {
                if (op.OutputToEventLog != null)
                    op.OutputToEventLog(new LogMessage2() { Mes = "Error logging: " + ex.Message, Level = LogLevel.Critical });
            }
        }
        public void Logging(OutputPoints2 OutPoints, string Mes,
            string ClientId = null, LogLevel? Level = null, LogEventType? EventType = null, LogDestination? Destination = null)
        {
            if (string.IsNullOrEmpty(Mes) || OutPoints == null)
                return;
            Logging(new LogMessage2()
            {
                OP = OutPoints,
                ClientId = ClientId,
                Mes = Mes,
                Destination = Destination,
                Level = Level ?? OutPoints.MinLogLevel,
                EventType = EventType
            });
        }
        // public void Logging(OutputPoints0 OutPoints, string Mes, LogLevel? Level, LogEventType? EventType, LogDestination? Destination)
        // {
        //     Logging(OutPoints, "", Mes, Level, EventType,Destination);
        // }
        public void Logging(IList<LogMessage2> LogMesCollection, OutputPoints2 OutPoints = null)
        {
            if (LogMesCollection == null)// || OutPoints == null)
                return;
            lock (m_SyncObj)
            {
                foreach (LogMessage2 LogMes in LogMesCollection)
                {
                    LogMes.OP = LogMes.OP ?? OutPoints;
                    Logging(LogMes);
                }
            }
        }
        public void LoggingAndClear(IList<LogMessage2> LogMesCollection, bool WithoutDelimiterLine, OutputPoints2 OutPoints = null)
        {
            if (LogMesCollection != null)
                if (!WithoutDelimiterLine
                && LogMesCollection.Count > 0 && LogMesCollection[LogMesCollection.Count - 1].Mes != OutputPoints2.DelimiterLine)
                    LogMesCollection.Add(new LogMessage2() { Mes = OutputPoints2.DelimiterLine });

            Logging(LogMesCollection, OutPoints);
            LogMesCollection.Clear();
        }
        public void Logging(IList<string> StrLogMessages, OutputPoints2 OutPoints)
        {
            if (StrLogMessages == null || OutPoints == null)
                return;
            List<LogMessage2> LogMesCollection = new List<LogMessage2>();
            for (int i = 0; i < StrLogMessages.Count; i++)
            {
                if (string.IsNullOrEmpty(StrLogMessages[i]))
                    continue;
                LogMessage2 LM = StrLogMes_to_LogMessage(StrLogMessages[i]);
                LogMesCollection.Add(LM);
            }
            Logging(LogMesCollection, OutPoints);
        }

        public LogMessage2 StrLogMes_to_LogMessage(string StrLogMessage)
        {
            DateTime curDate = DateTime.Now;
            string curMes = StrLogMessage;

            int delimIndex = curMes.IndexOf(OutputPoints2.DateDelimiterInString);
            if (delimIndex != -1 && DateTime.TryParseExact(curMes.Substring(0, delimIndex),
                    OutputPoints2.DateFormatInString, System.Globalization.CultureInfo.CurrentCulture,
                    System.Globalization.DateTimeStyles.None, out curDate))
                curMes = curMes.Substring(delimIndex + OutputPoints2.DateDelimiterInString.Length);

            return new LogMessage2() { Date = curDate, Mes = curMes };
        }

        public IList<Assembly> GetRelevantAssemblyList(string CompanyName)
        {
            IList<Assembly> res = new List<Assembly>();
            Assembly EntryAssembly = Assembly.GetEntryAssembly();
            if (EntryAssembly != null)
            {
                res.Add(EntryAssembly);
                foreach (AssemblyName an in Assembly.GetEntryAssembly().GetReferencedAssemblies())
                    try
                    {
                        Assembly ass = Assembly.Load(an);
                        if (!res.Contains(ass))
                            res.Add(ass);
                    }
                    catch { }
            }

            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                if (!res.Contains(ass))
                    res.Add(ass);

            for (int i = res.Count - 1; i >= 0; i--)
                try
                {
                    AssemblyCompanyAttribute CompanyAttr = Attribute.GetCustomAttribute(
                        res[i], typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
                    if (CompanyAttr == null || !CompanyAttr.Company.Contains(CompanyName))
                        res.RemoveAt(i);
                }
                catch { }
            return res;
        }
        public IList<string> PrepareToLoggingRelatedAsseblies(string CompanyName)
        {
            List<string> Lines = new List<string>();
            IList<Assembly> AssebliesList = GetRelevantAssemblyList(CompanyName);
            Lines.Add(OutputPoints2.DelimiterLine);
            Lines.Add("Versions of the modules:");
            foreach (Assembly ass in AssebliesList)
                Lines.Add(Path.GetFileName(ass.Location) + " : ver. " + ass.GetName().Version.ToString());
            Lines.Add(OutputPoints2.DelimiterLine);
            return Lines;
        }
        public void LoggingRelatedAsseblies(OutputPoints2 OutPoints, string companyName = null)
        {
            if (string.IsNullOrEmpty(companyName))
                companyName = CompanyName;
            IList<string> Lines = PrepareToLoggingRelatedAsseblies(companyName);
            foreach (string Line in Lines)
                Logging(OutPoints, Line, null, null, null, null);
        }
    }
}