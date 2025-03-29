using UnityEngine;
using UnityEngine.UI;

public class CreditsController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private GameObject objectToSpin;
    [SerializeField] private GameObject creditsObject;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    // The specific rotation to reset to when disabled
    [SerializeField] private Vector3 resetRotation = new Vector3(0, -104.488f, 0);

    private Animator animator;
    private bool isActive => creditsObject.activeInHierarchy;
    private bool wasActiveLastFrame;

    private void Start()
    {
        // Get the Animator component from the object to spin
        animator = objectToSpin?.GetComponent<Animator>();
        wasActiveLastFrame = isActive;
    }

    private void Update()
    {
        if (objectToSpin != null)
        {
            if (isActive)
            {
                objectToSpin.transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
                if (animator != null) animator.enabled = false;
                wasActiveLastFrame = true;
            }
            else
            {
                if (wasActiveLastFrame)
                {
                    // Reset rotation when transitioning from active to inactive
                    objectToSpin.transform.eulerAngles = resetRotation;
                    wasActiveLastFrame = false;
                }

                if (animator != null) animator.enabled = true;
            }
        }
    }
}