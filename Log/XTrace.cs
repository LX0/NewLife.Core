using System;
using NewLife.Configuration;

//namespace XLog
//{
//    /// <summary>
//    /// ��־�࣬�������ٵ��Թ���
//    /// </summary>
//    public class XTrace : NewLife.Log.XTrace { }
//}

namespace NewLife.Log
{
    /// <summary>
    /// ��־�࣬�������ٵ��Թ���
    /// </summary>
    public class XTrace
    {
        #region д��־
        private static TextFileLog Log = TextFileLog.Create(Config.GetConfig<String>("NewLife.LogPath"));
        /// <summary>
        /// ��־·��
        /// </summary>
        public static String LogPath { get { return Log.LogPath; } }

        /// <summary>
        /// �����־
        /// </summary>
        /// <param name="msg">��Ϣ</param>
        public static void WriteLine(String msg)
        {
            Log.WriteLine(msg);
        }

        /// <summary>
        /// ��ջ���ԡ�
        /// �����ջ��Ϣ�����ڵ���ʱ������������ġ�
        /// ����������ɴ�����־�������á�
        /// </summary>
        public static void DebugStack()
        {
            Log.DebugStack();
        }

        /// <summary>
        /// ��ջ���ԡ�
        /// </summary>
        /// <param name="maxNum">��󲶻��ջ������</param>
        public static void DebugStack(int maxNum)
        {
            Log.DebugStack(maxNum);
        }

        /// <summary>
        /// д��־�¼����󶨸��¼���XTrace�����ٰ���־д����־�ļ���ȥ��
        /// </summary>
        public static event EventHandler<WriteLogEventArgs> OnWriteLog
        {
            add { Log.OnWriteLog += value; }
            remove { Log.OnWriteLog -= value; }
        }
        //public static event EventHandler<WriteLogEventArgs> OnWriteLog;

        /// <summary>
        /// д��־
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            Log.WriteLine(format, args);
        }
        #endregion

        #region ����
        private static Boolean? _Debug;
        /// <summary>�Ƿ���ԡ��������ָ����ֵ����ֻ��ʹ�ô���ָ����ֵ������ÿ�ζ���ȡ���á�</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;
                //String str = ConfigurationManager.AppSettings["NewLife.Debug"];
                //if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["Debug"];
                //if (String.IsNullOrEmpty(str)) return false;
                //if (str == "1") return true;
                //if (str == "0") return false;
                //if (str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                //if (str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                //return false;

                return Config.GetConfig<Boolean>("NewLife.Debug", Config.GetConfig<Boolean>("Debug", false));
            }
            set { _Debug = value; }
        }
        #endregion
    }
}