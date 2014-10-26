﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>序列化接口</summary>
    public interface IFormatterX
    {
        #region 属性
        /// <summary>数据流</summary>
        Stream Stream { get; set; }

        /// <summary>主对象</summary>
        Stack<Object> Hosts { get; }

        /// <summary>成员</summary>
        MemberInfo Member { get; set; }
        #endregion

        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type = null);

        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Read(Type type);

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Read<T>();

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
        #endregion
    }

    /// <summary>序列化接口</summary>
    public abstract class FormatterBase //: IFormatterX
    {
        #region 属性
        private Int64 _StartPosition = 0;

        private Stream _Stream;
        /// <summary>数据流。默认实例化一个内存数据流</summary>
        public virtual Stream Stream { get { return _Stream ?? (_Stream = new MemoryStream()); } set { _Stream = value; _StartPosition = value == null ? 0 : value.Position; } }

        private Stack<Object> _Hosts = new Stack<Object>();
        /// <summary>主对象</summary>
        public Stack<Object> Hosts { get { return _Hosts; } }

        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member { get { return _Member; } set { _Member = value; } }

        private Encoding _Encoding = Encoding.Default;
        /// <summary>字符串编码，默认Default</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }
        #endregion

        #region 方法
        /// <summary>获取流里面的数据</summary>
        /// <returns></returns>
        public Byte[] GetBytes()
        {
            var ms = Stream;
            var pos = ms.Position;
            if (pos == 0 || pos == _StartPosition) return new Byte[0];

            if (ms is MemoryStream && pos == ms.Length && _StartPosition == 0)
                return (ms as MemoryStream).ToArray();

            ms.Position = _StartPosition;
            return ms.ReadBytes(pos - _StartPosition);
        }
        #endregion
    }
}