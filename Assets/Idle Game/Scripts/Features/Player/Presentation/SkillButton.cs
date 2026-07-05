using UnityEngine;
using UnityEngine.UI;

public sealed class SkillButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private int slotIndex;
    
    private PlayerSkillController _skillController;

    public void Bind(PlayerSkillController skillController)
    {
        _skillController = skillController;
        
        if(button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void HandleClick()
    {
        _skillController?.TryUseSkill(slotIndex);
    }

    private void OnDestroy()
    {
        if(button != null)
            button.onClick.RemoveListener(HandleClick);
    }
}