using System;
using System.IO;

namespace SevenZip
{
    /// <summary>���������������쳣</summary>
    class DataErrorException : ApplicationException
    {
        public DataErrorException() : base("Data Error") { }
    }

    /// <summary>��Ч������Χ</summary>
    class InvalidParamException : ApplicationException
    {
        public InvalidParamException() : base("Invalid Parameter") { }
    }

    /// <summary>�������</summary>
    public interface ICodeProgress
    {
        /// <summary>���ý���</summary>
        /// <param name="inSize">�����С��-1��ʾδ֪</param>
        /// <param name="outSize">�����С��-1��ʾδ֪</param>
        void SetProgress(Int64 inSize, Int64 outSize);
    };

    /// <summary>����ӿ�</summary>
    public interface ICoder
    {
        /// <summary>����������</summary>
        /// <param name="inStream">������</param>
        /// <param name="outStream">�����</param>
        /// <param name="inSize">�����С��-1��ʾδ֪</param>
        /// <param name="outSize">�����С��-1��ʾδ֪</param>
        /// <param name="progress">��������ί��</param>
        /// <exception cref="SevenZip.DataErrorException">��������Ч</exception>
        void Code(Stream inStream, Stream outStream, Int64 inSize, Int64 outSize, ICodeProgress progress);
    };

    /// <summary>��������</summary>
    public enum CoderPropID
    {
        /// <summary>Ĭ������</summary>
        DefaultProp = 0,

        /// <summary>�ֵ��С</summary>
        DictionarySize,

        /// <summary>��ʹ�õ�PPM�ڴ��С</summary>
        UsedMemorySize,

        /// <summary>PPM����˳��</summary>
        Order,

        /// <summary>���С</summary>
        BlockSize,

        /// <summary>LZMAλ��״̬λ����(0&lt;=x&lt;=4)</summary>
        PosStateBits,

        /// <summary>
        /// Specifies number of literal context bits for LZMA (0 &lt;= x &lt;= 8).
        /// </summary>
        LitContextBits,

        /// <summary>
        /// Specifies number of literal position bits for LZMA (0 &lt;= x &lt;= 4).
        /// </summary>
        LitPosBits,

        /// <summary>LZ���ֽ���</summary>
        NumFastBytes,

        /// <summary>ƥ����ҷ�ʽ LZMA: "BT2", "BT4", "BT4B"</summary>
        MatchFinder,

        /// <summary>ƥ�����ѭ������</summary>
        MatchFinderCycles,

        /// <summary>��������</summary>
        NumPasses,

        /// <summary>�㷨����</summary>
        Algorithm,

        /// <summary>�߳���</summary>
        NumThreads,

        /// <summary>�������ģʽ</summary>
        EndMarker
    };

    /// <summary>���ñ������Խӿ�</summary>
    public interface ISetCoderProperties
    {
        /// <summary>���ñ�������</summary>
        /// <param name="propIDs"></param>
        /// <param name="properties"></param>
        void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
    };

    /// <summary>д���������</summary>
    public interface IWriteCoderProperties
    {
        /// <summary>д���������</summary>
        /// <param name="outStream"></param>
        void WriteCoderProperties(Stream outStream);
    }

    /// <summary>���ý�������</summary>
    public interface ISetDecoderProperties
    {
        /// <summary>���ý�������</summary>
        /// <param name="properties"></param>
        void SetDecoderProperties(byte[] properties);
    }
}