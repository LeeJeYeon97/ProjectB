using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCPositionData", menuName = "Content/NPCPositionData")]
public class NPCPositionData : ScriptableObject
{
    public Transform spawnPosition;

    public List<Transform> entranceQueue;   // 가게 앞 대기줄 순서대로 좌표 리스트
}
