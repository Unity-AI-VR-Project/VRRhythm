using UnityEngine;
using System.Collections.Generic;
using Define;

public class DataManager : ManagerBase
{
    public List<ChatLog> chatLogs = new List<ChatLog>();

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        base.Initialize();
    }
}
