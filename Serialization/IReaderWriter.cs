﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器接口
    /// </summary>
    public interface IReaderWriter
    {
        #region 属性
        /// <summary>字符串编码。</summary>
        Encoding Encoding { get; set; }

        ///// <summary>
        ///// 是否小端字节序。
        ///// </summary>
        ///// <remarks>
        ///// 网络协议都是Big-Endian；
        ///// Java编译的都是Big-Endian；
        ///// Motorola的PowerPC是Big-Endian；
        ///// x86系列则采用Little-Endian方式存储数据；
        ///// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        ///// </remarks>
        //Boolean IsLittleEndian { get; set; }
        #endregion
    }
}