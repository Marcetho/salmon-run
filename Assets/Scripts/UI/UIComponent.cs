using UnityEngine;

/// <summary>
/// Base class for all UI components providing common initialization and cleanup methods.
/// </summary>
public abstract class UIComponent : MonoBehaviour
{
    /// <summary>
    /// Initializes the UI component. Called when the component is first created.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Cleans up any resources used by the UI component. Called when the component is destroyed.
    /// </summary>
    public virtual void Cleanup() { }
}
