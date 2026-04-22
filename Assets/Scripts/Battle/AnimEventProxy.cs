using UnityEngine;

public class AnimEventProxy : MonoBehaviour
{
    private BattleEntity parentEntity;

    // ==========================================
    // Unity 生命周期
    // ==========================================
    private void Awake()
    {
        parentEntity = GetComponentInParent<BattleEntity>();

        if (parentEntity == null)
        {
            // 专业级报错：指明出错的物体名称，方便快速定位
            Debug.LogError($"[AnimEventProxy] 致命错误：未能在父级找到 BattleEntity 组件！当前挂载节点: {gameObject.name}");
        }
    }

    // ==========================================
    // 公共接口 (供 Animation Event 调用)
    // ==========================================

    /// <summary>
    /// 接收动画帧事件，并向上传递给核心实体触发判定
    /// </summary>
    public void Proxy_TriggerHitPoint()
    {
        if (parentEntity != null)
        {
            parentEntity.TriggerHitPoint();
        }
    }
}