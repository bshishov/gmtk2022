
using TMPro;
using UnityEngine;

public class BookCanvas : MonoBehaviour
{
    [SerializeField] 
    private TextMeshProUGUI Text;

    public void SetText(string text)
    {
        Text.text = text;
    }
}