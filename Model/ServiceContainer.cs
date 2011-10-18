using System;
using System.Collections.Generic;
using System.Globalization;

namespace NewLife.Model
{
    /// <summary>�����������ࡣʹ�÷��ͻ��࣬������Ϊ����������ľ�̬���캯����</summary>
    /// <typeparam name="TService">�������������</typeparam>
    /// <remarks>
    /// ����������ͨ���̳е�ǰ��ʵ��һ��˽�еķ���λ��������Ϊ������ṩ����λ����
    /// ����ڲ���Ĭ��ʵ�ֿ����ھ�̬���캯���н����޸���ע�ᡣ
    /// ��ΪԼ��������ڲ��ķ���λȫ��ͨ��������ɣ���֤������ʹ��ǰ�������ע�ᡣ
    /// </remarks>
    public class ServiceContainer<TService> where TService : ServiceContainer<TService>, new()
    {
        #region ��̬���캯��
        static ServiceContainer()
        {
            // ʵ����һ������Ϊ�˴�������ľ�̬���캯��
            TService service = new TService();
        }
        #endregion

        #region ��ǰ��̬��������
        /// <summary>��ǰ��������</summary>
        public static IObjectContainer Container { get { return ObjectContainer.Current; } }
        #endregion

        #region ����
        /// <summary>
        /// ע�����ͺ�����
        /// </summary>
        /// <typeparam name="TInterface">�ӿ�����</typeparam>
        /// <typeparam name="TImplement">ʵ������</typeparam>
        /// <param name="name">����</param>
        /// <param name="overwrite">�Ƿ񸲸�</param>
        /// <returns></returns>
        public static IObjectContainer Register<TInterface, TImplement>(String name = null, Boolean overwrite = true)
        {
            return Container.Register<TInterface, TImplement>(name, overwrite);
        }

        /// <summary>
        /// ע��
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="impl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IObjectContainer Register<T>(Type impl, String name = null)
        {
            return Container.Register(typeof(T), impl, name);
        }

        /// <summary>
        /// ��������ָ�����Ƶ�ʵ��
        /// </summary>
        /// <typeparam name="TInterface">�ӿ�����</typeparam>
        /// <param name="name">����</param>
        /// <returns></returns>
        public static TInterface Resolve<TInterface>(String name = null)
        {
            return Container.Resolve<TInterface>(name);
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type ResolveType<TInterface>(String name = null)
        {
            return Container.ResolveType(typeof(TInterface), name);
        }
        #endregion
    }
}