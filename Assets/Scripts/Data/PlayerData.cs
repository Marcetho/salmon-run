using UnityEngine;

public enum FishSpecies
{
    Sockeye, Chum, Chinook, Pink, Coho
}
public class PlayerData : MonoBehaviour
{
    [SerializeField] int schoolSize;
    [SerializeField] FishSpecies currentSpecies;
    [SerializeField] int currentStamina;
    [SerializeField] int currentHealth;
    [SerializeField] float swimSpeed;
    [SerializeField] float jumpStrength;

}
