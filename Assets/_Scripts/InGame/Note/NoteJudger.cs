using UnityEngine;
using System.Collections.Generic; // List를 사용하지 않지만, 현재 코드에는 포함되어 있어 유지합니다.

public enum JudgementType
{
    Perfect,
    Excellent,
    Good,
    Normal,
    Miss,
    BadCut // 방향이 틀렸을 때
}

public class NoteJudger : MonoBehaviour
{
    // 노트가 어떤 방향으로 베어져야 하는지 정의하는 enum
    public enum NoteDirection
    {
        Up, Down, Left, Right, Any // Any는 닷지 노트 등에 사용
    }

    private bool isActive = false;
    private NoteMover _noteMover; // NoteMover 인스턴스 참조
    
    [Header("사운드 설정")]
    [Tooltip("노트가 베어졌을 때 재생할 효과음.")]
    public AudioClip cutSoundClip; // Unity 에디터에서 사운드 파일을 여기에 드래그 앤 드롭
    private AudioSource _audioSource; // 효과음을 재생할 AudioSource 컴포넌트

    [Header("판정 시간 범위 (초)")] // 시간 기반으로 변경
    [Tooltip("TargetMusicTime 기준 ± 오차 범위")]
    public float perfectTimingWindow = 0.05f; // 예를 들어 ±0.05초 (총 0.1초)
    public float excellentTimingWindow = 0.10f; // 예를 들어 ±0.10초 (총 0.2초)
    public float goodTimingWindow = 0.15f;    // 예를 들어 ±0.15초 (총 0.3초)
    public float normalTimingWindow = 0.20f;  // 예를 들어 ±0.20초 (총 0.4초)
    public float autoMissTimingWindow = 0.30f; // 이 시간 밖이면 Miss (총 0.6초)

    // 추가: 이 노트가 요구하는 베기 방향
    public NoteDirection requiredDirection;
    
    

    /// <summary>
    /// NoteSpawnerTime에서 노트를 스폰한 직후에 호출되어야 합니다.
    /// NoteMover 참조를 설정하고 판정기를 활성화합니다.
    /// </summary>
    void Awake()
    {
        // NoteJudger 초기화 시 MusicTimeChacker가 없으면 에러 발생.
        // 스크립트가 비활성화되면 Initialize 함수를 통한 _noteMover 할당이 안될 수 있습니다.
        // 따라서 NoteJudger는 Initialize를 통해 활성화되는 것이 더 안전합니다.
        // 여기서는 AudioSource 초기화만 진행합니다.

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            // NoteJudger가 붙어있는 GameObject에 AudioSource 컴포넌트가 없으면 추가합니다.
            _audioSource = gameObject.AddComponent<AudioSource>();
            // 필요에 따라 AudioSource의 기본 설정을 조정할 수 있습니다 (예: 3D 사운드 여부, 볼륨 등)
            // _audioSource.spatialBlend = 1.0f; // 3D 사운드로 설정
            // _audioSource.volume = 0.7f;      // 기본 볼륨 설정
            // _audioSource.playOnAwake = false; // Awake 시 자동 재생 방지
        }
    }

    /// <summary>
    /// NoteSpawnerTime에서 노트를 스폰한 직후에 호출되어야 합니다.
    /// NoteMover 참조를 설정하고 판정기를 활성화합니다.
    /// </summary>
    public void Initialize(NoteMover mover)
    {
        if (mover == null)
        {
            Debug.LogError("NoteJudger: Initialize 호출 시 NoteMover가 null입니다.", this);
            enabled = false;
            return;
        }
        _noteMover = mover;
        isActive = true;

        // NoteJudger가 초기화될 때 사운드 클립이 할당되었는지 확인
        if (cutSoundClip == null)
        {
            Debug.LogWarning("NoteJudger: 'Cut Sound Clip'이 할당되지 않았습니다. 노트 절단 시 사운드가 재생되지 않습니다.", this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // _noteMover가 초기화되지 않았거나 이미 판정이 완료된 경우 처리하지 않습니다.
        if (!_noteMover || !isActive) return;
        if (!other.CompareTag("Saber")) return;
        
        // 사벨 컴포넌트로부터 스윙 방향 정보 가져오기
        Saber saber = other.GetComponentInParent<Saber>(); 
        if (saber == null) 
        {
            Debug.LogWarning("NoteJudger: 충돌한 오브젝트의 부모에서 Saber 컴포넌트를 찾을 수 없습니다.", other);
            return;
        }

        Vector3 saberSwingDirection = saber.GetSwingDirection(); // 사벨이 휘둘러진 방향 (정규화된 벡터)
        Vector3 hitPoint = other.ClosestPoint(transform.position); // 충돌 지점

        // --- 1단계: 시간 오차 판정 (Miss 우선) ---
        float currentMusicTime = (float)_noteMover.MusicTimeChacker.elapsedTime;
        float targetMusicTime = _noteMover.TargetMusicTime;
        float timeError = Mathf.Abs(currentMusicTime - targetMusicTime);

        if (timeError > autoMissTimingWindow)
        {
            ApplyJudgement(JudgementType.Miss, 0, 0, 0); // 너무 일찍 또는 늦게 쳐서 Miss
            HandleNoteCut(hitPoint, saberSwingDirection); // 시각적인 절단 효과는 발생시킵니다.
            gameObject.SetActive(false); 
            Invoke("DistroyNotes", 1.5f);
            isActive = false; // 추가적인 트리거 방지
            return;
        }

        // --- 2단계: 베기 방향 일치 여부 확인 (BadCut) ---
        bool isDirectionCorrect = CheckDirection(saberSwingDirection, requiredDirection);

        // --- 3단계: 스윙 각도 및 절단 정확도 계산 ---
        // (이 함수들은 현재 더미 값이므로 실제 비트세이버 로직으로 대체 필요)
        float swingAngleBeforeCut = CalculateSwingAngleBeforeCut(saberSwingDirection, transform.forward); 
        float swingAngleAfterCut = CalculateSwingAngleAfterCut(saberSwingDirection, transform.forward);   
        float cutAccuracy = CalculateCutAccuracy(hitPoint, transform.position, transform.localScale); 

        // --- 4단계: 최종 판정 및 점수 적용 ---
        if (!isDirectionCorrect)
        {
            // 방향이 틀렸다면 BadCut 판정
            ApplyJudgement(JudgementType.BadCut, swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy); 
        }
        else
        {
            // 방향이 맞다면 시간 오차에 따른 세부 판정
            JudgementType finalJudgement;
            if (timeError <= perfectTimingWindow) finalJudgement = JudgementType.Perfect;
            else if (timeError <= excellentTimingWindow) finalJudgement = JudgementType.Excellent;
            else if (timeError <= goodTimingWindow) finalJudgement = JudgementType.Good;
            else finalJudgement = JudgementType.Normal; // goodTimingWindow 범위 안에 들지 못하면 Normal

            ApplyJudgement(finalJudgement, swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy);
        }

        // 잔상으로 클론 생성 및 절단 처리 (이제는 원본 노트를 직접 자릅니다)
        HandleNoteCut(hitPoint, saberSwingDirection); 
        
        // 노트 비활성화 및 일정 시간 후 파괴
        gameObject.SetActive(false); 
        Invoke("DistroyNotes", 1.5f); // 1.5초 후 게임 오브젝트 파괴
        isActive = false; // 판정 완료 플래그
    }

    void DistroyNotes()
    {
        Destroy(gameObject);
    }

    // 사벨 스윙 방향을 계산하는 예시 (Saber 스크립트 내부에 있을 수 있음)
    // 이 예시 코드는 Saber 스크립트가 별도로 존재하고 GetSwingDirection()을 제공한다고 가정합니다.
    /* (Saber 스크립트 예시는 주석 처리) */

    // 베기 방향을 확인하는 헬퍼 함수
    bool CheckDirection(Vector3 saberSwingDir, NoteDirection requiredDir)
    {
        // 노트의 로컬 좌표계로 사벨 스윙 방향 변환 (노트의 화살표 방향과 일치시키기 위함)
        Vector3 localSaberSwingDir = transform.InverseTransformDirection(saberSwingDir);
        // Debug.Log($"Required: {requiredDir}, Saber Swing (Local): {localSaberSwingDir}"); // 디버깅용

        float threshold = 0.7f; // 각도 일치 임계값 (조정 필요)
                                // Dot Product를 사용하여 두 벡터 간의 유사도를 측정
                                // Dot(A, B) = |A||B|cos(theta) => 정규화된 벡터의 경우 cos(theta)

        switch (requiredDir)
        {
            case NoteDirection.Up:      return Vector3.Dot(localSaberSwingDir, Vector3.up) > threshold;
            case NoteDirection.Down:    return Vector3.Dot(localSaberSwingDir, Vector3.down) > threshold;
            case NoteDirection.Left:    return Vector3.Dot(localSaberSwingDir, Vector3.left) > threshold;
            case NoteDirection.Right:   return Vector3.Dot(localSaberSwingDir, Vector3.right) > threshold;
            case NoteDirection.Any:     return true; // Any 방향은 항상 참
            default: return false;
        }
    }

    // 스윙 각도 계산 (이 값들은 점수 계산을 위한 '점수 기여도'로 간주)
    // TODO: 실제 비트세이버 로직은 사벨의 궤적, 속도 등을 추적하여 훨씬 복잡하게 계산됩니다.
    // 현재는 단순히 예시 값입니다.
    float CalculateSwingAngleBeforeCut(Vector3 saberSwingDir, Vector3 noteForward)
    {
        // 100점 만점 중 70점을 차지하는 부분 (최대 100점)
        // 이 함수는 '베기 전 스윙이 얼마나 충분했는지'에 대한 점수를 반환해야 합니다.
        // 예를 들어, 0도에서 100도 사이의 스윙 각도를 점수로 변환 (예: 100도면 100점)
        return 100f; // 현재는 최대 점수 반환 (TODO: 실제 계산)
    }

    float CalculateSwingAngleAfterCut(Vector3 saberSwingDir, Vector3 noteForward)
    {
        // 100점 만점 중 30점을 차지하는 부분 (최대 60점)
        // 이 함수는 '베어낸 후 얼마나 더 스윙이 이어졌는지'에 대한 점수를 반환해야 합니다.
        // 예를 들어, 0도에서 60도 사이의 스윙 각도를 점수로 변환 (예: 60도면 60점)
        return 60f; // 현재는 최대 점수 반환 (TODO: 실제 계산)
    }

    float CalculateCutAccuracy(Vector3 hitPoint, Vector3 noteCenter, Vector3 noteScale)
    {
        // 100점 만점 중 15점을 차지하는 부분 (최대 1.0)
        // 노트의 중심에서 얼마나 정확히 베어냈는지 (0.0 ~ 1.0)
        float maxDistance = Mathf.Max(noteScale.x, noteScale.y, noteScale.z) / 2f; // 노트의 절반 크기
        float distance = Vector3.Distance(hitPoint, noteCenter);
        float accuracy = 1f - Mathf.Clamp01(distance / maxDistance); // 거리가 멀수록 0에 가까워짐
        return accuracy; // TODO: 실제 계산
    }

    // ApplyJudgement 함수 시그니처 변경
    void ApplyJudgement(JudgementType result, float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        int score = 0;
        bool comboIncreased = false;

        switch (result)
        {
            case JudgementType.Perfect:
            case JudgementType.Excellent:
            case JudgementType.Good:
            case JudgementType.Normal: // Normal도 이제 BeatSaber 점수 계산 로직을 따르되, 시간 오차 페널티 적용
                // 비트세이버 점수 계산: 스윙 각도 + 절단 정확도
                score = CalculateBeatSaberScore(swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy);
                
                // 시간 오차에 따른 점수 배율 적용 (판정이 안 좋을수록 배율 감소)
                float timeError = Mathf.Abs((float)_noteMover.MusicTimeChacker.elapsedTime - _noteMover.TargetMusicTime);
                float timingMultiplier = 1.0f; // Perfect 기준

                if (result == JudgementType.Excellent) timingMultiplier = 0.9f; // Excellent는 90%
                else if (result == JudgementType.Good) timingMultiplier = 0.8f;   // Good은 80%
                else if (result == JudgementType.Normal) timingMultiplier = 0.6f; // Normal은 60% (상당한 페널티)

                score = Mathf.RoundToInt(score * timingMultiplier);

                InGameManager.instance.AddScore(score);
                InGameManager.instance.AddCombo();
                comboIncreased = true;
                break;

            case JudgementType.BadCut: // 방향이 틀렸을 때
                InGameManager.instance.ResetCombo(); // 콤보 리셋
                InGameManager.instance.TakeDamage(10); // 적은 데미지 (조정 필요)
                score = 0; // BadCut은 점수 없음
                break;

            case JudgementType.Miss: // 너무 일찍/늦게 치거나 아예 못 쳤을 때
                InGameManager.instance.ResetCombo();
                InGameManager.instance.TakeDamage(20);
                score = 0;
                break;
        }

        Debug.Log($"[{gameObject.name}] 판정 결과: {result}, 점수: {score}, 콤보 증가: {comboIncreased}");
        ParticlePoolManager.instance.SpawnParticle(result.ToString(), transform.position); // 판정 결과에 따른 파티클 생성
    }

    // 비트세이버 점수 계산 로직 (100 + 15점 로직 적용)
    // 각도 및 정확도 점수는 미리 계산되어 전달된다고 가정
    int CalculateBeatSaberScore(float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        // swingAngleBeforeCut: 0~100 사이의 점수
        // swingAngleAfterCut: 0~60 사이의 점수
        // cutAccuracy: 0.0~1.0 사이의 정확도 (곱하기 15점)

        int score = 0;

        // 베기 전 각도 점수 (최대 70점)
        score += Mathf.RoundToInt(swingAngleBeforeCut * 0.7f); // swingAngleBeforeCut이 이미 0~100점 사이 값으로 가정

        // 베기 후 각도 점수 (최대 30점)
        score += Mathf.RoundToInt(swingAngleAfterCut * 0.5f); // swingAngleAfterCut이 이미 0~60점 사이 값으로 가정 (60 * 0.5 = 30)

        // 절단 정확도 점수 (최대 15점)
        score += Mathf.RoundToInt(cutAccuracy * 15f);

        return score;
    }

    // GetScore는 이제 CalculateBeatSaberScore로 대체되므로, 더 이상 사용되지 않습니다.
    // 기존 코드의 GetScore는 Normal 판정에 20점을 주는 역할이었으나,
    // 이제 Normal도 CalculateBeatSaberScore를 기반으로 점수를 계산하고 시간 오차 페널티를 받습니다.
    /*
    int GetScore(JudgementType result)
    {
        return result switch
        {
            JudgementType.Normal => 20,
            _ => 0
        };
    }
    */

    // 노트 절단 및 이펙트 처리를 별도 함수로 분리
    void HandleNoteCut(Vector3 hitPoint, Vector3 saberSwingDirection)
    {
        // 원본 노트에 붙어있는 NoteMover와 NoteJudger 스크립트를 비활성화하거나 제거합니다.
        // 이렇게 해야 NoteMover가 더 이상 노트를 이동시키지 않고, NoteJudger가 중복 판정을 하지 않습니다.
        NoteMover noteMover = GetComponent<NoteMover>();
        if (noteMover != null)
        {
            noteMover.enabled = false; // NoteMover의 Update를 중지
            Destroy(noteMover);        // NoteMover 컴포넌트 제거
        }
        if (_audioSource != null && cutSoundClip != null)
        {
            Debug.Log("사운드 재생");
            _audioSource.PlayOneShot(cutSoundClip);
        }
        // --- 사운드 재생 추가 끝 ---
        
        // 현재 NoteJudger 컴포넌트도 제거합니다.
        Destroy(this); // 현재 스크립트 인스턴스(NoteJudger) 제거

        // 원본 노트의 콜라이더를 비활성화하여 추가 충돌을 방지합니다.
        Collider originalCollider = GetComponent<Collider>();
        if (originalCollider != null)
        {
            originalCollider.enabled = false;
        }
        // --- 사운드 재생 추가 ---
        

        // Cutter.Cut 함수는 이제 이 'gameObject'(원본 노트)를 왼쪽 조각으로 변환하고,
        // 새로운 오른쪽 조각을 생성할 것입니다.
        // Cutter 스크립트 내부에서 생성된 오른쪽 조각에는 DestroyAfterDelay 스크립트가 붙어,
        // 일정 시간 후 자동으로 파괴될 것입니다.
        Cutter.Cut(gameObject, hitPoint, saberSwingDirection); 
        
        // 여기서는 이미 OnTriggerEnter에서 Invoke("DistroyNotes", 1.5f)가 호출되고
        // gameObject.SetActive(false)가 실행되므로, 이 조각(이제 왼쪽 조각)은
        // 일정 시간 후 파괴될 것입니다.
    }
}