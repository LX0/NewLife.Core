﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Net
{
    /// <summary>TCP服务器</summary>
    /// <remarks>
    /// 核心工作：启动服务<see cref="Start"/>时，监听端口，并启用多个（逻辑处理器数的10倍）异步接受操作<see cref="AcceptAsync"/>。
    /// 
    /// 服务器完全处于异步工作状态，任何操作都不可能被阻塞。
    /// 
    /// 注意：服务器接受连接请求后，不会开始处理数据，而是由<see cref="Accepted"/>事件订阅者决定何时开始处理数据。
    /// </remarks>
    public class TcpServer : DisposeBase, ISocketServer
    {
        #region 属性
        private NetUri _Local = new NetUri(ProtocolType.Tcp, IPAddress.Any, 0);
        /// <summary>本地绑定信息</summary>
        public NetUri Local { get { return _Local; } set { _Local = value; } }

        /// <summary>端口</summary>
        public Int32 Port { get { return _Local.Port; } set { _Local.Port = value; } }

        private Int32 _MaxNotActive = 30;
        /// <summary>最大不活动时间。
        /// 对于每一个会话连接，如果超过该时间仍然没有收到任何数据，则断开会话连接。
        /// 单位秒，默认30秒。时间不是太准确，建议15秒的倍数。为0表示不检查。</summary>
        public Int32 MaxNotActive { get { return _MaxNotActive; } set { _MaxNotActive = value; } }

        private Boolean _AutoReceiveAsync = true;
        /// <summary>自动开始会话的异步接收，默认true。
        /// 接受连接请求后，自动开始会话的异步接收，默认打开，如果会话需要同步接收数据，需要关闭该选项。</summary>
        public Boolean AutoReceiveAsync { get { return _AutoReceiveAsync; } set { _AutoReceiveAsync = value; } }

        private TcpListener _Server;
        /// <summary>服务器</summary>
        public TcpListener Server { get { return _Server; } set { _Server = value; } }

        /// <summary>底层Socket</summary>
        Socket ISocket.Socket { get { return _Server == null ? null : _Server.Server; } }

        private Boolean _Active;
        /// <summary>是否活动</summary>
        public Boolean Active { get { return _Active; } set { _Active = value; } }

        private Boolean _ThrowException;
        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return _ThrowException; } set { _ThrowException = value; } }

        private IStatistics _Statistics = new Statistics();
        /// <summary>统计信息</summary>
        public IStatistics Statistics { get { return _Statistics; } private set { _Statistics = value; } }
        #endregion

        #region 构造
        /// <summary>构造TCP服务器对象</summary>
        public TcpServer() { }

        /// <summary>构造TCP服务器对象</summary>
        /// <param name="port"></param>
        public TcpServer(Int32 port) { Port = port; }
        #endregion

        #region 释放资源
        /// <summary>已重载。释放会话集合等资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            // 释放托管资源
            //if (disposing)
            {
                Stop();
            }
        }
        #endregion

        #region 开始停止
        /// <summary>开始</summary>
        public virtual void Start()
        {
            if (Active || Disposed) return;

            // 开始监听
            if (Server == null) Server = new TcpListener(Local.EndPoint);

            WriteLog("{0}.Start {1}", this.GetType().Name, this);

            // 三次握手之后，Accept之前的总连接个数，队列满之后，新连接将得到主动拒绝ConnectionRefused错误
            // 在我（大石头）的开发机器上，实际上这里的最大值只能是200，大于200跟200一个样
            Server.Start();

            if (!AcceptAsync(true)) return;

            Active = true;
        }

        /// <summary>停止</summary>
        public virtual void Stop()
        {
            if (!Active) return;

            WriteLog("{0}.Stop {1}", this.GetType().Name, this);

            if (_Async != null && _Async.AsyncWaitHandle != null) _Async.AsyncWaitHandle.Close();

            CloseAllSession();

            if (Server != null) Server.Stop();
            Server = null;

            Active = false;
        }
        #endregion

        #region 连接处理
        /// <summary>连接完成。在事件处理代码中，事件参数不得另作他用，套接字事件池将会将其回收。</summary>
        /// <remarks>这里一定不需要再次ReceiveAsync，因为TcpServer在处理完成Accepted事件后，会调用Start->ReceiveAsync</remarks>
        public event EventHandler<AcceptedEventArgs> Accepted;

        private IAsyncResult _Async;

        /// <summary>开启异步接受新连接</summary>
        /// <param name="throwException">是否抛出异常</param>
        /// <returns>开启异步是否成功</returns>
        Boolean AcceptAsync(Boolean throwException)
        {
            if (_Async != null) return true;
            try
            {
                _Async = Server.BeginAcceptTcpClient(OnAccept, null);
                return true;
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed()) OnError("BeginAcceptTcpClient", ex);

                // BeginAcceptTcpClient异常一般是服务器已经被关闭，所以这里不需要去关闭服务器

                if (throwException) throw;
                return false;
            }
        }

        void OnAccept(IAsyncResult ar)
        {
            _Async = null;

            if (!Active) return;

            if (Server == null) return;

            TcpClient client = null;
            try
            {
                client = Server.EndAcceptTcpClient(ar);
            }
            catch (Exception ex)
            {
                if (!ex.IsDisposed())
                {
                    OnError("EndAcceptTcpClient", ex);

                    // EndAcceptTcpClient异常一般是网络故障，但是为了确保系统可靠性，我们仍然不能关闭服务器
                    //Stop();

                    // 开始新的监听，避免因为异常就失去网络服务
                    AcceptAsync(false);
                }

                return;
            }

            // 在用户线程池里面去处理数据
            ThreadPoolX.QueueUserWorkItem(obj => OnAccept(obj as TcpClient), client);

            // 开始新的征程
            AcceptAsync(false);
        }

        /// <summary>收到新连接时处理</summary>
        /// <param name="client"></param>
        protected virtual void OnAccept(TcpClient client)
        {
            WriteLog("{0} Accept {1}", this, client.Client.RemoteEndPoint);

            var session = CreateSession(client);
            // 服务端不支持掉线重连
            session.AutoReconnect = false;
            session.Log = Log;
            if (Accepted != null) Accepted(this, new AcceptedEventArgs { Session = session });

            Sessions.Add(session.Remote.EndPoint + "", session);

            // 设置心跳时间
            client.Client.SetTcpKeepAlive(true);

            // 自动开始异步接收处理
            if (AutoReceiveAsync) session.ReceiveAsync();
        }
        #endregion

        #region 会话
        private Object _Sessions_lock = new object();
        private IDictionary<String, TcpSession> _Sessions;
        /// <summary>会话集合。用自增的数字ID作为标识，业务应用自己维持ID与业务主键的对应关系。</summary>
        public IDictionary<String, TcpSession> Sessions
        {
            get
            {
                if (_Sessions != null) return _Sessions;
                lock (_Sessions_lock)
                {
                    if (_Sessions != null) return _Sessions;

                    return _Sessions = new TcpSessionCollection() { Server = this };
                }
            }
        }

        /// <summary>创建会话</summary>
        /// <param name="client"></param>
        /// <returns></returns>
        protected virtual TcpSession CreateSession(TcpClient client)
        {
            var session = new TcpSession(this, client);

            return session;
        }

        private void CloseAllSession()
        {
            var sessions = _Sessions;
            if (sessions != null)
            {
                _Sessions = null;

                XTrace.WriteLine("准备释放Tcp会话{0}个！", sessions.Count);
                sessions.TryDispose();
                sessions.Clear();
            }
        }
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            if (Log != null) Log.Error("{0}.{1}Error {2} {3}", this.GetType().Name, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Action = action, Exception = ex });
        }
        #endregion

        #region 日志
        private ILog _Log;
        /// <summary>日志对象</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            if (Log != null) Log.Info(format, args);
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Sessions.Count > 0)
                return String.Format("{0} [{1}]", Local, Sessions.Count);
            else
                return Local.ToString();
        }
        #endregion
    }

    /// <summary>接受连接时触发</summary>
    public class AcceptedEventArgs : EventArgs
    {
        private ISocketSession _Session;
        /// <summary>会话</summary>
        public ISocketSession Session { get { return _Session; } set { _Session = value; } }
    }
}