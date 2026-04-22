/// <summary>
/// 战斗状态机基类
/// 定义了所有战斗状态的生命周期和上下文引用
/// </summary>
public abstract class BattleState
{
    protected BattleManager battleManager;

    public BattleState(BattleManager manager)
    {
        this.battleManager = manager;
    }

    // ==========================================
    // Lifecycle Methods
    // ==========================================

    /// <summary>
    /// 进入该状态时执行一次 (用于初始化事件与数据)
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// 每帧执行 (需在 MonoBehaviour 的 Update 中被调用)
    /// </summary>
    public virtual void Execute() { }

    /// <summary>
    /// 离开该状态时执行一次 (用于清理与注销事件监听)
    /// </summary>
    public virtual void Exit() { }
}