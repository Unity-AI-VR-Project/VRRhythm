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
    public enum eButton
    {
        Continue,
        Restart,
        Settings,
        Lobby,
        Chat,
        Close,
        Exit,
        Count
    }
    public enum InGameState
    {
        PreGame,
        Playing,
        PlayAsPaused,
        Paused
    }
    /// <summary>
    /// ��Ʈ �浹 �� ������ �ʿ��� ��� ���� ������ ��� ����ü�Դϴ�.
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
    /// ��Ʈ ���� �� ���Ǵ� ���� ���� ��Ҹ� ��Ÿ���ϴ�.
    /// </summary>
    public struct CutScores
    {
        public float swingAngleBeforeCut; // ��Ʈ�� ġ�� �� ���� ���� (��Ʈ ���̹� ���� 0~100)
        public float swingAngleAfterCut;  // ��Ʈ�� ģ �� ���� ���� (��Ʈ ���̹� ���� 0~100)
        public float cutAccuracy;         // ��Ʈ �߾��� �󸶳� ��Ȯ�� ������� (0~1 ����)
    }
    public struct ChatObjectData
    {
        public DateTime timeStamp;
        public string content;
        public int emoji;

        public ChatObjectData(DateTime timeStamp, string content, int emoji)
        {
            this.timeStamp = timeStamp;
            this.content = content;
            this.emoji = emoji;
        }
    }
    public struct ChatLog
    {
        public eScenes scene;
        public ChatObjectData chatObjectData;
    }
    public struct NoteData
    {
        public float SpawnTime;
        public Vector3 SpawnPosition;
        public NoteDirection RequiredDirection;
        public SaberNoteType RequiredNoteType;
        public Vector3 TargetPosition; // ��Ʈ�� ������� ���� �Ǵ� �÷��̾� ��ġ
        public float TravelDuration; // ��Ʈ�� SpawnPosition���� TargetPosition���� �̵��ϴ� �� �ɸ��� �ð�
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