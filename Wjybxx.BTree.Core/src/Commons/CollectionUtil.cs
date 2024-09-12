#region LICENSE

// // Copyright 2024 wjybxx(845740757@qq.com)
// //
// // Licensed under the Apache License, Version 2.0 (the "License");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Collections
{
internal static class CollectionUtil
{
    /// <summary>
    /// 全局共享random
    /// </summary>
    public static readonly Random SharedRandom = new Random();

#nullable disable

    #region indexref

    /** 查询List中是否包含指定对象引用 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsRef<T>(this IList<T> list, T element) where T : class {
        return IndexOfRef(list, element, 0, list.Count) >= 0;
    }

    /** 查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfRef<T>(this IList<T> list, object element) where T : class {
        return IndexOfRef(list, element, 0, list.Count);
    }

    /** 反向查对象引用在数组中的下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfRef<T>(this IList<T> list, object element) where T : class {
        return LastIndexOfRef(list, element, 0, list.Count);
    }

    /// <summary>
    /// 查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="element">要查找的元素</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int IndexOfRef<T>(this IList<T> list, object element, int start, int end) where T : class {
        if (list == null) throw new ArgumentNullException(nameof(list));
        if (element == null) {
            for (int i = start; i < end; i++) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = start; i < end; i++) {
                if (element == list[i]) {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向查对象引用在数组中的下标
    /// </summary>
    /// <param name="list">数组</param>
    /// <param name="element">要查找的元素</param>
    /// <param name="start">开始下标，包含</param>
    /// <param name="end">结束下标，不包含</param>
    /// <typeparam name="T"></typeparam>
    public static int LastIndexOfRef<T>(this IList<T> list, object element, int start, int end) where T : class {
        if (element == null) {
            for (int i = end - 1; i >= start; i--) {
                if (list[i] == null) {
                    return i;
                }
            }
        } else {
            for (int i = end - 1; i >= start; i--) {
                if (element == list[i]) {
                    return i;
                }
            }
        }
        return -1;
    }

    /** 从List中删除指定引用 */
    public static bool RemoveRef<T>(this IList<T> list, object element) where T : class {
        int index = IndexOfRef(list, element);
        if (index < 0) {
            return false;
        }
        list.RemoveAt(index);
        return true;
    }

    #endregion

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Swap<T>(this IList<T> list, int i, int j) {
        T a = list[i];
        T b = list[j];
        list[i] = b;
        list[j] = a;
    }

#nullable enable
    /// <summary>
    /// 洗牌算法
    /// 1.尽量只用于数组列表
    /// 2.DotNet8开始自带洗牌算法
    /// </summary>
    /// <param name="list">要打乱的列表</param>
    /// <param name="rnd">随机种子</param>
    /// <typeparam name="T"></typeparam>
    public static void Shuffle<T>(IList<T> list, Random? rnd = null) {
        rnd ??= SharedRandom;
        int size = list.Count;
        for (int i = size; i > 1; i--) {
            Swap(list, i - 1, rnd.Next(i));
        }
    }
}
}