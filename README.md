# 세종대학교 ARVRMR (2025-2) - Unity VR 기본 템플릿 설정 가이드

이 문서는 세종대학교 2025년도 2학기 ARVRMR 수업을 위한 Unity VR 프로젝트의 기본 설정 과정을 안내합니다. 

Meta Quest 2/3 환경을 기준으로 합니다.

<img width="1080" height="793" alt="image" src="https://github.com/user-attachments/assets/5f1ea2d7-92e7-4997-b406-13e9073f88f7" />

---

## 1. 프로젝트 환경

* **Unity 버전:** 2023.2.6f1
* **렌더 파이프라인:** URP (Universal Render Pipeline)
* **타겟 플랫폼:** Meta Quest 2 / Quest 3

---

## 2. XR 플러그인 설정

1.  **XR Plug-in Management 설치**
    * `Edit > Project Settings`로 이동합니다.
    * 하단의 `XR Plug-in Management` 탭을 선택하고 `Install` 버튼을 클릭합니다.

2.  **Oculus 플러그인 활성화**
    * 설치가 완료되면, **Android 탭** (Quest 기기 타겟)을 선택하고 **Oculus** 항목을 체크합니다.
    * (PC VR 테스트 시) **PC 탭** (Desktop)을 선택하고 **Oculus** 항목을 체크합니다.
    * `Oculus` 항목 옆에 노란색 경고 아이콘이 나타나면 클릭 후 `Fix All` 또는 `Apply All`을 눌러 권장 설정을 자동 적용합니다.

---

## 3. XR Interaction Toolkit (XRI) 설치

1.  **패키지 매니저 실행**
    * `Window > Package Manager`로 이동합니다.
    * 창 좌측 상단의 `Packages:` 드롭다운 메뉴를 `Unity Registry`로 변경합니다.

2.  **XRI 설치**
    * 검색창이나 목록에서 **XR Interaction Toolkit**을 찾아 선택하고 `Install` 버튼을 클릭합니다.

3.  **Starter Assets 임포트 (필수)**
    * XRI 설치가 완료되면, Package Manager 창 우측의 설명란에서 **Samples** 탭을 엽니다.
    * **Starter Assets** 항목 옆의 `Import` 버튼을 클릭합니다.

---

## 4. 씬(Scene) 기본 설정

1.  **기존 카메라 삭제**
    * Hierarchy 뷰에서 `Main Camera` 오브젝트를 삭제합니다.
      
2.  **XR Origin (VR 플레이어) 배치**
    * Hierarchy 뷰(빈 공간)에서 마우스 오른쪽 버튼을 클릭합니다.
    * 메뉴에서 **`XR > XR Origin (XR Rig)`**를 선택합니다.
    * 메뉴에서 **`XR > Locomotion System`**를 선택한 다음, Inspector 뷰의 `Locomotion System, XR Origin (XR Rig)`을 슬롯에 끌어다 놓습니다.

3.  **컨트롤러 입력 프리셋 설정**
    * `XR Origin (XR Rig)`의 자식 오브젝트인 **`LeftHand Controller`**를 선택합니다.
    * Inspector 뷰의 `XR Controller (Action-based)` 컴포넌트 상단에 있는 **`Preset`** 아이콘을 클릭하고 **`XRI Default LeftHand`**를 선택합니다.
    * **`RightHand Controller`** 오브젝트에도 동일하게 반복하되, **`XRI Default RightHand`** 프리셋을 선택합니다.

4.  **LeftHand 컨트롤러 설정 (Direct Grab 전용)**
    * **`LeftHand Controller`** 오브젝트를 선택합니다.
    * Inspector 뷰에서 `XR Ray Interactor`, `XR Interactor Line Visual`, `XR Ray Reticle` 등의 컴포넌트를 **제거(Remove Component)**합니다.
    * `Add Component` 버튼을 클릭하여 **`XR Direct Interactor`** 컴포넌트를 추가합니다.

---

## 5. 상호작용 구현

### 물체 잡기 (Gra)

1.  잡고 싶은 물체(예: Cube)를 생성합니다.
2.  해당 오브젝트에 `Collider` 컴포넌트를 추가합니다.
3.  물리 효과를 위해 `Rigidbody` 컴포넌트를 추가합니다.
4.  `Add Component`를 눌러 **XR Grab Interactable** 컴포넌트를 추가합니다.
5.  (생략) `LeftHand Controller`의 `XR Direct Interactor`와 `RightHand Controller`의 `XR Ray Interactor`의 **`Interaction Layer Mask`**에 이 물체의 `Layer`가 포함되어 있는지 확인합니다.

### 텔레포트 (Teleportation)

1.  발판으로 사용할 `Cylinder` 오브젝트를 생성합니다.
2.  `Add Component`를 눌러 **Teleportation Anchor** 컴포넌트를 추가합니다.
3.  `Teleportation Anchor` 컴포넌트의 **`Teleportation Provider`** 슬롯에 Hierarchy 뷰의 **`Locomotion System`** 오브젝트를 끌어다 놓습니다.
4.  `Teleport Trigger` 항목을 **`On Select Entered`**로 설정합니다. 
5.  (생략) `RightHand Controller`의 자식 오브젝트인 **`XR Teleport Interactor`**의 **`Interaction Layer Mask`**에 이 발판 오브젝트의 `Layer`가 포함되어 있는지 확인합니다.

---

## 6. VR UI 기본 설정 (Canvas)

1.  **Canvas 생성**
    * Hierarchy 뷰(빈 공간)에서 마우스 오른쪽 버튼 클릭 > **`UI > Canvas`**를 선택합니다.
    * 이때 **`EventSystem`** 오브젝트도 자동으로 함께 생성됩니다.

2.  **Canvas 설정 (World Space)**
    * 방금 생성한 `Canvas` 오브젝트를 선택합니다.
    * Inspector 뷰에서 `Canvas` 컴포넌트의 **`Render Mode`**를 `Screen Space - Overlay`에서 **`World Space`**로 변경합니다.
    * `Canvas` 오브젝트의 **`Graphic Raycaster`** 컴포넌트의 **체크박스를 해제**하여 비활성화합니다.
    * `Add Component` 버튼을 클릭하여 **`Tracked Device Graphic Raycaster`** 컴포넌트를 새로 추가합니다.
    * `Canvas` 오브젝트의 `Rect Transform`를 적절히 조절합니다.

3.  **EventSystem 설정**
    * Hierarchy 뷰에서 `EventSystem` 오브젝트를 선택합니다.
    * 기존에 붙어있는 **`Standalone Input Module`** 컴포넌트를 **제거(Remove Component)**합니다.
    * `Add Component` 버튼을 클릭하여 **`XR UI Input Module`**을 추가합니다.
