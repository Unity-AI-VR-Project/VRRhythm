using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class MusicDisplayObject : MonoBehaviour
{
    [SerializeField] private Image MusicCover;
    [SerializeField] private TextMeshProUGUI Name;
    [SerializeField] private TextMeshProUGUI Artist;
    [SerializeField] private TextMeshProUGUI BPM;
    [SerializeField] private TextMeshProUGUI Length;

    public void SetObject(int number, string name, string artist, int bpm, string length)
    {
        Sprite loadedSprite = Resources.Load<Sprite>("Music/Cover/" + number);
        if(loadedSprite != null)
        {
            MusicCover.sprite = loadedSprite;
        }
        Name.text = $"{name}";
        Artist.text = $"아티스트 : {artist}";
        BPM.text = $"BPM : {bpm}";
        Length.text = $"노래 길이 : {length}";
    }
}
