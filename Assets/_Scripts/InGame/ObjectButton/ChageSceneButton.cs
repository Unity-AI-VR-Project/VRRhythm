using UnityEngine;

public class ChageSceneButton : ObjectButton
{
    public override void OnButtonClick()
    {
        base.OnButtonClick();
        if (SceneController.Instance != null) // Ensure Instance is not null
        {
            SceneController.Instance.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("SceneController.Instance is null. Ensure SceneController is properly initialized.");
        }
    }
}
