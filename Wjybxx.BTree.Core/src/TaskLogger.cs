#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;

#if UNITY_2018_4_OR_NEWER
using UnityEngine;
#endif

namespace Wjybxx.BTree
{
/// <summary>
/// 用于行为树记录日志
/// </summary>
public static class TaskLogger
{
    public static void Info(string format, params object?[] args) {
#if UNITY_2018_4_OR_NEWER
        Debug.LogFormat(format, args);
#else
        Console.WriteLine(format, args);
#endif
    }

    public static void Info(Exception? ex, string format, params object?[] args) {
#if UNITY_2018_4_OR_NEWER
        Debug.LogFormat(format, args);
        if (ex != null)
        { 
            Debug.LogException(ex);
        }
#else
        Console.WriteLine(format, args);
        if (ex != null) {
            Console.WriteLine(ex);
        }
#endif
    }

    public static void Warning(string format, params object?[] args) {
#if UNITY_2018_4_OR_NEWER
        Debug.LogWarningFormat(format, args);
#else
        Console.WriteLine(format, args);
#endif
    }

    public static void Warning(Exception? ex, string format, params object?[] args) {
#if UNITY_2018_4_OR_NEWER
        Debug.LogWarningFormat(format, args);
        if (ex != null)
        { 
            Debug.LogException(ex);
        }
#else
        Console.WriteLine(format, args);
        if (ex != null) {
            Console.WriteLine(ex);
        }
#endif
    }
}
}