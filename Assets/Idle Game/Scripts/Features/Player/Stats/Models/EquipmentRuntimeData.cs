using System.Collections.Generic;
// 반드시 리펙터링 필요
// 장비 부위를 구분할 수 있는 머신 필요하다.
public sealed class EquipmentRuntimeData
{
    public string ItemId;
    public List<StatModifier> Modifiers = new();
}