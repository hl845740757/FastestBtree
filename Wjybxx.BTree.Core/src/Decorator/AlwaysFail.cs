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

namespace Wjybxx.BTree.Decorator
{
/// <summary>
/// 在子节点完成之后固定返回失败
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class AlwaysFail<T> : Decorator<T> where T : class
{
    private int failureStatus;

    public AlwaysFail() {
    }

    public AlwaysFail(Task<T> child) : base(child) {
    }

    protected override int Execute() {
        if (child == null) {
            return TaskStatus.ToFailure(failureStatus);
        }
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            Template_StartChild(child, true, ref inlineHelper);
        }
        return child.IsCompleted ? TaskStatus.ToFailure(child.Status) : TaskStatus.RUNNING;
    }

    /// <summary>
    /// 失败时使用的状态码
    /// </summary>
    public int FailureStatus {
        get => failureStatus;
        set => failureStatus = value;
    }
}
}