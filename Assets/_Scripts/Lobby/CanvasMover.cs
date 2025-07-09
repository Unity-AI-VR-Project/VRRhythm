using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CanvasMover : MonoBehaviour
{
    // === 옵저버 패턴을 위한 이벤트 선언 ===
    // 현재 노래 인덱스가 변경될 때 외부에 알리는 이벤트 (static으로 변경)
    public static event System.Action<int> OnSongChanged;
    // ======================================

    // 스크롤 방향을 정의하는 Enum
    public enum ButtonType { Previous, Next }
    public ButtonType buttonType; // Inspector에서 이 버튼의 타입을 설정

    [Header("스크롤할 오브젝트들")]
    [Tooltip("여기에 씬에 있는 6개의 캔버스(또는 GameObject)를 드래그하여 할당하세요. X축으로 순서대로 배치해야 합니다.")]
    public List<GameObject> scrollableObjects = new List<GameObject>(); // 스크롤시킬 오브젝트 리스트 (6개)

    [Header("스크롤 설정")]
    [Tooltip("오브젝트 하나가 이동할 거리이자, 오브젝트들 간의 간격 (기본값: 10)")]
    public float scrollDistance = 10f; // 각 오브젝트가 한 번에 이동할 거리 (캔버스 또는 패널 너비)

    [Tooltip("스크롤 애니메이션이 진행될 시간")]
    public float slideDuration = 0.4f; // 스크롤 애니메이션 지속 시간

    // AudioSource와 Clips는 각 CanvasMover 인스턴스에 고유하게 할당될 수 있습니다.
    // 하지만 음악 재생 로직은 하나의 중앙 관리자에서 이루어져야 하므로,
    // 이 변수들은 단 하나의 "오디오 관리자" 역할을 하는 CanvasMover에만 할당되어야 합니다.
    // 여기서는 편의를 위해 `_instance`를 통해 접근하도록 할 것입니다.
    [Header("오디오 설정")]
    [Tooltip("음악을 재생할 AudioSource 컴포넌트를 할당하세요. (하나의 CanvasMover 인스턴스에만 할당!)")]
    public AudioSource audioSource;
    [Tooltip("재생할 음악 클립들을 순서대로 할당하세요. (하나의 CanvasMover 인스턴스에만 할당!)")]
    public  AudioClip[] clips;

    [Header("충돌 감지 쿨다운")]
    [Tooltip("Saber와의 충돌 후 다음 충돌을 감지할 때까지의 시간 (초)")]
    [SerializeField] private float cooldown = 0.5f;
    private float _lastTriggerTime = -999f;

    private bool isScrolling = false;
    private int numberOfObjects;

    // 현재 재생 중인 곡의 인덱스 (static으로 변경하여 모든 인스턴스가 공유)
    public static int currentSongIndex { get; private set; } = 0; // 초기값 0

    // CanvasMover 인스턴스에 대한 static 참조 (싱글턴 패턴과 유사)
    // AudioSource와 clips는 이 인스턴스를 통해서만 접근되어야 합니다.
    private static CanvasMover _instance;

    void Awake()
    {
        // 씬에 CanvasMover가 여러 개 있을 수 있지만,
        // 오디오 관련 변수(audioSource, clips)와 currentSongIndex는
        // 단 하나의 마스터 CanvasMover 인스턴스를 통해 관리되도록 합니다.
        // 현재 이 스크립트를 Previous/Next 버튼에 각각 할당하는 방식이므로,
        // _instance는 둘 중 먼저 Awake되는 스크립트가 될 것입니다.
        // 이 구조에서는 clips와 audioSource가 한 곳에만 할당되어야 합니다.
        if (_instance == null)
        {
            _instance = this;
            // 최초 초기화 시에만 currentSongIndex를 0으로 설정
            currentSongIndex = 0;
        }

        numberOfObjects = scrollableObjects.Count;

        if (numberOfObjects != 9)
        {
            Debug.LogError($"[CanvasMover] 'Scrollable Objects' 리스트에 정확히 9개의 오브젝트를 할당해야 합니다! 현재 {numberOfObjects}개 할당됨.");
            enabled = false;
            return;
        }

        // 오디오 관련 설정은 _instance에서만 검사하고 사용
        if (this == _instance)
        {
            if (audioSource == null)
            {
                Debug.LogError("[CanvasMover] AudioSource가 할당되지 않았습니다. Inspector에서 할당해주세요!");
                enabled = false; // 마스터 스크립트만 비활성화
                return;
            }
            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("[CanvasMover] Clips 배열이 비어있습니다. Inspector에서 음악 클립을 할당해주세요!");
                enabled = false; // 마스터 스크립트만 비활성화
                return;
            }
            if (clips.Length != numberOfObjects)
            {
                Debug.LogWarning($"[CanvasMover] Clips 배열의 길이가 할당된 오브젝트 수 ({numberOfObjects})와 다릅니다. 이는 예상치 못한 동작을 유발할 수 있습니다. (현재 Clips.Length: {clips.Length})");
            }
        }

        // 초기 음악 재생은 _instance가 초기화된 후에 호출되어야 합니다.
        // Start에서 호출하는 것이 더 안전합니다.
    }

    void Start()
    {
        Debug.Log("[CanvasMover] 스크립트가 준비되었습니다. 'Saber'와 충돌하면 스크롤 및 음악이 변경됩니다.");
        LogCurrentPositions();

        // 마스터 인스턴스에서만 초기 음악 재생
        if (this == _instance)
        {
            PlayClip(currentSongIndex);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Saber")) return;
        if (Time.time - _lastTriggerTime < cooldown) return;

        _lastTriggerTime = Time.time;

        if (isScrolling) return;

        bool scrollNext = (buttonType == ButtonType.Next);
        ScrollObjects(scrollNext);
    }

    public void ScrollObjects(bool scrollNext)
    {
        if (isScrolling) return;
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

        for (int i = 0; i < numberOfObjects; i++)
        {
            scrollableObjects[i].transform.position = startPositions[i] + new Vector3(targetMove, 0, 0);
        }

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

        // currentSongIndex는 static이므로 어느 인스턴스에서 호출하든 동일한 값을 업데이트
        currentSongIndex = scrollNext
            ? (currentSongIndex + 1) % clips.Length
            : (currentSongIndex - 1 + clips.Length) % clips.Length;

        // 음악 재생은 static _instance를 통해서만 호출
        _instance.PlayClip(currentSongIndex); // PlayClip은 static이 아니므로 인스턴스 통해 호출

        isScrolling = false;
        Debug.Log("[CanvasMover] 스크롤 완료.");
        LogCurrentPositions();
    }

    private GameObject GetExtremeObject(bool getLeftMost)
    {
        GameObject extremeObj = scrollableObjects[0];
        float extremeX = extremeObj.transform.position.x;

        for (int i = 1; i < numberOfObjects; i++)
        {
            if (getLeftMost)
            {
                if (scrollableObjects[i].transform.position.x < extremeX)
                {
                    extremeX = scrollableObjects[i].transform.position.x;
                    extremeObj = scrollableObjects[i];
                }
            }
            else
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

    private void LogCurrentPositions()
    {
        string log = "[CanvasMover] 현재 오브젝트 X 위치:";
        foreach (GameObject obj in scrollableObjects)
        {
            log += $" {obj.name}: {obj.transform.position.x:F2},";
        }
        Debug.Log(log.TrimEnd(','));
    }

    /// <summary>
    /// 지정된 인덱스의 음악 클립을 AudioSource에 할당하고 재생합니다.
    /// 이 메서드는 반드시 _instance를 통해서만 호출되어야 합니다.
    /// </summary>
    /// <param name="index">재생할 음악 클립의 인덱스</param>
    private void PlayClip(int index)
    {
        // _instance가 아니면 이 메서드 실행을 막음 (오디오 중복 재생 방지)
        if (this != _instance)
        {
            Debug.LogWarning("[CanvasMover] PlayClip은 마스터 인스턴스를 통해서만 호출되어야 합니다.");
            return;
        }

        if (audioSource == null || clips == null || index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[CanvasMover] 오디오 재생에 실패했습니다. AudioSource 또는 Clips 배열을 확인하거나, 유효하지 않은 인덱스 ({index})입니다.");
            return;
        }

        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[CanvasMover] '{clips[index].name}' 음악을 재생합니다. 현재 곡 인덱스: {index}");

        // 음악이 변경되었음을 외부에 알림 (static 이벤트 발생)
        OnSongChanged?.Invoke(index);
    }

    /// <summary>
    /// 다음 곡의 정보를 반환합니다. (static으로 변경)
    /// </summary>
    /// <returns>다음 곡의 AudioClip. 다음 곡이 없거나 유효하지 않으면 null.</returns>
    public static AudioClip GetNextSongClip()
    {
        if (_instance == null || _instance.clips == null || _instance.clips.Length == 0) return null;
        int nextIndex = (currentSongIndex + 1) % _instance.clips.Length;
        return _instance.clips[nextIndex];
    }

    /// <summary>
    /// 이전 곡의 정보를 반환합니다. (static으로 변경)
    /// </summary>
    /// <returns>이전 곡의 AudioClip. 이전 곡이 없거나 유효하지 않으면 null.</returns>
    public static AudioClip GetPreviousSongClip()
    {
        if (_instance == null || _instance.clips == null || _instance.clips.Length == 0) return null;
        int prevIndex = (currentSongIndex - 1 + _instance.clips.Length) % _instance.clips.Length;
        return _instance.clips[prevIndex];
    }
}