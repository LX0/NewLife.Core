using System;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife.Model
{
    /// <summary>��������</summary>
    /// <remarks>
    /// ����������ͨ���̳е�ǰ��ʵ��һ��˽�еķ���λ��������Ϊ������ṩ����λ����
    /// ����ڲ���Ĭ��ʵ�ֿ����ھ�̬���캯���н����޸���ע�ᡣ
    /// ��ΪԼ��������ڲ��ķ���λȫ��ͨ��������ɣ���֤������ʹ��ǰ�������ע�ᡣ
    /// </remarks>
    public class ServiceContainer
    {
        #region ��ǰ��̬��������
        /// <summary>��ǰ��������</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        #region ����
        /// <summary>
        /// ע��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="impl"></param>
        /// <param name="name"></param>
        public static void Register<T>(Type impl, String name)
        {
            Container.Register(typeof(T), impl, name);
        }

        /// <summary>
        /// ��������ָ�����Ƶ�ʵ��
        /// </summary>
        /// <typeparam name="TInterface">�ӿ�����</typeparam>
        /// <param name="name">����</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(String name)
        {
            return Container.Resolve<TInterface>(name);
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(String name)
        {
            return Container.ResolveType(typeof(TInterface), name);
        }
        #endregion
    }
}