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

using System.Collections.Generic;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// Switch-选择一个分支运行，直到其结束。
/// Switch的基础实现通过逐个检测child的前置条件实现选择，在分支较多的情况下可能开销较大，
/// 在多数情况下，我们可能只是根据配置选择一个分支，可选择<see cref="ISwitchHandler{T}"/>实现。
///
/// Q：为什么Switch要支持内联？
/// A：Switch有一个重要的用途：决策树。在做出决策以后，中间层的节点就没有价值了，而保留它们会导致较大的运行时开销。
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class Switch<T> : SingleRunningChildBranch<T> where T : class
{
    private ISwitchHandler<T>? handler;

    public Switch() {
    }

    public Switch(List<Task<T>>? children) : base(children) {
    }

    protected override int Enter() {
        if (runningChild == null) {
            int index = SelectChild();
            if (index < 0) {
                runningIndex = -1;
                runningChild = null;
                return TaskStatus.ERROR;
            }
            runningIndex = index;
            runningChild = children[index];
        }
        return TaskStatus.RUNNING;
    }

    protected override int Execute() {
        Task<T> runningChild = this.runningChild; // 完成时会被清理
        Task<T> inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, runningChild);
        } else if (runningChild.IsRunning) {
            runningChild.Template_Execute(true);
        } else {
            Template_StartChild(runningChild, false, ref inlineHelper);
        }
        return runningChild.Status;
    }

    private int SelectChild() {
        if (handler != null) {
            return handler.Select(this);
        }
        for (int idx = 0; idx < children.Count; idx++) {
            Task<T> child = children[idx];
            if (!Template_CheckGuard(child.Guard)) {
                continue;
            }
            return idx;
        }
        return -1;
    }

    protected override int OnChildCompleted(Task<T> child) {
        throw new System.NotImplementedException();
    }

    public ISwitchHandler<T>? Handler {
        get => handler;
        set => handler = value;
    }
}
}