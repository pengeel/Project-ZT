﻿using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BackpackUI : MonoBehaviour
{
    public CanvasGroup backpackWindow;

    [HideInInspector]
    public Canvas windowCanvas;

    public Dropdown pageSelector;

    public GameObject itemCellPrefab;
    public Transform itemCellsParent;

    public Text money;
    public Text weight;
    public Text size;

    public Button closeButton;
    public Button sortButton;
    public MakingTool handworkButton;

    public DiscardArea discardArea;
    public ScrollRect gridRect;
    public Image gridMask;

    private void Awake()
    {
        if (!backpackWindow.GetComponent<GraphicRaycaster>()) backpackWindow.gameObject.AddComponent<GraphicRaycaster>();
        windowCanvas = backpackWindow.GetComponent<Canvas>();
        windowCanvas.overrideSorting = true;
        windowCanvas.sortingLayerID = SortingLayer.NameToID("UI");
        closeButton.onClick.AddListener(BackpackManager.Instance.CloseWindow);
        sortButton.onClick.AddListener(BackpackManager.Instance.Sort);
        pageSelector.onValueChanged.AddListener(BackpackManager.Instance.SetPage);
        //if (!discardArea.GetComponent<DiscardArea>()) discardArea.AddComponent<DiscardArea>();
        if (!handworkButton.GetComponent<Button>()) handworkButton.gameObject.AddComponent<Button>();
        handworkButton.GetComponent<Button>().onClick.AddListener(delegate
        {
            MakingManager.Instance.CanMake(handworkButton);
            MakingManager.Instance.OpenWindow();
        });
    }
}