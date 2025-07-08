using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Linq를 사용하기 위해 추가

public class CanvasMover : MonoBehaviour
{
    // === 옵저버 패턴을 위한 이벤트 선언 ===
    // 현재 노래 인덱스가 변경될 때 외부에 알리는 이벤트
    public static event System.Action<int> OnSongChanged;
    // ======================================

    // 스크롤 방향을 정의하는 Enum
    public enum ButtonType { Previous, Next }
    public ButtonType buttonType; // Inspector에서 이 버튼의 타입을 설정

    [Header("스크롤할 오브젝트들")]
    [Tooltip("여기에 씬에 있는 6개의 캔버스(또는 GameObject)를 드래그하여 할당하세요. X축으로 순서대로 배치해야 합니다.")]
    public List<GameObject> scrollableObjects = new List<GameObject>(); // 스크롤시킬 오브젝트 리스트 (6개)

    [Header("스크롤 설정")]
    [Tooltip("오브젝트 하나가 이동할 거리이자, 오브젝트들 간의 간격 (기본값: 10.4)")]
    public float scrollDistance = 10.4f; // 각 오브젝트가 한 번에 이동할 거리 (캔버스 또는 패널 너비)

    [Tooltip("스크롤 애니메이션이 진행될 시간")]
    public float slideDuration = 0.4f; // 스크롤 애니메이션 지속 시간

    [Header("오디오 설정")]
    [Tooltip("음악을 재생할 AudioSource 컴포넌트를 할당하세요.")]
    public AudioSource audioSource; // 음악을 재생할 AudioSource
    [Tooltip("재생할 음악 클립들을 순서대로 할당하세요. 이 순서가 캔버스의 순서와 매칭됩니다.")]
    public AudioClip[] clips; // 재생할 음악 클립 리스트

    [Header("충돌 감지 쿨다운")]
    [Tooltip("Saber와의 충돌 후 다음 충돌을 감지할 때까지의 시간 (초)")]
    [SerializeField] private float cooldown = 0.5f; // 버튼 재입력을 막기 위한 쿨다운 설정
    private float _lastTriggerTime = -999f; // 마지막 트리거 발생 시간

    private bool isScrolling = false; // 현재 스크롤 중인지 여부
    private int numberOfObjects; // 스크롤할 오브젝트의 총 개수 (여기서는 6)

    // 현재 중앙에 위치한 캔버스의 '원래' 인덱스를 나타냅니다. (이 인덱스는 clips 배열과 매칭됩니다)
    // 이 값을 통해 어떤 음악이 재생되어야 하는지 결정합니다.
    private int currentCenterCanvasOriginalIndex = 0;

    void Awake()
    {
        numberOfObjects = scrollableObjects.Count; // 할당된 오브젝트의 개수 자동 감지

        // 할당된 오브젝트 수가 6개가 아닐 경우 경고 및 스크립트 비활성화
        if (numberOfObjects != 6) // 6개로 변경됨
        {
            Debug.LogError($"[CanvasMover] 'Scrollable Objects' 리스트에 정확히 6개의 오브젝트를 할당해야 합니다! 현재 {numberOfObjects}개 할당됨.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        // AudioSource 할당 확인
        if (audioSource == null)
        {
            Debug.LogError("[CanvasMover] AudioSource가 할당되지 않았습니다. Inspector에서 할당해주세요!");
            enabled = false;
            return;
        }
        // AudioClip 배열 확인
        if (clips == null || clips.Length == 0)
        {
            Debug.LogError("[CanvasMover] Clips 배열이 비어있습니다. Inspector에서 음악 클립을 할당해주세요!");
            enabled = false;
            return;
        }
        // clips 배열의 길이가 numberOfObjects와 일치하는지 확인하는 것이 좋습니다.
        // 예를 들어, 캔버스 6개에 각각 다른 곡을 매칭한다면 clips.Length도 6이어야 합니다.
        if (clips.Length != numberOfObjects)
        {
            Debug.LogWarning($"[CanvasMover] Clips 배열의 길이가 할당된 오브젝트 수 ({numberOfObjects})와 다릅니다. 이는 예상치 못한 동작을 유발할 수 있습니다. (현재 Clips.Length: {clips.Length})");
            // 경고만 띄우고 스크립트를 비활성화하지는 않습니다. 하지만 정확한 매칭을 위해 길이를 맞추는 것이 좋습니다.
        }

        // 초기 음악 재생 (첫 번째 캔버스에 해당하는 곡)
        // 가정: 처음 중앙에 배치된 캔버스(인덱스 0)가 첫 곡을 나타냅니다.
        PlayClip(currentCenterCanvasOriginalIndex);
    }

    void Start()
    {
        Debug.Log("[CanvasMover] 스크립트가 준비되었습니다. 'Saber'와 충돌하면 스크롤 및 음악이 변경됩니다.");
        LogCurrentPositions();
    }

    /// <summary>
    /// 콜라이더가 트리거로 설정된 오브젝트에 다른 콜라이더가 진입했을 때 호출됩니다.
    /// </summary>
    /// <param name="other">충돌한 다른 콜라이더</param>
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트의 태그가 "Saber"인지 확인
        if (!other.CompareTag("Saber"))
        {
            return; // Saber가 아니면 무시
        }

        // 쿨다운 시간 확인 (연속적인 트리거 방지)
        if (Time.time - _lastTriggerTime < cooldown)
        {
            return; // 쿨다운 중이면 무시
        }

        _lastTriggerTime = Time.time; // 마지막 트리거 시간 업데이트

        // 스크롤이 현재 진행 중인지 확인 (중복 스크롤 방지)
        if (isScrolling)
        {
            return;
        }

        // ButtonType에 따라 스크롤 방향 결정
        bool scrollNext = (buttonType == ButtonType.Next);
        ScrollObjects(scrollNext);
    }


    /// <summary>
    /// 할당된 오브젝트들을 지정된 방향으로 스크롤하고 무한 반복 효과를 만듭니다.
    /// </summary>
    /// <param name="scrollNext">true면 다음(왼쪽으로 이동), false면 이전(오른쪽으로 이동)</param>
    public void ScrollObjects(bool scrollNext)
    {
        if (isScrolling) return; // 이미 스크롤 중이면 무시

        StartCoroutine(DoScroll(scrollNext));
    }

    private IEnumerator DoScroll(bool scrollNext)
    {
        isScrolling = true;
        float targetMove = scrollNext ? -scrollDistance : scrollDistance;

        List<Vector3> startPositions = new List<Vector3>();
        foreach (GameObject obj in scrollableObjects)
        {
            startPositions.Add(obj.transform.position);
        }

        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / slideDuration;

            for (int i = 0; i < numberOfObjects; i++)
            {
                scrollableObjects[i].transform.position = Vector3.Lerp(startPositions[i], startPositions[i] + new Vector3(targetMove, 0, 0), lerpT);
            }
            yield return null;
        }

        // 위치 고정
        for (int i = 0; i < numberOfObjects; i++)
        {
            scrollableObjects[i].transform.position = startPositions[i] + new Vector3(targetMove, 0, 0);
        }

        // 무한 스크롤 시 위치 보정
        if (scrollNext)
        {
            GameObject leftMost = GetExtremeObject(true);
            GameObject rightMost = GetExtremeObject(false);
            leftMost.transform.position = new Vector3(rightMost.transform.position.x + scrollDistance, leftMost.transform.position.y, leftMost.transform.position.z);
        }
        else
        {
            GameObject rightMost = GetExtremeObject(false);
            GameObject leftMost = GetExtremeObject(true);
            rightMost.transform.position = new Vector3(leftMost.transform.position.x - scrollDistance, rightMost.transform.position.y, rightMost.transform.position.z);
        }

        // 여기서 음악 인덱스만 직접 갱신
        currentCenterCanvasOriginalIndex = scrollNext
            ? (currentCenterCanvasOriginalIndex + 1) % clips.Length
            : (currentCenterCanvasOriginalIndex - 1 + clips.Length) % clips.Length;

        // 음악 재생
        PlayClip(currentCenterCanvasOriginalIndex);

        isScrolling = false;
        Debug.Log("[CanvasMover] 스크롤 완료.");
        LogCurrentPositions();
    }

    /// <summary>
    /// 스크롤 가능한 오브젝트들 중 가장 왼쪽 또는 가장 오른쪽 오브젝트를 찾습니다.
    /// </summary>
    /// <param name="getLeftMost">true면 가장 왼쪽, false면 가장 오른쪽 오브젝트를 반환합니다.</param>
    /// <returns>가장 왼쪽 또는 가장 오른쪽 오브젝트 GameObject.</returns>
    private GameObject GetExtremeObject(bool getLeftMost)
    {
        GameObject extremeObj = scrollableObjects[0];
        float extremeX = extremeObj.transform.position.x;

        for (int i = 1; i < numberOfObjects; i++)
        {
            if (getLeftMost) // 가장 왼쪽을 찾는 경우
            {
                if (scrollableObjects[i].transform.position.x < extremeX)
                {
                    extremeX = scrollableObjects[i].transform.position.x;
                    extremeObj = scrollableObjects[i];
                }
            }
            else // 가장 오른쪽을 찾는 경우
            {
                if (scrollableObjects[i].transform.position.x > extremeX)
                {
                    extremeX = scrollableObjects[i].transform.position.x;
                    extremeObj = scrollableObjects[i];
                }
            }
        }
        return extremeObj;
    }

    /// <summary>
    /// 현재 오브젝트들의 X 위치를 디버그 로그로 출력합니다.
    /// </summary>
    private void LogCurrentPositions()
    {
        string log = "[CanvasMover] 현재 오브젝트 X 위치:";
        foreach (GameObject obj in scrollableObjects)
        {
            log += $" {obj.name}: {obj.transform.position.x:F2},"; // 소수점 두 자리까지 표시
        }
        Debug.Log(log.TrimEnd(',')); // 마지막 쉼표 제거
    }

    /// <summary>
    /// 지정된 인덱스의 음악 클립을 AudioSource에 할당하고 재생합니다.
    /// </summary>
    /// <param name="index">재생할 음악 클립의 인덱스</param>
    private void PlayClip(int index)
    {
        if (audioSource == null || clips == null || index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[CanvasMover] 오디오 재생에 실패했습니다. AudioSource 또는 Clips 배열을 확인하거나, 유효하지 않은 인덱스 ({index})입니다.");
            return;
        }

        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[CanvasMover] '{clips[index].name}' 음악을 재생합니다. 현재 곡 인덱스: {index}");

        // === 음악이 변경되었음을 외부에 알림 (이벤트 발생) ===
        OnSongChanged?.Invoke(index);
        // ====================================================
    }
}