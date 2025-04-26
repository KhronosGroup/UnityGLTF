using System;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityGLTF
{
    public class LogCollector : ILogHandler
    {
        private readonly StringBuilder sb = new StringBuilder();

        private string LogTypeToLog(LogType logType)
        {
#if UNITY_EDITOR
            // create strings with <color> tags
            switch (logType)
            {
                case LogType.Error:
                    return "<color=red>[" + logType + "]</color>";
                case LogType.Assert:
                    return "<color=red>[" + logType + "]</color>";
                case LogType.Warning:
                    return "<color=yellow>[" + logType + "]</color>";
                case LogType.Log:
                    return "[" + logType + "]";
                case LogType.Exception:
                    return "<color=red>[" + logType + "]</color>";
                default:
                    return "[" + logType + "]";
            }
#else
				return "[" + logType + "]";
#endif
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args) => sb.AppendLine($"{LogTypeToLog(logType)} {string.Format(format, args)} [Context: {context}]");
        public void LogException(Exception exception, Object context) => sb.AppendLine($"{LogTypeToLog(LogType.Exception)} {exception} [Context: {context}]");

        public void LogAndClear(string format = "{0}")
        {
            if (sb.Length > 0)
            {
                var str = sb.ToString();
#if UNITY_2019_1_OR_NEWER
                var logType = LogType.Log;
#if UNITY_EDITOR
                if (str.IndexOf("[Error]", StringComparison.Ordinal) > -1 ||
                    str.IndexOf("[Exception]", StringComparison.Ordinal) > -1 ||
                    str.IndexOf("[Assert]", StringComparison.Ordinal) > -1)
                    logType = LogType.Error;
                else if (str.IndexOf("[Warning]", StringComparison.Ordinal) > -1)
                    logType = LogType.Warning;
#endif
                Debug.LogFormat(logType, LogOption.NoStacktrace, null, format, sb.ToString());
#else
					Debug.Log(string.Format("Export Messages:" + "\n{0}", str));
#endif
            }
            sb.Clear();
        }
    }
}