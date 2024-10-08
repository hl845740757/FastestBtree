# FastestBtree

这是传统的心跳驱动行为树，但性能超越事件驱动行为树，这也将是市面上性能最高的行为树实现。

得益于偶然的灵感，使得心跳驱动的内联优化可以比事件驱动更优，因此其极限性能要要优于我commons仓库的事件驱动的行为树；
不过，虽然这版的行为树性能很极致，但由于其不支持事件驱动，因此适用场景有限，在AI领域适用是可以的，在其它领域就要慎重。

该仓库是为了验证算法的其可行性，大家如果想使用也可以使用；如果想要适用性更广的通用任务树，可从我的commons库下载事件驱动行为树的源码。

PS：
1. 想了解详细的原理，可研究`Task`类源码；也可关注公众号，或直接微信搜索《行为树：极限性能优化》。
2. 该仓库没有任何外部依赖，可独立使用；测试用例也可以直接运行。
3. 从事件驱动改为心跳驱动，共1个commit，大家可以看见整个过程的修改 -- 我尽量保持了最小变化。
4. 限于精力，这里没有提供提供心跳驱动版的FSM节点 -- 在心跳驱动框架下不是很有意义。

# TaskTree/Btree(任务树/行为树)

对于行为树，我有非常多想写的内容，但限于篇幅，这里只对核心部分进行说明。  
在开始以前，我要强调一下，我这里的提供的其实是通用的任务树，而不是局限于游戏AI的结构，你可以用它做你一切想要的逻辑，技能脚本、副本脚本、任务脚本。。。

仓库说明：该仓库和Dson仓库一样，都是从Bigcat仓库分离出来的，因为行为树的核心逻辑是与业务无关的，可以很通用。

## Task(任务)

Task代表一个异步任务，不过与一般异步任务不同的是：Task不会自动运行，需要用户每帧调用`TaskEntry`的`update`方法来驱动任务的执行。

可简单表示为：

``` java 
   public void mainLoop() {
      int frame = 0;
      while (true) { // 这个死循环指线程的死循环，在游戏开发中称为主循环
         frame++;
         taskEntry.update(frame);
      }
   }
```

## Context(上下文)

Task的上下文由三部分组成：黑板、取消令牌、共享属性。  
每个Task都可以有独立的上下文，当未指定Task的上下文时，会默认从父节点中捕获缺失的上下文。

```java
    class Task {
        Object blackboard;
        CancelToken cancelToken;
        Object sharedProps;
    }
```

### Blackboard(黑板)

有过行为树编程经验的，一定对黑板很熟悉。简单说：**黑板就是一块共享内存**，Task通过黑板共享信息，Task既从黑板中读取(部分)
输入，也将(部分)输出写入黑板。

在我们的实现中，每个Task都可以有独立的黑板，未显示绑定的情况下，默认从父节点继承；Task并未限定黑板的类型，黑板完全由用户自身实现。

#### 黑板实现的指导

1. 黑板通常需要实现父子关系，在当前黑板查询失败时自动从父黑板中查询 -- (local + share 或 local + parent)。
2. 不要在黑板中提供过多的功能，尽量只保持简单的数据读写，就好像我们使用内存一样。
3. 如果要实现数据更新广播，可参考volatile字段的设计，即只有特定标记的字段更新才派发事件 -- 避免垃圾事件风暴。

### CancelToken(取消令牌)

由于Task是复杂的树形结构，而取消要作用于一组任务，而不是单个任务，因此这些任务必须共享一个上下文来实现监听；
我考虑过将取消信号放入黑板，但这可能导致糟糕的API，或限制黑板的扩展；而通过额外的字段来共享对象将大幅简化设计，而没有明显的缺陷。

#### 协作式取消

任何的异步任务，不论单线程还是多线程，立即取消通常都是不安全的，因为取消命令的发起者通常不清楚任务当前执行的代码位置；
安全的取消通常需要任务自身检测取消信号来实现，只有任务自身知道如何停止。

在默认情况下，Task只会在心跳模板方法中检测取消信号，而不会向CancelToken注册监听器；不过我提供了控制位允许用户启用自动监听，
通常而言，我们的大多数业务只在TaskEntry启用自动监听接口，因为通常是取消整个任务。

ps：jdk的Future的取消接口，在异步编程如火如荼的今天，已经不太适合了，我后面会写一套自己的Future。

### SharedProps(共享属性/配置)

共享属性用于解决**数据和行为分离**
架构下的配置需求，主要解决策划的配置问题，减少维护工作量。我以技能配置为例，进行简单说明。  
在许多项目中，角色技能是有等级的，或是按公式计算，或是每一级单独配置，但不论选择那种方案，技能数值最好都抽离出来；
就好像脚本一样，技能的数值全部在脚本的开头定义，而逻辑则放在后面，这样策划在调整数值的时候就比较方便。  
当我们使用复杂的树形结构来做技能脚本时，就需要每个Task都需要能读取到这部分属性，共享属性就是为这样的目的服务的。

ps：你可以将共享属性理解为另一个黑板，只是我们只读不写。

## 心跳+事件驱动

行为树虽然提供了事件驱动支持，但**心跳驱动为主，事件驱动为辅**。  
我一直反对纯粹的事件驱动，有几个重要的理由：

1. 子节点将自己加入某个调度队列，导致父节点对子节点丧失控制权，也丧失时序保证 -- Unity开发者尤为严重。
2. 事件驱动会大幅增加代码的复杂度，**因为事件是跳跃性思维，而心跳是顺序思维**。
3. 事件也是一种信号，错失信号可能导致程序陷入错误无法恢复 -- 你玩游戏碰见的很多bug都源于此。
4. 不要老拿性能说问题，首先大多数的行为树不深；另外，**脱离你掌控的代码一定不是好代码**。
5. 本库对行为树做了较多的性能优化，其核心优化为**内联**。

事件驱动分两部分：child的状态更新事件 和 外部事件。  
child状态更新事件，主要是子节点进入完成状态的事件，父节点通常在处理该事件中计算是否结束；
外部事件是指外部通过`onEvent`派发给TaskEntry的事件，叶子节点一般直接处理事件，非并行节点一般转发给运行中的子节点，
并行节点一般派发给主节点（第一个子节点）；如果有特殊的需求，则需要用户自己扩展。

## 内联

行为树性能问题通常来源于自顶向下的update，这会有较多的无效路径。市面上也有一些解决方案，比如：ScheduleMgr，将所有运行中的子节点收集到调度管理器，由调度管理器进行调度。
这个方法我很早就了解了，但我一直没有选择，因为它有两个问题：

1. 它可能改变update的顺序，从而导致逻辑上的变化，不安全。
2. 如果ScheduleMgr要保持和自顶向下的迭代顺序，那么启动和停止节点的开销就会变大，而行为树的节点变化频率是比较高的。

有一天工作的时候，我忽然想到：像Sequence/Selector这类简单的节点，**能不能让它的父节点跳过它，直接驱动它的子节点呢**？  
这个Idea真的太重要了，在进行多次尝试和思考后，我发现多数的非并行节点（含装饰节点）都是可以被内联的，这使得我们的行为树的Update路径大幅缩短。

相比SchedulerMgr，内联实现的优点：

1. 保持了自顶向下update的时序，保证了逻辑的一致性！
2. 实现简单，开销小。

下面给出一段内联的实现：

```java
    // Inverter类
    protected override int Execute() {
        Task<T>? inlinedChild = inlineHelper.GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.Template_ExecuteInlined(ref inlineHelper, child);
        } else if (child.IsRunning) {
            child.Template_Execute(true);
        } else {
            Template_StartChild(child, true, ref inlineHelper);
        }
        return child.IsCompleted ? TaskStatus.Invert(child.Status) : TaskStatus.RUNNING;
    }
```

PS：关于行为树的更多优化，以及更详细的解释，会在公众号上分享。

## reentry(重入)

重入的概念：重入是指Task上一次的执行还未完全退出，就又被父节点再次启动。  
以状态机为例（状态机中最常见），假设现在有一个状态A，在`execute`时检测到条件满足，请求状态机再次切换为自己；
由于上一次的执行尚未完全退出，因此现在**有"两个"状态A都在execute代码块**，我们称这种情况为重入。

重入的危险性：调用状态机的`changeState`会触发当前状态的`exit`方法，然后触发新状态的`enter`方法，对于前一个状态A的执行而言，task的上下文已彻底变更；
如果前一个状态A的执行没有立即return，就可能访问到错误的数据，从而造成错误 --
*在以往的工作中便出现过忘记return导致NPE的情况*。

ps：想到一个经常遇见的问题，List在迭代的时候删除元素。

```java
    public void enter() {
    // 初始化一些数据
    }
    
    public void execute() {
        if (cond()) {
            stateMachine.changeState(this); // 自己切换自己的情况不常见（但存在），更多的情况是不知不觉中绕一圈。
            return; // 这里如果没有return是有风险的
        }
    }
    
    public void exit() {
        // 清理一些数据
    }
```

### 重入检测

在说解决方案前，我先说一点心得：  
**代码是逻辑，是不确定的东西，你永远无法判断用户逻辑的正确性，代码应该怎样运行，应当由用户说了算。**
你不能因为担心出错，而禁止用户的合法需求；另外，你认为可能是错误的东西，可能是在用户的掌控中，而是正确的。
因此，要么你什么也不做，要么就帮助用户检测错误。如果你的系统在某些情况下会出错，而其它情况下不会出错，那这个系统就不是一个可靠的系统。

**解决方案**：  
我们在每个Task上记录一个`reentryId`，在Task的生命周期发生变更时+1；用户在执行不确定代码前，先将reentryId保存下来，执行不确定代码后，
通过检查重入id的相等性就可得知Task的生命周期是否发生了变更，以及是否已经被重入。

ActionTask的模板方法如下：

```java
    public void execute() {
        int reentryId = getReentryId();
        int status = executeImpl();
        if (isExited(reentryId)) { // 当前任务已退出
            return;
        }
        // ... 更新状态
    }
```

## 个人公众号(游戏开发)

![写代码的诗人](https://github.com/hl845740757/commons/blob/dev/docs/res/qrcode_for_wjybxx.jpg)