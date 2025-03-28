using UnityEngine;

public class BoatCollision : MonoBehaviour
{
    private boats boatManager;

    private void Start()
    {
        boatManager = FindFirstObjectByType<boats>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && boatManager != null && boatManager.FoodSpawnerReference != null)
        {
            for (int i = 0; i < boatManager.PointsLostOnHit; i++)
            {
                boatManager.FoodSpawnerReference.DecrementFoodCollected();
            }
            Debug.Log($"Hit by boat! Lost {boatManager.PointsLostOnHit} points!");
        }
    }
}
