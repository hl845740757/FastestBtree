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
/// 简单并发节点。
/// 1.其中第一个任务为主要任务，其余任务为次要任务l；
/// 2.一旦主要任务完成，则节点进入完成状态；次要任务可能被运行多次。
/// 3.外部事件将派发给主要任务。
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleParallel<T> : ParallelBranch<T> where T : class
{
    public SimpleParallel() {
    }

    public SimpleParallel(List<Task<T>>? children) : base(children) {
    }

    protected override void Enter(int reentryId) {
        InitChildHelpers(false);
    }

    protected override void Execute() {
        List<Task<T>> children = this.children;
        int reentryId = ReentryId;
        for (int idx = 0; idx < children.Count; idx++) {
            Task<T> child = children[idx];
            ParallelChildHelper<T> childHelper = GetChildHelper(child);
            Task<T> inlinedChild = childHelper.GetInlinedChild();
            if (inlinedChild != null) {
                inlinedChild.Template_ExecuteInlined(ref childHelper.Unwrap(), child);
            } else if (child.IsRunning) {
                child.Template_Execute(true);
            } else {
                SetChildCancelToken(child, childHelper.cancelToken); // 运行前赋值取消令牌
                Template_StartChild(child, true);
            }
            if (CheckCancel(reentryId)) { // 得出结果或取消
                return;
            }
        }
    }

    protected override void OnChildRunning(Task<T> child) {
        ParallelChildHelper<T> childHelper = GetChildHelper(child);
        childHelper.InlineChild(child);
    }

    protected override void OnChildCompleted(Task<T> child) {
        ParallelChildHelper<T> childHelper = GetChildHelper(child);
        childHelper.StopInline();
        UnsetChildCancelToken(child);

        Task<T> mainTask = children[0];
        if (child == mainTask) {
            SetCompleted(child.Status, true);
        }
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T> mainTask = children[0];
        ParallelChildHelper<T> childHelper = GetChildHelper(mainTask);

        Task<T> inlinedChild = childHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else {
            mainTask.OnEvent(eventObj);
        }
    }
}
}