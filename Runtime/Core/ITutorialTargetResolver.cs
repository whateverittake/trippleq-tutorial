using UnityEngine;

namespace TrippleQ.Tutorial
{
    /// <summary>
    /// Optional: resolve tutorial target by key to avoid direct scene references.
    /// You can ignore this for now and use Play(steps, targets).
    /// </summary>
    public interface ITutorialTargetResolver
    {
        RectTransform Resolve(object key);
    }
}
