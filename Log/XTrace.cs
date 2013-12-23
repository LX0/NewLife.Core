using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NewLife.Configuration;
using NewLife.Exceptions;
using NewLife.Reflection;

namespace NewLife.Log
{
    /// <summary>��־�࣬�������ٵ��Թ���</summary>
    /// <remarks>
    /// �þ�̬�����д��־��д����ջ��Dump�����ڴ�ȵ��Թ��ܡ�
    /// 
    /// Ĭ��д��־���ı��ļ�����ͨ���޸�<see cref="Log"/>������������־�����ʽ��
    /// ���ڿ���̨���̣�����ֱ��ͨ��<see cref="UseConsole"/>����������־����ض���Ϊ����̨��������ҿ���Ϊ��ͬ�߳�ʹ�ò�ͬ��ɫ��
    /// </remarks>
    public static class XTrace
    {
        #region д��־
        /// <summary>�ı��ļ���־</summary>
        private static ILog _Log;
        /// <summary>��־�ṩ�ߣ�Ĭ��ʹ���ı��ļ���־</summary>
        public static ILog Log { get { InitLog(); return _Log; } set { _Log = value; } }

        /// <summary>�����־</summary>
        /// <param name="msg">��Ϣ</param>
        public static void WriteLine(String msg)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(msg, null, true));

            Log.Info(msg);
        }

        /// <summary>д��־</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteLine(String format, params Object[] args)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(String.Format(format, args), null, true));

            Log.Info(format, args);
        }

        /// <summary>����쳣��־</summary>
        /// <param name="ex">�쳣��Ϣ</param>
        //[Obsolete("����֧�֣�")]
        public static void WriteException(Exception ex)
        {
            InitLog();
            if (OnWriteLog != null) OnWriteLog(null, WriteLogEventArgs.Current.Set(null, ex, true));

            Log.Error("{0}", ex);
        }

        ///// <summary>����쳣��־</summary>
        ///// <param name="ex">�쳣��Ϣ</param>
        //public static void WriteExceptionWhenDebug(Exception ex) { if (Debug) WriteException(ex); }

        /// <summary>д��־�¼���</summary>
        [Obsolete("��ֱ��ʹ��CompositeLogʵ�ָ�ֵ��Log����")]
        public static event EventHandler<WriteLogEventArgs> OnWriteLog;
        #endregion

        #region ����
        static XTrace()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static object _lock = new object();

        /// <summary>
        /// 2012.11.05 �������ε��õ�ʱ������ͬ��BUG������LogΪ�յ����⡣
        /// </summary>
        static void InitLog()
        {
            if (_Log != null) return;

            lock (_lock)
            {
                if (_Log != null) return;
                _Log = TextFileLog.Create(null);
            }

            var asmx = AssemblyX.Create(Assembly.GetExecutingAssembly());
            WriteLine("{0} v{1} Build {2:yyyy-MM-dd HH:mm:ss}", asmx.Name, asmx.FileVersion, asmx.Compile);
        }
        #endregion

        #region ʹ�ÿ���̨���
        //private static Int32 init = 0;
        /// <summary>ʹ�ÿ���̨�����־��ֻ�ܵ���һ��</summary>
        /// <param name="useColor">�Ƿ�ʹ����ɫ��Ĭ��ʹ��</param>
        /// <param name="useFileLog">�Ƿ�ͬʱʹ���ļ���־��Ĭ��ʹ��</param>
        public static void UseConsole(Boolean useColor = true, Boolean useFileLog = true)
        {
            //if (init > 0 || Interlocked.CompareExchange(ref init, 1, 0) != 0) return;
            if (!Runtime.IsConsole) return;

            var clg = _Log as ConsoleLog;
            var ftl = _Log as TextFileLog;
            var cmp = _Log as CompositeLog;
            if (cmp != null)
            {
                ftl = cmp.Get<TextFileLog>();
                clg = cmp.Get<ConsoleLog>();
            }

            // ���ƿ���̨��־
            if (clg == null)
                clg = new ConsoleLog { UseColor = useColor };
            else
                clg.UseColor = useColor;

            if (!useFileLog)
            {
                Log = clg;
                if (ftl != null) ftl.Dispose();
            }
            else
            {
                if (ftl == null) ftl = TextFileLog.Create(null);
                Log = new CompositeLog(clg, ftl);
            }
        }
        #endregion

        #region ����WinForm�쳣
        private static Int32 initWF = 0;
        private static Boolean _ShowErrorMessage;
        //private static String _Title;

        /// <summary>����WinForm�쳣����¼��־����ָ���Ƿ���<see cref="MessageBox"/>��ʾ��</summary>
        /// <param name="showErrorMessage">��Ϊ�����쳣ʱ���Ƿ���ʾ��ʾ��Ĭ����ʾ</param>
        public static void UseWinForm(Boolean showErrorMessage = true)
        {
            _ShowErrorMessage = showErrorMessage;

            if (initWF > 0 || Interlocked.CompareExchange(ref initWF, 1, 0) != 0) return;
            //if (!Application.MessageLoop) return;

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var show = _ShowErrorMessage && Application.MessageLoop;
            var msg = "" + e.ExceptionObject;
            WriteLine(msg);
            if (e.IsTerminating)
            {
                //WriteLine("�쳣�˳���");
                Log.Fatal("�쳣�˳���");
                //XTrace.WriteMiniDump(null);
                if (show) MessageBox.Show(msg, "�쳣�˳�", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (show) MessageBox.Show(msg, "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            WriteException(e.Exception);
            if (_ShowErrorMessage && Application.MessageLoop) MessageBox.Show("" + e.Exception, "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region ʹ��WinForm�ؼ������־
        /// <summary>��WinForm�ؼ��������־����Ҫ���Ƿ�UI�̲߳���</summary>
        /// <remarks>���ǳ��ù��ܣ�Ϊ�˱�����ų��ù��ܣ�����UseWinForm��ͷ</remarks>
        /// <param name="control">Ҫ����־�����WinForm�ؼ�</param>
        /// <param name="useFileLog">�Ƿ�ͬʱʹ���ļ���־��Ĭ��ʹ��</param>
        /// <param name="maxLines">�������</param>
        public static void UseWinFormControl(this Control control, Boolean useFileLog = true, Int32 maxLines = 1000)
        {
            //if (handler != null)
            //    OnWriteLog += (s, e) => handler(control, e);
            //else
            //    OnWriteLog += (s, e) => UseWinFormWriteLog(control, e.ToString() + Environment.NewLine, maxLines);
            var clg = _Log as TextControlLog;
            var ftl = _Log as TextFileLog;
            var cmp = _Log as CompositeLog;
            if (cmp != null)
            {
                ftl = cmp.Get<TextFileLog>();
                clg = cmp.Get<TextControlLog>();
            }

            // ���ƿ���̨��־
            if (clg == null) clg = new TextControlLog();
            clg.Control = control;
            clg.MaxLines = maxLines;

            if (!useFileLog)
            {
                Log = clg;
                if (ftl != null) ftl.Dispose();
            }
            else
            {
                if (ftl == null) ftl = TextFileLog.Create(null);
                Log = new CompositeLog(clg, ftl);
            }
        }

        /// <summary>��WinForm�ؼ��������־����Ҫ���Ƿ�UI�̲߳���</summary>
        /// <remarks>���ǳ��ù��ܣ�Ϊ�˱�����ų��ù��ܣ�����UseWinForm��ͷ</remarks>
        /// <param name="control">Ҫ����־�����WinForm�ؼ�</param>
        /// <param name="msg">��־</param>
        /// <param name="maxLines">�������</param>
        [Obsolete("=>TextControlLog.WriteLog")]
        public static void UseWinFormWriteLog(this Control control, String msg, Int32 maxLines = 1000)
        {
            if (control == null) return;

            TextControlLog.WriteLog(control, msg, maxLines);
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

                try
                {
                    //return Config.GetConfig<Boolean>("NewLife.Debug", Config.GetConfig<Boolean>("Debug", false));
                    return Config.GetMutilConfig<Boolean>(false, "NewLife.Debug", "Debug");
                }
                catch { return false; }
            }
            set { _Debug = value; }
        }

        private static String _LogPath;
        /// <summary>�ı���־Ŀ¼</summary>
        public static String LogPath
        {
            get
            {
                if (_LogPath == null) _LogPath = Config.GetConfig<String>("NewLife.LogPath", "Log");
                return _LogPath;
            }
            set { _LogPath = value; }
        }

        private static String _TempPath;
        /// <summary>��ʱĿ¼</summary>
        public static String TempPath
        {
            get
            {
                if (_TempPath != null) return _TempPath;

                // ������TempPath������_TempPath����Ϊ��Ҫ��ʽ������һ��
                TempPath = Config.GetConfig<String>("NewLife.TempPath", "XTemp");
                return _TempPath;
            }
            set
            {
                _TempPath = value;
                if (String.IsNullOrEmpty(_TempPath)) _TempPath = "XTemp";
                if (!Path.IsPathRooted(_TempPath)) _TempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _TempPath);
                _TempPath = Path.GetFullPath(_TempPath);
            }
        }
        #endregion

        #region Dump
        /// <summary>д��ǰ�̵߳�MiniDump</summary>
        /// <param name="dumpFile">�����ָ�������Զ�д����־Ŀ¼</param>
        public static void WriteMiniDump(String dumpFile)
        {
            if (String.IsNullOrEmpty(dumpFile))
            {
                dumpFile = String.Format("{0:yyyyMMdd_HHmmss}.dmp", DateTime.Now);
                if (!String.IsNullOrEmpty(LogPath)) dumpFile = Path.Combine(LogPath, dumpFile);
            }

            MiniDump.TryDump(dumpFile, MiniDump.MiniDumpType.WithFullMemory);
        }

        /// <summary>
        /// ����Ҫʹ����windows 5.1 �Ժ�İ汾��������windows�ܾɣ��Ͱ�Windbg�����dll����������һ�㶼û�����⡣
        /// DbgHelp.dll ��windows�Դ��� dll�ļ� ��
        /// </summary>
        static class MiniDump
        {
            [DllImport("DbgHelp.dll")]
            private static extern Boolean MiniDumpWriteDump(IntPtr hProcess, Int32 processId, IntPtr fileHandle, MiniDumpType dumpType, ref MinidumpExceptionInfo excepInfo, IntPtr userInfo, IntPtr extInfo);

            /// <summary>MINIDUMP_EXCEPTION_INFORMATION</summary>
            struct MinidumpExceptionInfo
            {
                public UInt32 ThreadId;
                public IntPtr ExceptionPointers;
                public UInt32 ClientPointers;
            }

            [DllImport("kernel32.dll")]
            private static extern uint GetCurrentThreadId();

            public static Boolean TryDump(String dmpPath, MiniDumpType dmpType)
            {
                //ʹ���ļ��������� .dmp�ļ�
                using (var stream = new FileStream(dmpPath, FileMode.Create))
                {
                    //ȡ�ý�����Ϣ
                    var process = Process.GetCurrentProcess();

                    // MINIDUMP_EXCEPTION_INFORMATION ��Ϣ�ĳ�ʼ��
                    var mei = new MinidumpExceptionInfo();

                    mei.ThreadId = (UInt32)GetCurrentThreadId();
                    mei.ExceptionPointers = Marshal.GetExceptionPointers();
                    mei.ClientPointers = 1;

                    //������õ�Win32 API
                    var fileHandle = stream.SafeFileHandle.DangerousGetHandle();
                    var res = MiniDumpWriteDump(process.Handle, process.Id, fileHandle, dmpType, ref mei, IntPtr.Zero, IntPtr.Zero);

                    //��� stream
                    stream.Flush();
                    stream.Close();

                    return res;
                }
            }

            public enum MiniDumpType
            {
                None = 0x00010000,
                Normal = 0x00000000,
                WithDataSegs = 0x00000001,
                WithFullMemory = 0x00000002,
                WithHandleData = 0x00000004,
                FilterMemory = 0x00000008,
                ScanMemory = 0x00000010,
                WithUnloadedModules = 0x00000020,
                WithIndirectlyReferencedMemory = 0x00000040,
                FilterModulePaths = 0x00000080,
                WithProcessThreadData = 0x00000100,
                WithPrivateReadWriteMemory = 0x00000200,
                WithoutOptionalData = 0x00000400,
                WithFullMemoryInfo = 0x00000800,
                WithThreadInfo = 0x00001000,
                WithCodeSegs = 0x00002000
            }
        }
        #endregion

        #region ����ջ
        /// <summary>��ջ���ԡ�
        /// �����ջ��Ϣ�����ڵ���ʱ������������ġ�
        /// ����������ɴ�����־�������á�
        /// </summary>
        public static void DebugStack()
        {
            var msg = GetCaller(2, 0, Environment.NewLine);
            WriteLine("���ö�ջ��" + Environment.NewLine + msg);
        }

        /// <summary>��ջ���ԡ�</summary>
        /// <param name="maxNum">��󲶻��ջ������</param>
        public static void DebugStack(int maxNum)
        {
            var msg = GetCaller(2, maxNum, Environment.NewLine);
            WriteLine("���ö�ջ��" + Environment.NewLine + msg);
        }

        /// <summary>��ջ����</summary>
        /// <param name="start">��ʼ��������0��DebugStack��ֱ�ӵ�����</param>
        /// <param name="maxNum">��󲶻��ջ������</param>
        public static void DebugStack(int start, int maxNum)
        {
            // ����������ǰ���
            if (start < 1) start = 1;
            var msg = GetCaller(start + 1, maxNum, Environment.NewLine);
            WriteLine("���ö�ջ��" + Environment.NewLine + msg);
        }

        /// <summary>��ȡ����ջ</summary>
        /// <param name="start"></param>
        /// <param name="maxNum"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static String GetCaller(int start = 1, int maxNum = 0, String split = null)
        {
            // ����������ǰ���
            if (start < 1) start = 1;
            var st = new StackTrace(start, true);

            if (String.IsNullOrEmpty(split)) split = "<-";

            Type last = null;
            var asm = Assembly.GetEntryAssembly();
            var entry = asm == null ? null : asm.EntryPoint;

            int count = st.FrameCount;
            var sb = new StringBuilder(count * 20);
            if (maxNum > 0 && maxNum < count) count = maxNum;
            for (int i = 0; i < count; i++)
            {
                var sf = st.GetFrame(i);
                var method = sf.GetMethod();
                // ����<>���͵���������

                if (method == null || String.IsNullOrEmpty(method.Name) || method.Name[0] == '<' && method.Name.Contains(">")) continue;

                var type = method.DeclaringType ?? method.ReflectedType;

                var name = method.ToString();
                // ȥ��ǰ��ķ�������
                var p = name.IndexOf(" ");
                if (p >= 0) name = name.Substring(p + 1);
                // ȥ��ǰ���System
                name = name
                    .Replace("System.Web.", null)
                    .Replace("System.", null);

                sb.Append(name);

                // �����������ڵ㣬���Խ�����
                if (method == entry) break;

                if (i < count - 1) sb.Append(split);

                last = type;
            }
            return sb.ToString();
        }
        #endregion
    }
}