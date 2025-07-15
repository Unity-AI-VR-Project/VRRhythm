using System;
using UnityEngine;

public class MusicDisplayButton : MonoBehaviour
{
    public enum LobbyButton { Previous, Select ,Next }
    public LobbyButton type;

    [SerializeField] private MusicDisplay musicDisplay;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Saber"))
        {
            switch (type)
            {
                case LobbyButton.Previous:
                    musicDisplay.PreviousMusic();
                    break;
                case LobbyButton.Select:
                    musicDisplay.SelectMusic();
                    break;
                case LobbyButton.Next:
                    musicDisplay.NextMusic();
                    break;
                default:
                    break;
            }
        }
    }
}
