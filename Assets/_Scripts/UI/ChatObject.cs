using UnityEngine;
using TMPro;
using Define;

public class ChatObject : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeStamp;
    [SerializeField] private TextMeshProUGUI chatContent;
    [SerializeField] private TextMeshProUGUI emoji;

    public void SetChatObject(ChatObjectData data)
    {
        timeStamp.text = $"[{data.timeStamp.Hour}:{data.timeStamp.Minute}]";
        chatContent.text = data.content;
        emoji.text = $"<sprite={data.emoji}>";
    }
}
