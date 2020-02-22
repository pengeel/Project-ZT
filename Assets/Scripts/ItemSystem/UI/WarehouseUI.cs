﻿using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WarehouseUI : WindowUI
{
    public Dropdown pageSelector;

    public GameObject itemCellPrefab;
    public Transform itemCellsParent;

    public Text money;
    public Text size;

    public Button sortButton;

    public ScrollRect gridRect;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(WarehouseManager.Instance.CloseWindow);
        sortButton.onClick.AddListener(WarehouseManager.Instance.Sort);
        pageSelector.onValueChanged.AddListener(WarehouseManager.Instance.SetPage);
    }
}
