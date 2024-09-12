﻿#region LICENSE

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

using System.Collections.Generic;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class Selector<T> : SingleRunningChildBranch<T> where T : class
{
    public Selector() {
    }

    public Selector(List<Task<T>>? children) : base(children) {
    }

    public Selector(Task<T> first, Task<T>? second) : base(first, second) {
    }

    protected override int Enter() {
        if (children.Count == 0) {
            return TaskStatus.CHILDLESS;
        } else if (IsCheckingGuard()) {
            // 条件检测性能优化
            for (int i = 0; i < children.Count; i++) {
                Task<T> child = children[i];
                if (Template_CheckGuard(child)) {
                    return TaskStatus.SUCCESS;
                }
            }
            return TaskStatus.ERROR;
        }
        return TaskStatus.RUNNING;
    }

    protected override int OnChildCompleted(Task<T> child) {
        if (child.IsCancelled) {
            return TaskStatus.CANCELLED;
        }
        if (child.IsSucceeded) {
            return TaskStatus.SUCCESS;
        } else if (IsAllChildCompleted) {
            return TaskStatus.ERROR;
        } else {
            return TaskStatus.RUNNING;
        }
    }
}
}