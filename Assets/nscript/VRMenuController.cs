using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // New Input System
using System.Collections.Generic;

public class VRMenuController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("왼손 Secondary 버튼 (퀘스트의 경우 Y버튼)")]
    public InputActionProperty menuButtonAction;

    [Header("UI Objects")]
    public GameObject menuCanvasObj;    // 캔버스 전체 (VRMenuCanvas)
    public GameObject mainPanel;        // 메인 버튼들이 있는 패널
    public GameObject tutorialPanel;    // 튜토리얼 패널

    [Header("Tutorial Settings")]
    public RawImage slideDisplay;          // 설명 이미지가 뜰 곳
    public List<Texture> tutorialSlides; // 슬라이드 이미지 4장 넣을 리스트
    private int currentSlideIndex = 0;

    [Header("Position Settings")]
    public Transform headCamera;        // 플레이어의 머리 (Main Camera)
    public float spawnDistance = 1.5f;  // 눈앞 몇 미터에 띄울지

    // 씬 이동 관리자 (이전에 만든 것 활용)
    public TeleportManager teleportManager;

    private bool isMenuOpen = false;

    void Start()
    {
        // 시작할 때 메뉴 닫기
        CloseMenu();

        // 튜토리얼 초기화
        if (tutorialSlides.Count > 0) UpdateSlide();
    }

    void Update()
    {
        // 버튼이 눌렸는지 체크 (WasPressedThisFrame: 누른 순간 1회 발동)
        if (menuButtonAction.action != null && menuButtonAction.action.WasPressedThisFrame())
        {
            ToggleMenu();
        }
    }

    // --- 메뉴 열기/닫기 로직 ---

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (isMenuOpen)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        menuCanvasObj.SetActive(true);

        // [핵심] 시선 앞에 메뉴 배치하기
        if (headCamera != null)
        {
            // 카메라 위치에서 앞쪽으로 spawnDistance만큼 이동한 위치 계산
            // y축(높이)은 카메라 높이에 맞추되, 고개를 숙여도 메뉴가 기울지 않게 하려면 
            // headCamera.forward 대신 투영된 벡터를 쓸 수도 있지만, 
            // 여기서는 간단하게 카메라 정면을 따릅니다.

            Vector3 targetPosition = headCamera.position + (headCamera.forward * spawnDistance);

            // 메뉴 위치 설정
            menuCanvasObj.transform.position = targetPosition;

            // 메뉴가 플레이어를 바라보게 회전 (UI는 Z축 반대를 바라봐야 정면임에 주의)
            // LookAt을 쓰면 UI가 뒤집힐 수 있으므로, 카메라와 같은 방향을 보게 하거나 
            // 플레이어 쪽으로 회전시킵니다.

            menuCanvasObj.transform.LookAt(new Vector3(headCamera.position.x, menuCanvasObj.transform.position.y, headCamera.position.z));
            menuCanvasObj.transform.Rotate(0, 180, 0); // UI가 나를 보게 180도 회전
        }
    }

    public void CloseMenu()
    {
        isMenuOpen = false;
        menuCanvasObj.SetActive(false);
        // 메뉴 닫을 때 튜토리얼 패널도 초기화
        mainPanel.SetActive(true);
        tutorialPanel.SetActive(false);
    }

    // --- 버튼 기능 연결 ---

    public void OnClick_GoHome()
    {
        teleportManager.TeleportToGlobal(); // 전체 초기화
        CloseMenu(); // 이동 후 메뉴 닫기
    }

    public void OnClick_OpenTutorial()
    {
        Debug.Log("1. 버튼 클릭됨"); // 이게 안 뜨면 -> 1번(OnClick 연결) 문제

        if (mainPanel != null) mainPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        else Debug.LogError("Tutorial Panel이 연결 안 됨!");

        currentSlideIndex = 0;

        Debug.Log($"2. 이미지 개수: {tutorialSlides.Count}"); // 0개면 -> 2번(리스트) 문제

        UpdateSlide();
    }

    public void OnClick_CloseTutorial()
    {
        CloseMenu();
    }
    public void OnClick_CloseMenu()
    {
        tutorialPanel.SetActive(false);
        mainPanel.SetActive(false);
        menuCanvasObj.SetActive(false);
    }
    public void OnClick_RestartStage()
    {
        if (teleportManager != null)
        {
            teleportManager.TeleportToCurrentStage(); // 매니저의 재시작 함수 호출
        }

        CloseMenu(); // 이동 후 메뉴 닫기
    }

    // --- 튜토리얼 슬라이드 조작 ---

    public void OnClick_NextSlide()
    {
        if (tutorialSlides.Count == 0) return;

        currentSlideIndex++;
        if (currentSlideIndex >= tutorialSlides.Count)
        {
            currentSlideIndex = tutorialSlides.Count - 1; // 마지막에서 멈춤 (또는 0으로 루프 가능)
        }
        UpdateSlide();
    }

    public void OnClick_PrevSlide()
    {
        if (tutorialSlides.Count == 0) return;

        currentSlideIndex--;
        if (currentSlideIndex < 0)
        {
            currentSlideIndex = 0; // 처음에서 멈춤
        }
        UpdateSlide();
    }

    private void UpdateSlide()
    {
        if (slideDisplay != null && tutorialSlides.Count > currentSlideIndex)
        {
            slideDisplay.texture = tutorialSlides[currentSlideIndex];
        }
    }
}