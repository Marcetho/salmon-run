using UnityEngine;
using TMPro;

public class GameoverTextUI : UIComponent
{
    [SerializeField] private TextMeshProUGUI gameoverText;

    public override void Initialize()
    {
        gameoverText.gameObject.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameoverText.gameObject.SetActive(true);
    }
}
