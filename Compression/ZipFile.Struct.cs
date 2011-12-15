﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using NewLife.IO;
using NewLife.Reflection;
using NewLife.Serialization;
using System.Xml.Serialization;

namespace NewLife.Compression
{
    partial class ZipFile
    {
        #region CentralDirectory
        class EndOfCentralDirectory
        {
            #region 属性
            private UInt32 _Signature;
            /// <summary>签名。end of central dir signature</summary>
            public UInt32 Signature { get { return _Signature; } set { _Signature = value; } }

            private UInt16 _DiskNumber;
            /// <summary>卷号。number of this disk</summary>
            public UInt16 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

            private UInt16 _DiskNumberWithStart;
            /// <summary>number of the disk with the start of the central directory</summary>
            public UInt16 DiskNumberWithStart { get { return _DiskNumberWithStart; } set { _DiskNumberWithStart = value; } }

            private UInt16 _NumberOfEntriesOnThisDisk;
            /// <summary>total number of entries in the central directory on this disk</summary>
            public UInt16 NumberOfEntriesOnThisDisk { get { return _NumberOfEntriesOnThisDisk; } set { _NumberOfEntriesOnThisDisk = value; } }

            private UInt16 _NumberOfEntries;
            /// <summary>total number of entries in the central directory</summary>
            public UInt16 NumberOfEntries { get { return _NumberOfEntries; } set { _NumberOfEntries = value; } }

            private UInt32 _Size;
            /// <summary>size of the central directory</summary>
            public UInt32 Size { get { return _Size; } set { _Size = value; } }

            private UInt32 _Offset;
            /// <summary>offset of start of central directory with respect to the starting disk number</summary>
            public UInt32 Offset { get { return _Offset; } set { _Offset = value; } }

            private String _Comment;
            /// <summary>注释</summary>
            public String Comment { get { return _Comment; } set { _Comment = value; } }
            #endregion

            #region 定位
            //public const UInt32 DefaultSignature = 0x06054b50;

            //public static Int64 FindSignature(Stream stream)
            //{
            //    return stream.IndexOf(BitConverter.GetBytes(DefaultSignature));
            //}

            #endregion
        }
        #endregion

        #region Zip64CentralDirectory
        //class Zip64EndOfCentralDirectory : IAccessor
        //{
        //    #region 属性
        //    private UInt32 _Signature;
        //    /// <summary>签名</summary>
        //    public UInt32 Signature
        //    {
        //        get { return _Signature; }
        //        set { _Signature = value; }
        //    }

        //    private UInt64 _DataSize;
        //    /// <summary>大小</summary>
        //    public UInt64 DataSize
        //    {
        //        get { return _DataSize; }
        //        set { _DataSize = value; }
        //    }

        //    private UInt16 _VersionMadeBy;
        //    /// <summary>压缩的版本</summary>
        //    public UInt16 VersionMadeBy
        //    {
        //        get { return _VersionMadeBy; }
        //        set { _VersionMadeBy = value; }
        //    }

        //    private UInt16 _VersionNeededToExtract;
        //    /// <summary>需要用于解压缩的版本</summary>
        //    public UInt16 VersionNeededToExtract
        //    {
        //        get { return _VersionNeededToExtract; }
        //        set { _VersionNeededToExtract = value; }
        //    }

        //    private UInt32 _DiskNumber;
        //    /// <summary>卷号。number of this disk</summary>
        //    public UInt32 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

        //    private UInt32 _DiskNumberWithStart;
        //    /// <summary>number of the disk with the start of the central directory</summary>
        //    public UInt32 DiskNumberWithStart { get { return _DiskNumberWithStart; } set { _DiskNumberWithStart = value; } }

        //    private UInt64 _NumberOfEntriesOnThisDisk;
        //    /// <summary>total number of entries in the central directory on this disk</summary>
        //    public UInt64 NumberOfEntriesOnThisDisk { get { return _NumberOfEntriesOnThisDisk; } set { _NumberOfEntriesOnThisDisk = value; } }

        //    private UInt64 _NumberOfEntries;
        //    /// <summary>total number of entries in the central directory</summary>
        //    public UInt64 NumberOfEntries { get { return _NumberOfEntries; } set { _NumberOfEntries = value; } }

        //    private UInt64 _Size;
        //    /// <summary>size of the central directory</summary>
        //    public UInt64 Size { get { return _Size; } set { _Size = value; } }

        //    private UInt64 _Offset;
        //    /// <summary>offset of start of central directory with respect to the starting disk number</summary>
        //    public UInt64 Offset { get { return _Offset; } set { _Offset = value; } }

        //    /// <summary>扩展数据大小</summary>
        //    private UInt64 ExtendSize { get { return DataSize + 44; } set { DataSize = value + 44; } }

        //    [FieldSize("ExtendSize")]
        //    private Byte[] _Extend;
        //    /// <summary>扩展数据</summary>
        //    public Byte[] Extend { get { return _Extend; } set { _Extend = value; } }
        //    #endregion

        //    #region 方法
        //    //public override void Read(Stream stream)
        //    //{
        //    //    base.Read(stream);

        //    //    UInt64 n = DataSize - 44;
        //    //    if (n > 0)
        //    //    {
        //    //        Extend = new Byte[n];
        //    //        stream.Read(Extend, 0, (Int32)n);
        //    //    }
        //    //}

        //    //public override void Write(Stream stream)
        //    //{
        //    //    if (Extend != null && Extend.Length > 0) DataSize = 44 + (UInt64)Extend.Length;
        //    //    base.Write(stream);
        //    //    stream.Write(Extend, 0, Extend.Length);
        //    //}
        //    #endregion

        //    #region 定位
        //    public const UInt32 DefaultSignature = 0x06064b50;
        //    #endregion

        //    #region IAccessor 成员

        //    bool IAccessor.Read(IReader reader) { return false; }

        //    bool IAccessor.ReadComplete(IReader reader, bool success)
        //    {
        //        // 读取完成，开始读取扩展数据
        //        UInt64 n = DataSize - 44;
        //        if (n > 0) Extend = reader.ReadBytes((Int32)n);

        //        return false;
        //    }

        //    bool IAccessor.Write(IWriter writer)
        //    {
        //        // 重新计算数据大小
        //        if (Extend != null && Extend.Length > 0) DataSize = 44 + (UInt64)Extend.Length;

        //        return false;
        //    }

        //    bool IAccessor.WriteComplete(IWriter writer, bool success)
        //    {
        //        // 写入完成，开始写入扩展数据
        //        if (Extend != null && Extend.Length > 0) writer.Write(Extend, 0, Extend.Length);

        //        return false;
        //    }

        //    #endregion
        //}
        #endregion
    }
}