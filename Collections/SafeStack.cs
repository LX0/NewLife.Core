﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace NewLife.Collections
{
    /// <summary>基于数组实现的线程安全栈。快速高效，不会形成内存碎片。</summary>
    /// <remarks>
    /// 链表做的原子栈<see cref="InterlockedStack&lt;T&gt;"/>，本来是为了做对象池用的，但是链表节点自身也会形成内存碎片，给GC压力，十分纠结。
    /// 一直认为用数组做存储是效率最好的，但是纠结于无法实现原子操作，而迟迟不敢动手。
    /// 在 @Aimeast 的指点下，有所感悟，我们没必要严格的追求绝对安全，只要把冲突可能性降到尽可能低即可。
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SafeStack<T> : DisposeBase, IEnumerable<T>, ICollection, IEnumerable
    {
        #region 属性
        /// <summary>数据数组</summary>
        private T[] _array;

        private Int32 _Count;
        /// <summary>元素个数，同时也是下一个空位的位置指针</summary>
        public Int32 Count { get { return _Count; } }

        /// <summary>最大容量</summary>
        public Int32 Capacity { get { return _array == null ? 0 : _array.Length; } }

        private Int32 _lock;
        #endregion

        #region 构造
        /// <summary>实例化一个容纳4个元素的安全栈</summary>
        public SafeStack() : this(4) { }

        /// <summary>实例化一个指定大小的安全栈</summary>
        /// <param name="capacity"></param>
        public SafeStack(Int32 capacity)
        {
            _array = new T[capacity];
        }

        /// <summary>使用指定枚举实例化一个安全栈</summary>
        /// <param name="collection"></param>
        public SafeStack(IEnumerable collection)
        {
            var list = new List<T>();
            foreach (var item in collection)
            {
                list.Add((T)item);
            }
            _array = list.ToArray();
            _Count = _array.Length;
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            for (int i = 0; i < _array.Length && i < Count; i++)
            {
                var item = _array[i];
                if (item != null && item is IDisposable) (item as IDisposable).Dispose();
                _array[i] = default(T);
            }
        }
        #endregion

        #region 核心方法
        /// <summary>向栈压入一个对象</summary>
        /// <remarks>重点解决多线程环境下资源争夺以及使用lock造成性能损失的问题</remarks>
        /// <param name="item"></param>
        public void Push(T item)
        {
            // 检查锁，因为可能加锁来改变_array
            while (_lock > 0) Thread.SpinWait(1);

            Int32 p;
            do
            {
                p = Count;
            }
            // 如果Count现在还是p，表明取得这个位置，并把Count后移一位
            while (Interlocked.CompareExchange(ref _Count, p + 1, p) != p);

            // 是否容量超标
            if (p > _array.Length)
            {
                // 加锁，扩容
                // 开始抢锁
                while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) Thread.SpinWait(1);
                // DoubleLock
                if (p > _array.Length)
                {
                    // 稍等一会，可能某些读取尚未完成
                    Thread.SpinWait(100);

                    // 以4为最小值，成倍扩容
                    Int32 size = _array.Length < 4 ? 4 : _array.Length * 2;
                    var _arr = new T[size];
                    _array.CopyTo(_arr, 0);
                    _array = _arr;
                }

                // 解锁
                Interlocked.Decrement(ref _lock);
            }

            _array[p] = item;
        }

        /// <summary>从栈中弹出一个对象</summary>
        /// <returns></returns>
        public T Pop()
        {
            T item;
            if (!TryPop(out item)) throw new InvalidOperationException("栈为空！");

            return item;
        }

        /// <summary>尝试从栈中弹出一个对象</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryPop(out T item)
        {
            // 检查锁，因为可能加锁来改变_array
            while (_lock > 0) Thread.SpinWait(1);

            Int32 p;
            do
            {
                p = Count;

                if (p < 1)
                {
                    item = default(T);
                    return false;
                }
            }
            // 如果Count现在还是p，表明取得这个位置，并把Count前移一位
            while (Interlocked.CompareExchange(ref _Count, p - 1, p) != p);

            // p只是下一个空位置，必须前移一位才能找到最后一个元素
            p--;
            item = _array[p];
            _array[p] = default(T);

            return true;
        }

        /// <summary>获取栈顶对象，不弹栈</summary>
        /// <returns></returns>
        public T Peek()
        {
            T item;
            if (!TryPeek(out item)) throw new InvalidOperationException("栈为空！");

            return item;
        }

        /// <summary>尝试获取栈顶对象，不弹栈</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean TryPeek(out T item)
        {
            Int32 p = Count;
            if (p < 1)
            {
                item = default(T);
                return false;
            }

            item = _array[p - 1];
            return true;
        }
        #endregion

        #region 集合方法
        /// <summary>清空</summary>
        public void Clear()
        {
            // 先把指针移到开头，再执行清空操作，减少冲突可能性
            var len = Count;
            _Count = 0;
            for (int i = 0; i < _array.Length && i < len; i++) _array[i] = default(T);
        }

        /// <summary>转为数组</summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            var len = Count;
            if (len < 1) return null;

            T[] arr = new T[len];
            Array.Copy(_array, 0, arr, 0, len);
            return arr;
        }
        #endregion

        #region ICollection 成员
        void ICollection.CopyTo(Array array, int index)
        {
            if (Count < 1 || array == null || index >= array.Length) return;

            //_array.CopyTo(array, index);
            Array.Copy(_array, 0, array, index, Count);
        }

        bool ICollection.IsSynchronized { get { return true; } }

        private Object _syncRoot;
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }
        #endregion

        #region IEnumerable 成员
        /// <summary>获取枚举器</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _array.Length && i < Count; i++) yield return _array[i];
        }

        IEnumerator IEnumerable.GetEnumerator() { return _array.GetEnumerator(); }
        #endregion
    }
}