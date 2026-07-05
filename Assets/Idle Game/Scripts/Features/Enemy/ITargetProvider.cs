using UnityEngine;

public interface ITargetProvider
{
    Transform GetNearestTarget(Vector2 from);
}