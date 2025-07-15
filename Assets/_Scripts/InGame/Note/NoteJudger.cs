using UnityEngine;
using Define;


public class NoteJudger
{
    private float _autoMissTimingWindow;
    private float _perfectTimingWindow;
    private float _excellentTimingWindow;
    private float _goodTimingWindow;


    /// <summary>
    /// NoteJudger 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="autoMiss">자동 미스 판정 시간 임계값입니다.</param>
    /// <param name="perfect">퍼펙트 판정 시간 임계값입니다.</param>
    /// <param name="excellent">엑설런트 판정 시간 임계값입니다.</param>
    /// <param name="good">굿 판정 시간 임계값입니다.</param>
    public NoteJudger(float autoMiss, float perfect, float excellent, float good)
    {
        _autoMissTimingWindow = autoMiss;
        _perfectTimingWindow = perfect;
        _excellentTimingWindow = excellent;
        _goodTimingWindow = good;
    }

    /// <summary>
    /// 주어진 매개변수를 기반으로 노트의 최종 판정 타입을 계산합니다.
    /// </summary>
    private JudgementType GetJudgementInternal(
        Vector3 saberSwingDirection,
        float currentMusicTime,
        float targetMusicTime,
        NoteDirection requiredDirection,
        SaberNoteType requiredNoteType,
        SaberNoteType saberType,
        Vector3 noteForward)
    {
        // 방향 일치 여부와 노트 타입 일치 여부를 먼저 검사합니다.
        // 스윙 각도(궤적)와 hitPoint는 더 이상 판정에 직접적인 영향을 주지 않습니다.
        bool isDirectionCorrect = CheckDirection(saberSwingDirection, requiredDirection, noteForward);
        bool isNoteTypeCorrect = CheckNoteType(requiredNoteType, saberType);
        float timeDifference = Mathf.Abs(currentMusicTime - targetMusicTime);

        // 디버그 로그는 문제 해결에 매우 유용하므로 유지합니다.
        Debug.Log($"--- GetJudgementInternal Debug ---");
        Debug.Log($"Time Diff: {timeDifference} (AutoMiss: {_autoMissTimingWindow}, Perfect: {_perfectTimingWindow}, Excellent: {_excellentTimingWindow}, Good: {_goodTimingWindow})");
        Debug.Log($"Is Note Type Correct: {isNoteTypeCorrect} (Required: {requiredNoteType}, Saber: {saberType})");
        Debug.Log($"Is Direction Correct: {isDirectionCorrect} (Required: {requiredDirection}, Swing: {saberSwingDirection.normalized})");
        InGameManager.Instance.debugText.text = $"Is Direction Correct: {isDirectionCorrect} (Required: {requiredDirection}, Swing: {saberSwingDirection.normalized})";


        // 1. 노트 타입이 틀리면 BadCut
        if (!isNoteTypeCorrect)
        {
            Debug.Log("Judgement: BadCut (Note Type Mismatch)");
            return JudgementType.BadCut;
        }

        // 2. 타이밍 윈도우를 벗어나면 Miss
        if (timeDifference > _autoMissTimingWindow)
        {
            Debug.Log("Judgement: Miss (Outside AutoMiss Window)");
            return JudgementType.Miss;
        }

        // 3. 방향이 틀리면 BadCut
        if (!isDirectionCorrect)
        {
            Debug.Log("Judgement: BadCut (Incorrect Direction)");
            return JudgementType.BadCut;
        }

        // 4. 모든 조건(타입, 타이밍, 방향)이 맞으면 타이밍에 따라 최종 판정
        if (timeDifference <= _perfectTimingWindow)
        {
            Debug.Log("Judgement: Perfect");
            return JudgementType.Perfect;
        }
        else if (timeDifference <= _excellentTimingWindow)
        {
            Debug.Log("Judgement: Excellent");
            return JudgementType.Excellent;
        }
        else if (timeDifference <= _goodTimingWindow)
        {
            Debug.Log("Judgement: Good");
            return JudgementType.Good;
        }
        else
        {
            // 이 "BadCut (Fallback Timing)"은 timeDifference가 _goodTimingWindow는 초과했지만
            // _autoMissTimingWindow 내에 있을 때 발생합니다.
            // 이는 'Miss'가 아닌 'Good' 이하의 점수 없는 판정으로 볼 수 있습니다.
            Debug.Log("Judgement: BadCut (Fallback Timing)");
            return JudgementType.BadCut;
        }
    }

    /// <summary>
    /// 세이버 스윙 방향과 노트의 요구되는 방향이 일치하는지 확인합니다.
    /// 스윙 각도(궤적의 길이)는 무시하고, 오직 최종 스윙 방향 벡터의 '방향'만을 판별합니다.
    /// </summary>
    private bool CheckDirection(Vector3 saberSwingDirection, NoteDirection requiredDirection, Vector3 noteForward)
    {
        // NoteDirection.Any는 어떤 방향이든 허용
        // if (requiredDirection == NoteDirection.Any)
        // {
        //     return true; 
        // }

        // 노트의 로컬 방향을 기준으로 판단하기 위해 월드 공간의 Up, Right 벡터를 사용합니다.
        // 현재는 노트가 항상 월드 축 기준으로 스폰되는 것으로 가정합니다.
        Vector3 noteLocalUp = Vector3.up;
        Vector3 noteLocalRight = Vector3.right; // 월드 공간의 Right 벡터

        Vector3 targetDirection = Vector3.zero;
        switch (requiredDirection)
        {
            case NoteDirection.Up: targetDirection = noteLocalUp; break;
            case NoteDirection.Down: targetDirection = -noteLocalUp; break; // 아래 방향
            // 여기를 변경: Left와 Right의 targetDirection을 서로 바꿉니다.
            case NoteDirection.Left: targetDirection = noteLocalRight; break; // 플레이어 기준 왼쪽 노트가 월드 X축 양의 방향을 요구하는 경우
            case NoteDirection.Right: targetDirection = -noteLocalRight; break; // 플레이어 기준 오른쪽 노트가 월드 X축 음의 방향을 요구하는 경우

            // --- 대각선 방향 처리 (필요시 아래 주석 해제하여 사용) ---
            // Define.cs에 NoteDirection.UpLeft 등의 enum이 정의되어 있어야 합니다.
            // case NoteDirection.UpLeft:    targetDirection = (noteLocalUp + noteLocalRight).normalized; break;
            // case NoteDirection.UpRight:   targetDirection = (noteLocalUp + -noteLocalRight).normalized; break;
            // case NoteDirection.DownLeft:  targetDirection = (-noteLocalUp + noteLocalRight).normalized; break;
            // case NoteDirection.DownRight: targetDirection = (-noteLocalUp + -noteLocalRight).normalized; break;
            // ----------------------------------------------------
            default: // 알 수 없는 방향이 들어온 경우 (예: Any가 활성화되지 않았을 때 Any가 들어오면)
                Debug.LogWarning($"CheckDirection: Unknown NoteDirection: {requiredDirection}. Defaulting to false.");
                return false;
        }

        // 스윙 각도 판정을 위한 각도 임계값
        float angleThreshold = 60f; // 이 값을 조절하여 판정의 엄격함을 바꿀 수 있습니다 (예: 45f ~ 75f).

        float angle = Vector3.Angle(saberSwingDirection.normalized, targetDirection.normalized);

        // Debug.Log는 GetJudgementInternal에서 이미 충분히 출력되므로 여기서는 주석 처리합니다.
        // Debug.Log($"스윙 방향: {saberSwingDirection.normalized}, 목표 방향: {targetDirection.normalized}, 계산된 각도: {angle}, 임계값: {angleThreshold}");
        return angle < angleThreshold; // 임계값 이내면 올바른 방향으로 간주
    }

    /// <summary>
    /// 이 함수는 더 이상 직접적으로 사용되지 않습니다.
    /// </summary>
    private Vector3 GetRelativeDirectionVector(NoteDirection direction)
    {
        // 이 함수는 사용되지 않으므로, 호출될 일이 없습니다.
        // 참고용으로 유지.
        switch (direction)
        {
            case NoteDirection.Up: return Vector3.up;
            case NoteDirection.Down: return Vector3.down;
            case NoteDirection.Left: return Vector3.left;
            case NoteDirection.Right: return Vector3.right;
            // case NoteDirection.Any: return Vector3.zero; // Any가 활성화되면 필요
            default: return Vector3.zero;
        }
    }

    /// <summary>
    /// 노트의 요구되는 타입과 세이버의 타입이 일치하는지 확인합니다.
    /// Bomb 타입 처리는 주석 처리되어 있으니 필요하면 활성화하세요.
    /// </summary>
    private bool CheckNoteType(SaberNoteType requiredNoteType, SaberNoteType saberType)
    {
        // Bomb 타입 처리 (폭탄은 어떤 세이버로든 치면 안 되므로 항상 false 반환)
        // if (requiredNoteType == SaberNoteType.Bomb)
        // {
        //     return false; 
        // }

        // Any 타입 노트 처리 (어떤 세이버로든 칠 수 있음)
        // if (requiredNoteType == SaberNoteType.None) // None이 Any를 의미한다면
        // {
        //     return true;
        // }

        // 그 외에는 requiredNoteType과 saberType이 정확히 일치해야 함
        return requiredNoteType == saberType;
    }

    /// <summary>
    /// 컷 점수(스윙 각도 및 정확도)를 계산합니다.
    /// 이 함수는 이제 각도나 정확도와 상관없이 고정된 최고 점수를 반환합니다.
    /// </summary>
    public CutScores CalculateCutScores(
        Vector3 saberSwingDirection, // 이 매개변수는 더 이상 점수 계산에 사용되지 않습니다.
        Vector3 noteForward,         // 이 매개변수는 더 이상 점수 계산에 사용되지 않습니다.
        Vector3 hitPoint,            // 이 매개변수는 더 이상 점수 계산에 사용되지 않습니다.
        Vector3 noteCenter,          // 이 매개변수는 더 이상 점수 계산에 사용되지 않습니다.
        Vector3 noteScale)           // 이 매개변수는 더 이상 점수 계산에 사용되지 않습니다.
    {
        // 스윙 각도나 중앙 정확도를 무시하고, 방향만 맞으면 항상 최고 점수를 부여합니다.
        // 비트세이버의 최대 컷 점수 115점 중 70점(Pre-Swing) + 15점(Post-Swing) + 30점(Center)
        // 여기서는 Pre-Swing 점수(70점)만 사용하고, Post-Swing과 Center는 0으로 처리하거나,
        // 단순화하여 고정된 점수를 줄 수 있습니다. 90점은 Pre-Swing과 Post-Swing의 합과 유사합니다.
        float fixedBaseScore = 90f; // 스윙 각도나 정확도 무시하고 방향만 맞으면 부여되는 기본 점수

        float swingAngleAfterCut = 0f; // 이 점수는 사용하지 않음
        float cutAccuracy = 1f; // 이 값은 더 이상 점수 계산에 사용되지 않음

        Debug.Log($"세이버 스윙 방향 (CalculateCutScores 호출 시): {saberSwingDirection}");
        return new CutScores
        {
            swingAngleBeforeCut = fixedBaseScore, // 고정 점수 부여
            swingAngleAfterCut = swingAngleAfterCut,
            cutAccuracy = cutAccuracy
        };
    }

    /// <summary>
    /// 노트 충돌 정보를 받아 판정을 계산하고, 그 결과에 따라 게임 상태를 업데이트합니다.
    /// 최종적으로 NoteManager에게 노트 컷 연출을 요청합니다.
    /// </summary>
    public void JudgeAndProcessNote(GameObject hitNoteObject, Saber saber, Collider noteCollider, MusicSynchronizer timeChecker)
    {
        // 필수 컴포넌트 유효성 검사
        if (saber == null)
        {
            Debug.LogWarning($"NoteJudger: 처리 종료 - Saber 컴포넌트를 찾을 수 없습니다.");
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        Note noteComponent = hitNoteObject.GetComponent<Note>();
        if (noteComponent == null)
        {
            Debug.LogWarning($"NoteJudger: 충돌한 오브젝트({hitNoteObject.name})에 Note 컴포넌트가 없습니다.", hitNoteObject);
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        if (noteMovement == null)
        {
            Debug.LogWarning($"NoteJudger: 충돌한 오브젝트({hitNoteObject.name})에 NoteMovement 컴포넌트가 없습니다. 판정이 어려울 수 있습니다.", hitNoteObject);
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        // NoteHitContext 데이터 구성
        NoteHitContext context = new NoteHitContext
        {
            HitNoteObject = hitNoteObject,
            NoteCollider = noteCollider,
            NoteMovement = noteMovement,

            Saber = saber,
            SaberSwingDirection = saber.GetSwingDirection(), // Saber 스크립트에 GetSwingDirection() 메서드가 있어야 함

            RequiredDirection = noteComponent.requiredDirection,
            RequiredNoteType = noteComponent.requiredNoteType,
            NoteForward = noteComponent.transform.forward,
            NoteCenter = noteComponent.transform.position,
            NoteScale = noteComponent.transform.localScale,

            HitPoint = noteCollider.ClosestPoint(saber.transform.position), // hitPoint는 여전히 시각 연출에 사용될 수 있습니다.
            CurrentMusicTime = timeChecker.currentTimeDSP,
            TargetMusicTime = noteMovement.TargetMusicTime
        };

        // 최종 판정 계산
        JudgementType finalJudgement = GetJudgementInternal(
            context.SaberSwingDirection,
            context.CurrentMusicTime,
            context.TargetMusicTime,
            context.RequiredDirection,
            context.RequiredNoteType,
            context.Saber.saberNoteType,
            context.NoteForward
        );

        // 컷 점수 계산 (이제 이 함수는 각도나 정확도와 무관하게 고정 점수 반환)
        CutScores cutScores = CalculateCutScores(
            context.SaberSwingDirection, context.NoteForward, context.HitPoint, context.NoteCenter, context.NoteScale);

        // 판정 결과 적용 (점수, 콤보, 데미지, 파티클)
        ApplyJudgementResult(finalJudgement, cutScores.swingAngleBeforeCut, cutScores.swingAngleAfterCut, cutScores.cutAccuracy, context);

        // 노트 시각 연출 및 풀 반환 요청
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.HandleNoteCutVisuals(context.HitNoteObject, context.HitPoint, context.SaberSwingDirection);
        }
        else
        {
            Debug.LogError("NoteJudger: NoteManager.Instance가 초기화되지 않았습니다. 노트 컷 연출을 할 수 없습니다.");
            NoteManager.Instance?.ReturnPooledNote(context.HitNoteObject);
        }
    }

    /// <summary>
    /// 판정 결과에 따라 게임 점수, 콤보, 데미지를 처리하고 파티클을 스폰합니다.
    /// 스윙 각도 및 중앙 정확도에 따른 점수 차등은 더 이상 적용되지 않습니다.
    /// </summary>
    private void ApplyJudgementResult(
        JudgementType result,
        float swingAngleBeforeCut, // 이 값은 이제 고정 점수로 간주됩니다.
        float swingAngleAfterCut,  // 더 이상 점수 계산에 사용되지 않습니다.
        float cutAccuracy,         // 더 이상 점수 계산에 사용되지 않습니다.
        NoteHitContext context)
    {
        int score = 0;

        if (InGameManager.Instance == null || ParticlePoolManager.Instance == null)
        {
            Debug.LogError("NoteJudger: InGameManager.Instance 또는 ParticlePoolManager.Instance가 초기화되지 않았습니다. 점수/파티클 처리를 건너뜜.");
            return;
        }
        InGameManager.Instance.debugText.text += $"{result}";
        Debug.Log($"NoteJudger: 판정 결과 - {result}, 노트타입 {context.RequiredNoteType}, 세이버타입{context.Saber.saberNoteType}"); // 스윙 각도, 정확도 로그 제거

        // 판정 타입에 따른 점수 및 게임 상태 업데이트
        if (result == JudgementType.Perfect || result == JudgementType.Excellent || result == JudgementType.Good)
        {
            // CalculateCutScores에서 이미 swingAngleBeforeCut이 고정된 기본 점수를 반환하므로,
            // 추가적인 복잡한 점수 계산 없이 타이밍 승수만 적용합니다.
            score = Mathf.RoundToInt(swingAngleBeforeCut); // 이제 swingAngleBeforeCut 자체가 고정된 기본 점수

            float timingMultiplier;
            switch (result)
            {
                case JudgementType.Perfect: timingMultiplier = 1.0f; break;
                case JudgementType.Excellent: timingMultiplier = 0.9f; break;
                case JudgementType.Good: timingMultiplier = 0.8f; break;
                default: timingMultiplier = 0.6f; break; // 예상치 못한 경우 (방어 코드)
            }

            score = Mathf.RoundToInt(score * timingMultiplier); // 타이밍에 따른 최종 점수 조정

            InGameManager.Instance.AddScore(score);
            InGameManager.Instance.AddCombo();
        }
        else if (result == JudgementType.BadCut)
        {
            InGameManager.Instance.ResetCombo();
            // InGameManager.Instance.TakeDamage(10); // BadCut 시 데미지 (주석 처리됨)
            score = 0;
        }
        else // JudgementType.Miss
        {
            InGameManager.Instance.ResetCombo();
            // InGameManager.Instance.TakeDamage(20); // Miss 시 데미지 (주석 처리됨)
            InGameManager.Instance.MissUpdate();
        }

        ParticlePoolManager.Instance.SpawnParticle(result.ToString(), context.HitNoteObject.transform.position);
    }

    /// <summary>
    /// 비트 세이버 점수 계산 방식에 따라 최종 점수를 계산합니다.
    /// 이 함수는 이제 'swingAngleBeforeCut'을 이미 고정된 점수(예: 90)로 가정합니다.
    /// </summary>
    public int CalculateBeatSaberScore(float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        // swingAngleBeforeCut: 고정된 기본 점수 (예: 90)
        // swingAngleAfterCut, cutAccuracy는 점수 계산에 사용되지 않음
        return Mathf.RoundToInt(swingAngleBeforeCut);
    }
}