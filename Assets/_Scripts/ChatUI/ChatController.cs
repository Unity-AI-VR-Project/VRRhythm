using UnityEngine;
using TMPro;
public class ChtController : MonoBehaviour
{
    [SerializeField]
    private GameObject textChatPrefb;// 대화를 출력하는 Text UI 프리펩
    [SerializeField]
    private Transform paretcontent;//대화가 출력되는  Scrollview의 content



    [SerializeField]
    private TMP_InputField inputField;//대화 입력창


    private string ID = "P";

    // Update is called once per frame
    void Update()
    {
        //대화 입력창이 포커스 되어있지 않을  때 Enter키를 누르면 
        if (Input.GetKeyDown(KeyCode.Return) && inputField.isFocused == false)
        {
            //내외 입력창의 포커스를 활성화
            inputField.ActivateInputField();

        }

    }
    /// <summary>
    /// InputField 입력 종료 후 Enter키 등을 이용해 Focus를 비활성화할 때 호출
    /// </summary>
    public void OnendEditEventMethod()
    {

        //Enter키를누르면 대화 입력창에 입력된 내용을 대화창에 출력
        if (Input.GetKeyDown(KeyCode.Return))
        {
            UpdateChat();

        }


    }
    /// <summary>
    /// Enter키 or버튼을 눌러 InputField에 작성된 내용을 대화창에 출력
    /// </summary>
    public void UpdateChat()
    {
        //InputField가 비어있으면 종료
        if (inputField.text.Equals("")) return;

        //대화 내용 출력을 위해 Text UI 생성(TextChatPrefab을 복제 생성해서 ParentContent의 자식으로 배치)
        GameObject clone = Instantiate(textChatPrefb, paretcontent);

        //대화 입력창에 있는  내용을 대화창에  출력(ID:내용)
        clone.GetComponent<TextMeshProUGUI>().text = $"{ID}:{inputField.text}";

        //대화 입력창에 있는 내용초기화
        inputField.text = "";


    }

}
