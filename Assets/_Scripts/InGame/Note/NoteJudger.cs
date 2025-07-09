using UnityEngine;
using System.Collections.Generic;

// HandType enum은 이제 Enums.cs에 정의되어 있으므로 여기서 제거합니다.
// public enum HandType { ... } <-- 이 부분 삭제!

public enum JudgementType
{
    Perfect,
    Excellent,
    Good,
    Normal,
    Miss,
    BadCut // 방향이 틀렸을 때 또는 손 타입이 틀렸을 때
}

public class NoteJudger : MonoBehaviour
{
    // 노트가 어떤 방향으로 베어져야 하는지 정의하는 enum
    public enum NoteDirection
    {
        Up, Down, Left, Right, Any 
    }

    private bool isActive = false;
    private NoteMover _noteMover; 
    
    [Header("사운드 설정")]
    public AudioClip cutSoundClip; 
    private AudioSource _audioSource; 

    [Header("판정 시간 범위 (초)")] 
    public float perfectTimingWindow = 0.05f; 
    public float excellentTimingWindow = 0.10f; 
    public float goodTimingWindow = 0.15f;    
    public float normalTimingWindow = 0.20f;  
    public float autoMissTimingWindow = 0.30f; 

    public NoteDirection requiredDirection; 
    // 새로 추가: 이 노트가 요구하는 손 타입 - 이제 Enums.cs에 정의된 HandType 사용
    public HandType requiredHandType; 
    
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Initialize 메서드 시그니처 유지 (HandType은 이제 외부에서 참조)
    public void Initialize(NoteMover mover, NoteDirection direction, HandType handType) 
    {
        if (mover == null)
        {
            Debug.LogError("NoteJudger: Initialize 호출 시 NoteMover가 null입니다.", this);
            enabled = false;
            return;
        }
        _noteMover = mover;
        this.requiredDirection = direction; 
        this.requiredHandType = handType;   // 전달받은 handType 할당
        isActive = true;

        if (cutSoundClip == null)
        {
            Debug.LogWarning("NoteJudger: 'Cut Sound Clip'이 할당되지 않았습니다. 노트 절단 시 사운드가 재생되지 않습니다.", this);
        }
        // 디버그: NoteJudger 초기화 완료
        Debug.Log($"NoteJudger 초기화 완료: 노트 이름={gameObject.name}, 요구 방향={requiredDirection}, 요구 손={requiredHandType}, NoteMover 있음={(_noteMover != null)}, isActive={isActive}");
    }

    void OnTriggerEnter(Collider other)
    {
        // === 디버그 로그 추가 시작 ===
        Debug.Log($"OnTriggerEnter 호출됨: 충돌한 오브젝트={other.name}, 태그={other.tag}", other);

        if (!_noteMover || !isActive)
        {
            Debug.LogWarning($"NoteJudger: 초기 종료 - _noteMover가 null({_noteMover == null})이거나 isActive가 false({isActive})입니다.", this);
            return;
        }
        if (!other.CompareTag("Saber"))
        {
            Debug.LogWarning($"NoteJudger: 초기 종료 - 충돌 오브젝트의 태그가 'Saber'가 아닙니다. 현재 태그: {other.tag}", other);
            return;
        }
        
        Saber saber = other.GetComponentInParent<Saber>(); 
        if (saber == null) 
        {
            Debug.LogWarning($"NoteJudger: 초기 종료 - 충돌 오브젝트({other.name}) 또는 부모에서 Saber 컴포넌트를 찾을 수 없습니다.", other);
            return;
        }
        // === 디버그 로그 추가 끝 ===
        
        if (cutSoundClip != null)
        {
            AudioSource.PlayClipAtPoint(cutSoundClip, transform.position);
        }

        Vector3 saberSwingDirection = saber.GetSwingDirection();
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        float currentMusicTime = (float)_noteMover.MusicTimeChacker.elapsedTime;
        // 수정: PerfectHitMusicTime을 사용합니다.
        float targetMusicTime = _noteMover.TargetMusicTime; 
        float timeError = Mathf.Abs(currentMusicTime - targetMusicTime);

        Debug.Log($"타겟 시간 : {targetMusicTime}, 현재 음악 시간 : {currentMusicTime}, 오차 {timeError} ");

        if (timeError > autoMissTimingWindow)
        {
            //Debug.Log($"NoteJudger: 타격 오차({timeError:F3}초)가 허용치({autoMissTimingWindow:F3}초)를 초과하여 Miss 처리됩니다. (노트 음악 시간: {targetMusicTime:F3}초, 현재 음악 시간: {currentMusicTime:F3}초)");
            ApplyJudgement(JudgementType.Miss, 0, 0, 0);
            HandleNoteCut(hitPoint, saberSwingDirection);
            gameObject.SetActive(false); 
            Invoke("DistroyNotes", 1.5f);
            isActive = false;
            return;
        }

        bool isDirectionCorrect = CheckDirection(saberSwingDirection, requiredDirection);
        // 손 타입 일치 여부 확인 (Saber 스크립트에 public HandType saberHandType; 필드가 있다고 가정)
        bool isHandTypeCorrect = (saber.saberHandType == requiredHandType);
        
        // === 사용자 요청 디버그 로그 ===
        //Debug.Log($"손 타입 일치 :[{isHandTypeCorrect}], 타격 오차 : {timeError:F3}, 요구 손: {requiredHandType}, 사벨 손: {saber.saberHandType}");
        // ============================

        float swingAngleBeforeCut = CalculateSwingAngleBeforeCut(saberSwingDirection, transform.forward); 
        float swingAngleAfterCut = CalculateSwingAngleAfterCut(saberSwingDirection, transform.forward);   
        float cutAccuracy = CalculateCutAccuracy(hitPoint, transform.position, transform.localScale); 

        if (!isDirectionCorrect || !isHandTypeCorrect) 
        {
            // Debug.Log($"NoteJudger: BadCut 처리 - 방향 일치: {isDirectionCorrect}, 손 타입 일치: {isHandTypeCorrect}");
            ApplyJudgement(JudgementType.BadCut, swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy); 
        }
        else 
        {
            JudgementType finalJudgement;
            if (timeError <= perfectTimingWindow) finalJudgement = JudgementType.Perfect;
            else if (timeError <= excellentTimingWindow) finalJudgement = JudgementType.Excellent;
            else if (timeError <= goodTimingWindow) finalJudgement = JudgementType.Good;
            else finalJudgement = JudgementType.Normal;

            ApplyJudgement(finalJudgement, swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy);
        }

        HandleNoteCut(hitPoint, saberSwingDirection); 
        
        gameObject.SetActive(false); 
        Invoke("DistroyNotes", 1.5f);
        isActive = false; 
    }

    void DistroyNotes()
    {
        Destroy(gameObject);
    }

    bool CheckDirection(Vector3 saberSwingDir, NoteDirection requiredDir)
    {
        Vector3 localSaberSwingDir = transform.InverseTransformDirection(saberSwingDir);
        float threshold = 0.7f; 

        switch (requiredDir)
        {
            case NoteDirection.Up:      return Vector3.Dot(localSaberSwingDir, Vector3.up) > threshold;
            case NoteDirection.Down:    return Vector3.Dot(localSaberSwingDir, Vector3.down) > threshold;
            case NoteDirection.Left:    return Vector3.Dot(localSaberSwingDir, Vector3.left) > threshold;
            case NoteDirection.Right:   return Vector3.Dot(localSaberSwingDir, Vector3.right) > threshold;
            case NoteDirection.Any:     return true; 
            default: return false;
        }
    }

    float CalculateSwingAngleBeforeCut(Vector3 saberSwingDir, Vector3 noteForward)
    {
        return 100f; // 임시 값
    }

    float CalculateSwingAngleAfterCut(Vector3 saberSwingDir, Vector3 noteForward)
    {
        return 60f; // 임시 값
    }

    float CalculateCutAccuracy(Vector3 hitPoint, Vector3 noteCenter, Vector3 noteScale)
    {
        float maxDistance = Mathf.Max(noteScale.x, noteScale.y, noteScale.z) / 2f;
        float distance = Vector3.Distance(hitPoint, noteCenter);
        float accuracy = 1f - Mathf.Clamp01(distance / maxDistance);
        return accuracy;
    }

    void ApplyJudgement(JudgementType result, float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        int score = 0;
        bool comboIncreased = false;

        switch (result)
        {
            case JudgementType.Perfect:
            case JudgementType.Excellent:
            case JudgementType.Good:
            case JudgementType.Normal:
                score = CalculateBeatSaberScore(swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy);
                
                float timingMultiplier = 1.0f;

                if (result == JudgementType.Excellent) timingMultiplier = 0.9f;
                else if (result == JudgementType.Good) timingMultiplier = 0.8f;
                else if (result == JudgementType.Normal) timingMultiplier = 0.6f;

                score = Mathf.RoundToInt(score * timingMultiplier);

                InGameManager.instance.AddScore(score);
                InGameManager.instance.AddCombo();
                comboIncreased = true;
                break;

            case JudgementType.BadCut:
                InGameManager.instance.ResetCombo();
                InGameManager.instance.TakeDamage(10);
                score = 0;
                break;

            case JudgementType.Miss:
                InGameManager.instance.ResetCombo();
                InGameManager.instance.TakeDamage(20);
                score = 0;
                break;
        }

        Debug.Log($"[{gameObject.name}] 판정 결과: {result}, 점수: {score}, 콤보 증가: {comboIncreased}, 요구 손: {requiredHandType}");
        
        ParticlePoolManager.instance.SpawnParticle(result.ToString(), transform.position);
    }

    int CalculateBeatSaberScore(float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        int score = 0;
        score += Mathf.RoundToInt(swingAngleBeforeCut * 0.7f);
        score += Mathf.RoundToInt(swingAngleAfterCut * 0.5f);
        score += Mathf.RoundToInt(cutAccuracy * 15f);
        return score;
    }

    void HandleNoteCut(Vector3 hitPoint, Vector3 saberSwingDirection)
    {
        NoteMover noteMover = GetComponent<NoteMover>();
        if (noteMover != null)
        {
            noteMover.enabled = false;
            Destroy(noteMover);
        }

        Collider originalCollider = GetComponent<Collider>();
        if (originalCollider != null)
        {
            originalCollider.enabled = false;
        }

        Cutter.Cut(gameObject, hitPoint, saberSwingDirection); 

        gameObject.SetActive(false); 
        Invoke("DistroyNotes", 1.5f); 
        isActive = false; 
    }
}