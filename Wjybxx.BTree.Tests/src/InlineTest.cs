﻿#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
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

using NUnit.Framework;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;

namespace BTree.Tests;

/// <summary>
/// 内联测试
/// </summary>
public class InlineTest
{
    [Test]
    public void fireEventTest() {
        Task<Blackboard> branch = new Selector<Blackboard>();
        EventAcceptor eventAcceptor = new EventAcceptor();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.AddChild(new Selector<Blackboard>());

        branch = branch.GetChild(0);
        branch.AddChild(new Selector<Blackboard>());

        branch = branch.GetChild(0);
        branch.AddChild(new Selector<Blackboard>());

        branch = branch.GetChild(0);
        branch.AddChild(new Sequence<Blackboard>());

        branch = branch.GetChild(0);
        branch.AddChild(eventAcceptor);

        taskEntry.Update(0); // 先启动

        // 测试事件是否直接到达末端 -- 这个似乎只能debug看调用栈
        string message = "message";
        taskEntry.OnEvent(message);
        Assert.AreEqual(message, eventAcceptor.eventObj);
        
        taskEntry.Update(1); // debug查看心跳调用栈
    }

    private class EventAcceptor : LeafTask<Blackboard>
    {
        internal object? eventObj;

        protected override int Execute() {
            if (RunFrames >= 10) {
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj) {
            this.eventObj = eventObj;
        }
    }
}