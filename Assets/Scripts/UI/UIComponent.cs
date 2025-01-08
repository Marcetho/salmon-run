using UnityEngine;

public abstract class UIComponent : MonoBehaviour
{
    public virtual void Initialize() { }
    public virtual void Cleanup() { }
}
