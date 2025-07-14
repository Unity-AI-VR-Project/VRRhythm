using UnityEngine;

public class InGameStartTrigger : ObjectButton
{

    public override void OnButtonClick()
    {
        base.OnButtonClick();

        InGameManager.Instance.StartGame();
    }
}
