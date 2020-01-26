using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Logging2
{
    [XmlRoot("LM")]
    public class LogMessage2
    {
        [XmlIgnore]
        public OutputPoints2 OP;

        [XmlAttribute(AttributeName = "Date")]
        public virtual string DateStr
        {
            get { return Date.ToString(OutputPoints2.LogDateFormatOnly); }
            set
            {
                DateTime CurDate;
                if (DateTime.TryParseExact(value,
                        OutputPoints2.LogDateFormatOnly, System.Globalization.CultureInfo.CurrentCulture,
                        System.Globalization.DateTimeStyles.None, out CurDate))
                    Date = CurDate;
            }
        }
        [XmlAttribute]
        public virtual string ClientId { get; set; }
        private string m_Mes;
        [XmlAttribute]
        public string Mes
        {
            get
            {
                // switch(SerealizeType)
                // {
                //     case MessageSerealizeType.DateAndMes:
                //         return string.IsNullOrEmpty(ClientId) ? m_Mes : "ClientId: " + ClientId.ToString() + "; " + m_Mes;
                //     case MessageSerealizeType.Xml:
                //     default:
                return m_Mes;
                // }
            }
            set { m_Mes = value; }
        }
        //     public virtual string Mes { get; set; }
        [XmlIgnore]
        public virtual DateTime Date { get; set; }
        [XmlAttribute(AttributeName = "Src")]
        public virtual string SourceIdent { get; set; }
        [XmlAttribute]
        public virtual LogLevel Level { get; set; }
        [XmlIgnore]
        public virtual LogDestination? Destination { get; set; }
        [XmlIgnore]
        public virtual LogEventType? EventType { get; set; }
        [XmlAttribute("EventType")]
        public string EventTypeStr { get { return EventType.HasValue ? EventType.Value.ToString() : null; } set { } }

        // [XmlIgnore]
        // public virtual string MesWithDat
        // {
        //     get
        //     {
        //         try { return Date.ToString(OutputPoints2.LogDateFormat) + Mes; }
        //         catch { return Mes; }
        //     }
        // }
        //     [XmlIgnore]
        //     public virtual string MesForEvent
        //     {
        //         get
        //         {
        //             string res = Environment.NewLine + Mes;
        //             if (EventType != LogEventType.None)
        //                 res = " type: " + EventType.ToString() + res;
        //             if (Level > LogLevel.Low)
        //                 res = " severity:" + ((int)Level).ToString() + res;
        //             res = "Roseman" + res;
        //             return res;
        //         }
        //     }
        // private MessageSerealizeType? m_SerealizeType;
        // [XmlIgnore]
        // public MessageSerealizeType SerealizeType
        // {
        //     set { m_SerealizeType = value; }
        //     get
        //     {
        //         if (!m_SerealizeType.HasValue)
        //             return OutputPoints2.SerealizeType;
        //         else
        //             return m_SerealizeType.Value;
        //     }
        // }
        public string ToString(MessageSerializeType SerializeType)
        {
            switch (SerializeType)
            {
                case MessageSerializeType.DateAndMes:
                    return Date.ToString(OutputPoints2.LogDateFormat)
                        + (string.IsNullOrEmpty(ClientId) ? m_Mes : "ClientId: " + ClientId.ToString() + "; " + m_Mes);
                // return MesWithDat;
                case MessageSerializeType.Xml:
                    return SerializeClass2_WithException(this);
                default:
                    return "";
            }
        }
        //from CommonTools project
        private static string SerializeClass2_WithException(object cl, string RootText = null)
        {
            string res = ""; XmlSerializer serializer = null;
            if (cl != null)
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                if (!string.IsNullOrEmpty(RootText))
                {
                    XmlRootAttribute xRoot = new XmlRootAttribute();
                    xRoot.ElementName = RootText;
                    serializer = new XmlSerializer(cl.GetType(), xRoot);
                }
                else
                {
                    Type t = cl.GetType();
                    serializer = new XmlSerializer(cl.GetType());
                }

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;

                using (var stream = new StringWriter())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, cl, ns);
                    res = stream.ToString();
                }
            }
            return res;
        }

        public LogMessage2()
        {
            Date = DateTime.Now;
            Level = LogLevel.Low;
            EventType = LogEventType.None;
            // Destination = LogBase.DefaultDestination;
        }
    }
}
