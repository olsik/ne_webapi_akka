using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Logging2
{
    public class OutputPoints2
    {
        public static string LogDateFormat = "dd-MM-yyyy HH:mm:ss.fff | ";
        public static string LogDateFormatOnly = "dd-MM-yyyy HH:mm:ss.fff";
        public static string DelimiterLine = "-------------------------";
        public static string DateDelimiterInString = "|||";
        public static string DateFormatInString = "yyyyMMddHHmmssfff";
        public static MessageSerializeType DefaultSerializeType = MessageSerializeType.DateAndMes;
        private MessageSerializeType? m_SerializeType;
        public MessageSerializeType SerializeType
        {
            get { return m_SerializeType.HasValue ? m_SerializeType.Value : DefaultSerializeType; }
            set { m_SerializeType = value; }
        }
        public LogLevel MinLogLevel { get; set; } = LogLevel.Low;
        public LogDestination DefaultDestination { get; set; } = LogDestination.Screen | LogDestination.LogFile | LogDestination.Event;
        public D_OuptutPoint OutputToDb;
        public D_OuptutPoint OutputToFile;
        public D_OuptutPoint OutputToScreen;
        public D_OuptutPoint OutputToEventLog;

        protected Uri m_Uri = null;
        protected string m_LogPrefix;
        protected string m_FullExeName;
        protected string m_LogDir;

        protected string m_EventLogName = "Application";

        protected static object ToFile_Synchronization = new object();
        // protected static object ToEventLog_Synchronization = new object();
        // public static bool WriteStationMessageToRegularLog = true;

        public string LogPerion;
        private DateTime StartTime;
        public Uri p_Uri
        {
            get
            {
                if (m_Uri == null)
                {
                    if (System.Reflection.Assembly.GetEntryAssembly() != null)
                        m_Uri = new Uri(System.Reflection.Assembly.GetEntryAssembly().CodeBase);
                    else
                        m_Uri = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
                }
                return m_Uri;
            }
            set { m_Uri = value; }
        }
        public string p_LogPrefix
        {
            get
            {
                if (String.IsNullOrEmpty(m_LogPrefix))
                    m_LogPrefix = Path.GetFileNameWithoutExtension(p_FullExeName);
                return m_LogPrefix;
            }
            set { m_LogPrefix = value; }
        }
        public string p_FullExeName
        {
            get
            {
                if (String.IsNullOrEmpty(m_FullExeName))
                    m_FullExeName = p_Uri.LocalPath;
                return m_FullExeName;
            }
            set { m_FullExeName = value; }
        }
        public string p_LogDir
        {
            get
            {
                if (String.IsNullOrEmpty(m_LogDir))
                    m_LogDir = Path.Combine(Path.GetDirectoryName(p_FullExeName), "Log");
                return m_LogDir;
            }
        }
        public static Encoding p_FileEncoding { get; set; }
        public static bool p_UsingEncodingForFileOut = false;
        public static void UsingEncodingForFileOut(int EncodingPage)
        {
            p_UsingEncodingForFileOut = true;

            if (EncodingPage == -1)
                p_FileEncoding = Encoding.Unicode;
            else if (EncodingPage == 0)
                p_FileEncoding = Encoding.ASCII;
            else if (EncodingPage > 0)
                try
                {
                    p_FileEncoding = Encoding.GetEncoding(EncodingPage);
                    p_UsingEncodingForFileOut = true;
                }
                catch { }
        }
        public OutputPoints2(string LogPerion = "1d", string LogDir = null, string LogPrefix = null)
        {
            this.LogPerion = LogPerion;
            this.m_LogDir = LogDir;
            this.m_LogPrefix = LogPrefix;
            StartTime = DateTime.Now.Date;
        }
        public OutputPoints2 Clone(OutputPoints2 res = null)
        {
            if (res == null)
                res = new OutputPoints2();
            res.OutputToDb = OutputToDb;
            res.OutputToFile = OutputToFile;
            res.OutputToScreen = OutputToScreen;
            res.OutputToEventLog = OutputToEventLog;

            res.p_LogPrefix = p_LogPrefix;
            res.p_FullExeName = p_FullExeName;

            return res;
        }

        public void CreatePoint_OutputToConsoleScreen(string exeName = null, string eventLogName = null)
        {
            this.OutputToScreen = new D_OuptutPoint(OutputToConsole);
        }
        protected bool OutputToConsole(LogMessage2 LogMes)
        {
            Console.WriteLine(LogMes.ToString(SerializeType));
            return true;
        }

        public virtual void CreatePoint_OutputToEventLog(string exeName = null, string eventLogName = null)
        {
            throw new NotSupportedException();
        }

        protected virtual bool SaveToEventLog(LogMessage2 LogMes)
        {
            throw new NotSupportedException();
        }

        public void CreatePoint_OutputToFile(string LogPerion = "1d", string LogDir = null, string LogPrefix = null)
        {
            if (!string.IsNullOrEmpty(LogPerion))
                this.LogPerion = LogPerion;
            if (!string.IsNullOrEmpty(LogDir))
                this.m_LogDir = LogDir;
            if (!string.IsNullOrEmpty(LogPrefix))
                this.m_LogPrefix = LogPrefix;

            if (OutputPoints2.p_UsingEncodingForFileOut)
                this.OutputToFile = new D_OuptutPoint(SaveToFile_WithEncoding);
            else
                this.OutputToFile = new D_OuptutPoint(SaveToFile);
        }

        private Tuple<TimeSpan, string> GetPeriodTimeSpan()
        {
            Tuple<TimeSpan, string> res = new Tuple<TimeSpan, string>(TimeSpan.FromDays(1), "d");
            string confValue = LogPerion.ToLower();
            int pos;
            // if (0 > (pos = confValue.IndexOf('s')))//like 235s
            if (0 > (pos = confValue.IndexOf('m')))//like 12m
                if (0 > (pos = confValue.IndexOf('h')))//like 10h
                    pos = confValue.IndexOf('d');//like 3d

            int val = 0;
            if (!(pos > 0 && int.TryParse(confValue.Substring(0, pos), out val)))
                // {
                // message = "Incorrect format of period ('" + confValue + "')";
                return res;
            // }

            switch (confValue[pos])
            {
                // case 's':
                //     res = TimeSpan.FromSeconds(val);
                //     break;
                case 'm':
                    res = new Tuple<TimeSpan, string>(TimeSpan.FromMinutes(val), "m");
                    break;
                case 'h':
                    res = new Tuple<TimeSpan, string>(TimeSpan.FromHours(val), "h");
                    break;
                case 'd':
                    res = new Tuple<TimeSpan, string>(TimeSpan.FromDays(val), "d");
                    break;
            }
            return res;
        }

        protected virtual List<string> GetLogFileName(LogLevel Level, string SourceIdent = "")
        {
            List<string> res = new List<string>();
            if (string.IsNullOrEmpty(SourceIdent))
                SourceIdent = p_LogPrefix;

            int MinsFromStart = (int)(DateTime.Now - this.StartTime).TotalMinutes;
            Tuple<TimeSpan, string> LogPeriodTs = GetPeriodTimeSpan();
            int LogPeriodMin = (int)LogPeriodTs.Item1.TotalMinutes;
            int CurPeriodIndex = MinsFromStart / LogPeriodMin;
            int t = CurPeriodIndex * LogPeriodMin;
            DateTime CurLogStart = this.StartTime.Add(TimeSpan.FromMinutes(CurPeriodIndex * LogPeriodMin));

            string PeriodPart;
            switch (LogPeriodTs.Item2)
            {
                case "m":
                    PeriodPart = CurLogStart.ToString("_yyyyMMddHHmm");
                    break;
                case "h":
                    PeriodPart = CurLogStart.ToString("_yyyyMMddHH");
                    break;
                case "d":
                default:
                    PeriodPart = CurLogStart.ToString("_yyyyMMdd");
                    break;
            };

            if (Level != LogLevel.None)
                res.Add(Path.Combine(p_LogDir, SourceIdent + PeriodPart + ".log"));
            if (Level > LogLevel.Medium)
                res.Add(Path.Combine(p_LogDir, SourceIdent + "_Errors.log"));

            return res;
        }

        protected bool SaveToFile(LogMessage2 LogMes)
        {
            bool res = false;
            if (LogMes != null)
                try
                {
                    if (string.IsNullOrEmpty(LogMes.Mes))
                        return true;
                    // LogMes.Mes = "[OutputPoints0.SaveToFile] Mes IsNullOrEmpty";
                    List<string> LogFileNames = GetLogFileName(LogMes.Level, LogMes.SourceIdent);
                    lock (ToFile_Synchronization)
                    {
                        foreach (string LogFileName in LogFileNames)
                        {
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(LogFileName));

                                using (FileStream fs = new FileStream(LogFileName, FileMode.OpenOrCreate))
                                {
                                    fs.Seek(0, SeekOrigin.End);
                                    using (BinaryWriter binWriter = new BinaryWriter(fs))
                                    {
                                        string Line = LogMes.ToString(SerializeType) + Environment.NewLine;
                                        byte[] bLine = new byte[Line.Length];
                                        for (int i = 0; i < Line.Length; i++)
                                        {
                                            byte[] b = BitConverter.GetBytes(Line[i]);
                                            bLine[i] = b[0];
                                        }
                                        binWriter.Write(bLine);
                                    }
                                }
                            }
                        }
                    }
                    res = true;
                }
                catch (Exception ex)
                {
                    LogMessage2 exLogMes = new LogMessage2()
                    {
                        Mes = "Error saving to log file: " + ex.Message + " | Original message: " + LogMes.ToString(SerializeType),
                        Level = LogLevel.Critical,
                        EventType = LogEventType.Filesystem
                    };
                    if (this.OutputToEventLog != null)
                        this.OutputToEventLog(exLogMes);
                    if (this.OutputToScreen != null)
                        this.OutputToScreen(exLogMes);
                }
            return res;
        }
        protected virtual bool SaveToFile_WithEncoding(LogMessage2 LogMes)
        {
            bool res = false;

            if (p_FileEncoding == null)
                p_FileEncoding = Encoding.Unicode;

            if (LogMes != null)
                try
                {
                    if (string.IsNullOrEmpty(LogMes.Mes))
                        return true;
                    // LogMes.Mes = "[OutputPoints0.SaveToFile_WithEncoding] Mes IsNullOrEmpty";
                    List<string> LogFileNames = GetLogFileName(LogMes.Level, LogMes.SourceIdent);
                    lock (ToFile_Synchronization)
                    {
                        foreach (string LogFileName in LogFileNames)
                        {
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(LogFileName));
                                if (!File.Exists(LogFileName))
                                {
                                    using (File.Create(LogFileName)) { }

                                    if (Log2.p_LoggingRelatedAsseblies != null)
                                    {
                                        OutputPoints2 RelatedAsseblies_OP = this.Clone();
                                        RelatedAsseblies_OP.OutputToDb = RelatedAsseblies_OP.OutputToScreen
                                            = RelatedAsseblies_OP.OutputToEventLog = null;

                                        Log2.p_LoggingRelatedAsseblies(RelatedAsseblies_OP);
                                    }
                                }

                                using (var sw = new StreamWriter(LogFileName, true, p_FileEncoding, 1024))
                                {
                                    sw.WriteLine(LogMes.ToString(SerializeType));
                                }
                            }
                        }
                    }
                    res = true;
                }
                catch (Exception ex)
                {
                    LogMessage2 exLogMes = new LogMessage2()
                    {
                        Mes = "Error saving to log file: " + ex.Message + " | Original message: " + LogMes.ToString(SerializeType),
                        Level = LogLevel.Critical,
                        EventType = LogEventType.Filesystem
                    };
                    if (this.OutputToEventLog != null)
                        this.OutputToEventLog(exLogMes);
                    if (this.OutputToScreen != null)
                        this.OutputToScreen(exLogMes);
                }
            return res;
        }

    }
}