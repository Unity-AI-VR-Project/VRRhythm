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
        timeStamp.text = $"[{data.TimeStamp.Hour}:{data.TimeStamp.Minute}]";
        chatContent.text = data.Content;
        emoji.text = $"<sprite={data.Emoji}>";
    }
}
