using UnityEngine;
using TMPro;

public class CopyText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textToCopy;

    public void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = textToCopy.text;
    }
}