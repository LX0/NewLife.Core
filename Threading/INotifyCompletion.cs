using System;

namespace System.Runtime.CompilerServices
{
    /// <summary>��ʾ��������ƻ��ȴ��������ʱ�ĺ������֡�</summary>
	public interface INotifyCompletion
	{
        /// <summary>�ƻ�ʵ�����ʱ���õ�����������</summary>
        /// <param name="continuation"></param>
		void OnCompleted(Action continuation);
	}
}
