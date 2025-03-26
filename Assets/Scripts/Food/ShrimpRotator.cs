using UnityEngine;

public class ShrimpRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private bool rotateX = false;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;

    private void Update()
    {
        Vector3 rotation = new Vector3(
            rotateX ? rotationSpeed : 0f,
            rotateY ? rotationSpeed : 0f,
            rotateZ ? rotationSpeed : 0f
        ) * Time.deltaTime;

        transform.Rotate(rotation);
    }
}

