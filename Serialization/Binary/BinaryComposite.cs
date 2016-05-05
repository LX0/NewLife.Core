﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>复合对象处理器</summary>
    public class BinaryComposite : BinaryHandlerBase
    {
        /// <summary>要忽略的成员</summary>
        public ICollection<String> IgnoreMembers { get; set; }

        /// <summary>实例化</summary>
        public BinaryComposite()
        {
            Priority = 100;

            //IgnoreMembers = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            IgnoreMembers = new HashSet<String>();
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (value == null) return false;

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;

            var ms = GetMembers(type);
            WriteLog("BinaryWrite类{0} 共有成员{1}个", type.Name, ms.Count);

            if (Host.UseFieldSize)
            {
                // 遍历成员，寻找FieldSizeAttribute特性，重新设定大小字段的值
                foreach (var member in ms)
                {
                    // 获取FieldSizeAttribute特性
                    var att = member.GetCustomAttribute<FieldSizeAttribute>();
                    if (att != null) att.SetReferenceSize(value, member, Host.Encoding);
                }
            }

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 获取成员
            foreach (var member in ms)
            {
                if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;

                var v = value.GetValue(member);
                WriteLog("    {0}.{1} {2}", type.Name, member.Name, v);

                // 处理位域支持，仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    if (WriteBit(member, ref bit, ref offset, ref v)) continue;
                }

                // 特殊处理写入名值对
                var rs = (Host.UseName) ? WritePair(member, v) : Host.Write(v, mtype);
                if (!rs)
                {
                    Host.Hosts.Pop();
                    return false;
                }
            }
            Host.Hosts.Pop();

            if (offset > 0) throw new XException("类{0}的位域字段不足8位", type);

            return true;
        }

        Boolean WriteBit(MemberInfo member, ref Int32 bit, ref Int32 offset, ref Object v)
        {
            var att = member.GetCustomAttribute<BitSizeAttribute>();
            if (att != null)
            {
                // 合并位域数据
                bit = att.Set(bit, (Byte)v, offset);

                // 偏移
                offset += att.Size;

                // 不足8位，等下一次
                if (offset < 8) return true;

                // 足够8位，可以写入了，清空位移和bit给下一次使用
                v = (Byte)bit;
                offset = 0;
                bit = 0;
            }

            return false;
        }

        Boolean WritePair(MemberInfo member, Object v)
        {
            Byte[] buf = null;
            if (v is String)
                buf = (v as String).GetBytes(Host.Encoding);
            else if (v is Byte[])
                buf = (Byte[])v;
            else
            {
                // 准备好名值对再一起写入。为了得到数据长度，需要提前计算好数据长度，所以需要临时切换数据流
                var ms = new MemoryStream();
                var old = Host.Stream;
                Host.Stream = ms;
                var rs = Host.Write(v, GetMemberType(member));
                Host.Stream = old;

                if (!rs) return false;
                buf = ms.ToArray();
            }

            WriteLog("WritePair {0}\t= {1}", member.Name, v);

            // 开始写入
            var key = member.Name.GetBytes(Host.Encoding);
            if (!Host.Write(key, key.GetType())) return false;
            if (!Host.Write(buf, buf.GetType())) return false;

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            // 不支持基本类型
            if (Type.GetTypeCode(type) != TypeCode.Object) return false;
            // 不支持基类不是Object的特殊类型
            //if (type.BaseType != typeof(Object)) return false;
            if (!typeof(Object).IsAssignableFrom(type)) return false;

            var ms = GetMembers(type);
            WriteLog("BinaryRead类{0} 共有成员{1}个", type.Name, ms.Count);

            if (value == null) value = type.CreateInstance();

            Host.Hosts.Push(value);

            // 位域偏移
            var offset = 0;
            var bit = 0;

            // 成员序列化访问器
            var ac = value as IMemberAccessor;

            // 提前准备名值对
            IDictionary<String, Byte[]> dic = null;
            if (Host.UseName) dic = ReadPair();

            // 获取成员
            for (int i = 0; i < ms.Count; i++)
            {
                var member = ms[i];
                if (IgnoreMembers != null && IgnoreMembers.Contains(member.Name)) continue;

                var mtype = GetMemberType(member);
                Host.Member = member;
                WriteLog("    {0}.{1}", member.DeclaringType.Name, member.Name);

                // 处理位域支持，仅支持Byte
                if (member.GetMemberType() == typeof(Byte))
                {
                    if (TryReadBit(member, ref bit, ref offset, value)) continue;
                }

                // 成员访问器优先
                if (ac != null && TryReadAccessor(member, value, ref ac, ref ms)) continue;

                Object v = null;
                // 特殊处理写入名值对
                if (Host.UseName)
                {
                    //if (!TryReadPair(dic, member, ref v))
                    //{
                    //    Host.Hosts.Pop();
                    //    return false;
                    //}
                    // 名值对即使没有配对也没有关系
                    TryReadPair(dic, member, ref v);
                }
                else if (!Host.TryRead(mtype, ref v))
                {
                    Host.Hosts.Pop();
                    return false;
                }

                value.SetValue(member, v);
            }
            Host.Hosts.Pop();

            if (offset > 0) throw new XException("类{0}的位域字段不足8位", type);

            return true;
        }

        Boolean TryReadAccessor(MemberInfo member, Object value, ref IMemberAccessor ac, ref List<MemberInfo> ms)
        {
            // 访问器直接写入成员
            if (!ac.Read(Host, member)) return false;

            // 访问器内部可能直接操作Hosts修改了父级对象，典型应用在于某些类需要根据某个字段值决定采用哪个派生类
            var obj = Host.Hosts.Peek();
            if (obj != value)
            {
                value = obj;
                ms = GetMembers(value.GetType());
                ac = value as IMemberAccessor;
            }

            return true;
        }

        Boolean TryReadBit(MemberInfo member, ref Int32 bit, ref Int32 offset, Object value)
        {
            var att = member.GetCustomAttribute<BitSizeAttribute>();
            if (att == null) return false;

            // 仅在第一个位移处读取
            if (offset == 0)
            {
                var mtype = GetMemberType(member);
                Object v2 = null;
                if (!Host.TryRead(mtype, ref v2))
                {
                    Host.Hosts.Pop();
                    return false;
                }
                bit = (Byte)v2;
            }

            // 取得当前字段所属部分
            var n = att.Get(bit, offset);

            value.SetValue(member, (Byte)n);

            // 偏移
            offset += att.Size;

            // 足够8位，可以写入了，清空位移和bit给下一次使用
            if (offset >= 8)
            {
                offset = 0;
                bit = 0;
            }

            return true;
        }

        IDictionary<String, Byte[]> ReadPair()
        {
            var ms = Host.Stream;
            var dic = new Dictionary<String, Byte[]>();
            while (ms.Position < ms.Length)
            {
                var len = ms.ReadEncodedInt();
                if (len > ms.Length - ms.Position) break;

                var name = ms.ReadBytes(len).ToStr(Host.Encoding);
                // 避免名称为空导致dic[name]报错
                name += "";

                len = ms.ReadEncodedInt();
                if (len > ms.Length - ms.Position) break;

                dic[name] = ms.ReadBytes(len);
            }

            return dic;
        }

        Boolean TryReadPair(IDictionary<String, Byte[]> dic, MemberInfo member, ref Object value)
        {
            Byte[] buf = null;
            if (!dic.TryGetValue(member.Name, out buf)) return false;

            var mtype = GetMemberType(member);

            WriteLog("TryReadPair {0}\t= {1}", member.Name, buf.ToHex("-", 0, 32));

            if (mtype == typeof(String))
            {
                value = buf.ToStr(Host.Encoding);
                WriteLog(value + "");
                return true;
            }
            if (mtype == typeof(Byte[]))
            {
                value = buf;
                return true;
            }

            var old = Host.Stream;
            Host.Stream = new MemoryStream(buf);
            try
            {
                return Host.TryRead(mtype, ref value);
            }
            finally
            {
                Host.Stream = old;
                WriteLog("{0}".F(value));
            }
        }

        #region 获取成员
        /// <summary>获取成员</summary>
        /// <param name="type"></param>
        /// <param name="baseFirst"></param>
        /// <returns></returns>
        protected virtual List<MemberInfo> GetMembers(Type type, Boolean baseFirst = true)
        {
            if (Host.UseProperty)
                return type.GetProperties(baseFirst).Cast<MemberInfo>().ToList();
            else
                return type.GetFields(baseFirst).Cast<MemberInfo>().ToList();
        }

        static Type GetMemberType(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                default:
                    throw new NotSupportedException();
            }
        }
        #endregion
    }
}