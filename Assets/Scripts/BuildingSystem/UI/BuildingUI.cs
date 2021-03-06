﻿using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : WindowUI
{
    public GameObject buildingInfoCellPrefab;
    public Transform buildingInfoCellsParent;

    public GameObject buildingCellPrefab;
    public Transform buildingCellsParent;

    public GameObject buildingFlagPrefab;

    public ScrollRect cellsRect;

    public GameObject cancelArea;

    public CanvasGroup descriptionWindow;
    public Text nameText;
    public Text desciptionText;

    public CanvasGroup listWindow;
    public Button closeListButton;

    public CanvasGroup infoWindow;
    [HideInInspector]
    public Canvas infoCanvas;
    public Text infoNameText;
    public Text infoDesText;
    public Button mulFuncButton;
    public Button destroyButton;
    public Button closeInfoButton;

    public Button locationGoBackBtn;
    [HideInInspector]
    public CanvasGroup goBackBtnCanvas;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(BuildingManager.Instance.CloseWindow);
        closeListButton.onClick.AddListener(BuildingManager.Instance.HideBuiltList);
        if (!infoWindow.gameObject.GetComponent<GraphicRaycaster>()) infoWindow.gameObject.AddComponent<GraphicRaycaster>();
        infoCanvas = infoWindow.GetComponent<Canvas>();
        infoCanvas.overrideSorting = true;
        infoCanvas.sortingLayerID = SortingLayer.NameToID("UI");
        closeInfoButton.onClick.AddListener(BuildingManager.Instance.HideInfo);
        goBackBtnCanvas = locationGoBackBtn.gameObject.AddComponent<CanvasGroup>();
        goBackBtnCanvas.ignoreParentGroups = true;
        locationGoBackBtn.onClick.AddListener(BuildingManager.Instance.LocationGoBack);
    }
}
