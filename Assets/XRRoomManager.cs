using UnityEngine;
using System.Collections.Generic;

public class XRRoomManager : MonoBehaviour
{
    [Header("전체 시작 위치 (A 버튼용)")]
    public Transform globalStartPoint;   // 처음 씬 시작 위치

    [Header("각 장기 시작 위치 리스트")]
    public List<OrganStart> organStarts = new List<OrganStart>();

    private Transform currentOrganStart;

    // OrganNode에서 organName을 넘겨주면 여기서 해당 시작 위치를 찾아서 저장
    public void SetCurrentOrgan(string organName)
    {
        foreach (var organ in organStarts)
        {
            if (organ.organName == organName)
            {
                currentOrganStart = organ.startPoint;
                Debug.Log($"[XRRoomManager] Current organ = {organName}");
                return;
            }
        }

        Debug.LogWarning($"[XRRoomManager] Organ not found: {organName}");
    }

    public Transform GetCurrentOrganStart()
    {
        return currentOrganStart;
    }

    public Transform GetGlobalStart()
    {
        return globalStartPoint;
    }
}

[System.Serializable]
public class OrganStart
{
    public string organName;
    public Transform startPoint;
}
