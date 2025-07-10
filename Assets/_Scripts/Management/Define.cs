using System.Collections.Generic;
using System;
using UnityEngine;

namespace Define
{
    public enum eScenes
    {
        Title,
        Lobby,
        InGame
    }
    public enum SaberNoteType
    {
        Left,
        Right
    }
    public enum NoteDirection
    {
        Up,
        Down,
        Left,
        Right,
        Any
    }
    public enum JudgementType
    {
        Perfect,
        Excellent,
        Good,
        Normal,
        BadCut,
        Miss,
    }
    public enum InGameState
    {
        PreGame,
        Playing,
        PlayAsPaused,
        Paused
    }
    /// <summary>
    /// 노트 충돌 및 판정에 필요한 모든 관련 정보를 담는 구조체입니다.
    /// </summary>
    public struct NoteHitContext
    {
        public GameObject HitNoteObject;
        public Collider NoteCollider;
        public NoteMovement NoteMovement;

        public Saber Saber;
        public Vector3 SaberSwingDirection;

        public NoteDirection RequiredDirection;
        public SaberNoteType RequiredNoteType;
        public Vector3 NoteForward;
        public Vector3 NoteCenter;
        public Vector3 NoteScale;

        public Vector3 HitPoint;
        public float CurrentMusicTime;
        public float TargetMusicTime;
    }
    /// <summary>
    /// 노트 절단 시 계산되는 점수 구성 요소를 나타냅니다.
    /// </summary>
    public struct CutScores
    {
        public float swingAngleBeforeCut; // 노트를 치기 전 스윙 각도 (비트 세이버 기준 0~100)
        public float swingAngleAfterCut;  // 노트를 친 후 스윙 각도 (비트 세이버 기준 0~100)
        public float cutAccuracy;         // 노트 중앙을 얼마나 정확히 맞췄는지 (0~1 사이)
    }
    public struct NoteData
    {
        public float SpawnTime;
        public Vector3 SpawnPosition;
        public NoteDirection RequiredDirection;
        public SaberNoteType RequiredNoteType;
        public Vector3 TargetPosition; // 노트가 사라지는 지점 또는 플레이어 위치
        public float TravelDuration; // 노트가 SpawnPosition에서 TargetPosition까지 이동하는 데 걸리는 시간
    }

    [Serializable]
    public class NoteJsonData
    {
        public float time;
        public string band;
        public int relative_pos_in_beat;
        public float strength;
    }

    [Serializable]
    public class BeatJsonData
    {
        public int beat_index;
        public float beat_time;
        public List<NoteJsonData> notes;
    }

    [Serializable]
    public class MetadataJson
    {
        public float tempo;
        public string time_resolution_unit;
        public string band_group;
    }

    [Serializable]
    public class MapDataJson
    {
        public MetadataJson metadata;
        public List<BeatJsonData> beats;
    }

}