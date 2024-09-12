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
using System.Collections.Generic;
using Wjybxx.Commons;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 非并行分支节点抽象（最多只有一个运行中的子节点）
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class SingleRunningChildBranch<T> : BranchTask<T> where T : class
{
#nullable disable
    /** 运行中的子节点索引*/
    [NonSerialized] protected int runningIndex = -1;
    /** 运行中的子节点*/
    [NonSerialized] protected Task<T> runningChild;

    /// <summary>
    /// 被内联运行的子节点
    /// 该字段定义在这里是为了减少抽象层次，该类并不提供功能，需要子类在Start子节点的时候启用内联。
    /// </summary>
    [NonSerialized]
    protected TaskInlineHelper<T> inlineHelper = new TaskInlineHelper<T>();
#nullable enable

    protected SingleRunningChildBranch() {
    }

    protected SingleRunningChildBranch(List<Task<T>>? children) : base(children) {
    }

    protected SingleRunningChildBranch(Task<T> first, Task<T>? second) : base(first, second) {
    }

    #region open

    /** 允许外部在结束后查询 */
    public int RunningIndex => runningIndex;

    /** 获取运行中的子节点 */
    public Task<T>? RunningChild => runningChild;

    /** 是否所有子节点已进入完成状态 */
    public bool IsAllChildCompleted => runningIndex + 1 >= children.Count;

    /** 进入完成状态的子节点数量 */
    public int CompletedCount => runningIndex + 1;

    /** 成功的子节点数量 */
    public int SucceededCount {
        get {
            int r = 0;
            for (int i = 0; i <= runningIndex; i++) {
                if (children[r].IsSucceeded) {
                    r++;
                }
            }
            return r;
        }
    }

    public ref TaskInlineHelper<T> GetInlineHelper() {
        return ref inlineHelper;
    }

    #endregion

    #region logic

    public override void ResetForRestart() {
        base.ResetForRestart();
        runningIndex = -1;
        runningChild = null;
        inlineHelper.StopInline();
    }

    /** 模板类不重写enter方法，只有数据初始化逻辑 */
    protected override void BeforeEnter() {
        // 这里不调用super是安全的
        runningIndex = -1;
        runningChild = null;
        // inlineHelper.StopInline();
    }

    protected override void Exit() {
        // index不立即重置，允许返回后查询
        runningChild = null;
        inlineHelper.StopInline();
    }

    protected override void StopRunningChildren() {
        Stop(runningChild);
    }

    protected override void OnEventImpl(object eventObj) {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else if (runningChild != null) {
            runningChild.OnEvent(eventObj);
        }
    }

    protected override int Execute() {
        while (true) {
            Task<T>? runningChild = this.runningChild; // onCompleted时会被清理
            if (runningChild == null) {
                this.runningChild = runningChild = NextChild();
                Template_StartChild(runningChild, true, ref inlineHelper);
            } else {
                Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
                if (inlinedChild != null) {
                    inlinedChild.Template_ExecuteInlined(ref inlineHelper, runningChild);
                } else if (runningChild.IsRunning) {
                    runningChild.Template_Execute(true);
                } else {
                    Template_StartChild(runningChild, true, ref inlineHelper); // 可能继续运行前一个节点
                }
            }
            if (runningChild.IsRunning) {
                return TaskStatus.RUNNING;
            }
            this.runningChild = null;
            int result = OnChildCompleted(runningChild);
            if (result != TaskStatus.RUNNING) {
                return result;
            }
        }
    }

    protected virtual Task<T> NextChild() {
        // 避免状态错误的情况下修改了index
        int nextIndex = runningIndex + 1;
        if (nextIndex < children.Count) {
            runningIndex = nextIndex;
            return children[nextIndex];
        }
        throw new IllegalStateException(IllegalStateMsg());
    }

    /** 没有可继续运行的子节点 */
    protected string IllegalStateMsg() {
        return $"numChildren: {children.Count}, currentIndex: {runningIndex}";
    }

    /// <summary>
    /// 尝试计算结果
    /// </summary>
    /// <param name="child"></param>
    protected abstract int OnChildCompleted(Task<T> child);

    #endregion
}
}