using UnityEngine;
using TMPro;

public class GameUI : MonoBehaviour
{
    [Header("Coin UI")]
    public TextMeshProUGUI coinText;

    [Space(10)]

    [Header("Game Over UI")]
    public GameObject playAgainButton;
    public GameObject waitingForHostUI;
    public GameObject gameOverUI;

    [Space(10)]

    [Header("Quota UI")]
    public TextMeshProUGUI quotaText;

    [Space(10)]
    [Header("Dead UI")]
    public GameObject deadUI;
}
