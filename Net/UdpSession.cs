﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NewLife.Net
{
    /// <summary>Udp会话。仅用于服务端与某一固定远程地址通信</summary>
    class UdpSession : DisposeBase, ISocketSession
    {
        #region 属性
        private UdpServer _Server;
        /// <summary>服务器</summary>
        public UdpServer Server { get { return _Server; } set { _Server = value; } }

        /// <summary>底层Socket</summary>
        Socket ISocket.Socket { get { return _Server == null ? null : _Server.Client.Client; } }

        private Stream _Stream = new MemoryStream();
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream; } set { _Stream = value; } }

        private NetUri _Local;
        /// <summary>本地地址</summary>
        public NetUri Local
        {
            get
            {
                return _Local ?? (_Local = Server == null ? null : Server.Local);
            }
            set { Server.Local = _Local = value; }
        }

        /// <summary>端口</summary>
        public Int32 Port { get { return Local.Port; } set { Local.Port = value; } }

        private NetUri _Remote;
        /// <summary>远程地址</summary>
        public NetUri Remote { get { return _Remote; } set { _Remote = value; } }

        /// <summary>Socket服务器。当前通讯所在的Socket服务器，其实是TcpServer/UdpServer</summary>
        ISocketServer ISocketSession.Server { get { return _Server; } }

        //private Boolean _ThrowException;
        /// <summary>是否抛出异常，默认false不抛出。Send/Receive时可能发生异常，该设置决定是直接抛出异常还是通过<see cref="Error"/>事件</summary>
        public Boolean ThrowException { get { return Server.ThrowException; } set { Server.ThrowException = value; } }

        private IStatistics _Statistics = new Statistics();
        /// <summary>统计信息</summary>
        public IStatistics Statistics { get { return _Statistics; } private set { _Statistics = value; } }

        private IPEndPoint _Filter;
        #endregion

        #region 构造
        public UdpSession(UdpServer server, IPEndPoint remote)
        {
            Server = server;
            Remote = new NetUri(ProtocolType.Udp, remote);
            _Filter = remote;

            server.Received += server_Received;
            //server.ReceiveAsync();
            server.Error += server_Error;
        }

        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            Server.Received -= server_Received;
            Server.Error -= server_Error;
            // 释放对服务对象的引用，如果没有其它引用，服务对象将会被回收
            Server = null;
            GC.Collect();
        }
        #endregion

        #region 收发
        public Boolean Send(byte[] buffer, int offset = 0, int count = -1)
        {
            if (count <= 0) count = buffer.Length - offset;
            if (offset > 0) buffer = buffer.ReadBytes(offset, count);

            Server.WriteLog("{0}.Send {1} [{2}]", this.GetType().Name, this, count);

            try
            {
                Server.Client.Send(buffer, count, Remote.EndPoint);

                return true;
            }
            catch (Exception ex)
            {
                OnError("Send", ex);
                Dispose();
                throw;
            }
        }

        Boolean CheckFilter(IPEndPoint remote)
        {
            // IPAddress是类，不同实例对象当然不相等啦
            if (!_Filter.IsAny())
            {
                //if (_Filter.Address != remote.Address || _Filter.Port != remote.Port) return false;
                if (!_Filter.Equals(remote)) return false;
            }

            return true;
        }

        public byte[] Receive()
        {
            // UDP会话的直接读取可能会读到不是自己的数据，所以尽量不要两个会话一起读
            var buf = Server.Receive();

            var ep = Server.Remote.EndPoint;
            if (!CheckFilter(ep)) return new Byte[0];
            Remote.EndPoint = ep;

            return buf;
        }

        public int Receive(byte[] buffer, int offset = 0, int count = -1)
        {
            // UDP会话的直接读取可能会读到不是自己的数据，所以尽量不要两个会话一起读
            var size = Server.Receive(buffer, offset, count);

            var ep = Server.Remote.EndPoint;
            if (!CheckFilter(ep)) return 0;
            Remote.EndPoint = ep;

            return size;
        }
        #endregion

        #region 异步接收
        /// <summary>开始异步接收数据</summary>
        public Boolean ReceiveAsync() { return Server.ReceiveAsync(); }

        public event EventHandler<ReceivedEventArgs> Received;

        void server_Received(object sender, ReceivedEventArgs e)
        {
            if (Received == null) return;

            // 判断是否自己的数据
            var udp = e as UdpReceivedEventArgs;
            if (CheckFilter(udp.Remote))
            {
                Received(this, e);
            }
        }
        #endregion

        #region 异常处理
        /// <summary>错误发生/断开连接时</summary>
        public event EventHandler<ExceptionEventArgs> Error;

        void server_Error(object sender, ExceptionEventArgs e)
        {
            OnError(null, e.Exception);
        }

        /// <summary>触发异常</summary>
        /// <param name="action">动作</param>
        /// <param name="ex">异常</param>
        protected virtual void OnError(String action, Exception ex)
        {
            Server.WriteLog("{0}.{1}Error {2} {3}", this.GetType().Name, action, this, ex == null ? null : ex.Message);
            if (Error != null) Error(this, new ExceptionEventArgs { Exception = ex });

            //if (ex != null) Close();
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Remote != null && !Remote.EndPoint.IsAny())
                return String.Format("{0}=>{1}", Local, Remote.EndPoint);
            else
                return Local.ToString();
        }
        #endregion
    }
}