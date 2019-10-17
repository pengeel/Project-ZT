﻿using UnityEngine;

public class MapIconHolder : MonoBehaviour
{
    [Tooltip("游戏运行时修改无效。")]
    public Sprite icon;

    [Tooltip("游戏运行时修改无效。")]
    public Vector2 iconSize = new Vector2(48, 48);

    public bool drawOnWorldMap = true;

    public bool keepOnMap = true;

    [SerializeField, Tooltip("小于 0 时表示显示状态不受距离影响。")]
    private float maxValidDistance = -1;

    [HideInInspector]
    public float distanceSqr;

    public bool forceHided;

    public MapIconType iconType;

    public MapIcon iconInstance;

    public bool AutoHide => maxValidDistance > 0;

    private void Awake()
    {
        distanceSqr = maxValidDistance * maxValidDistance;
    }

    void Start()
    {
        if (MapManager.Instance) MapManager.Instance.CreateMapIcon(this);
    }

    //以下四个方法用于在游戏时动态修改图标信息
    public void SetIconImage(Sprite icon)
    {
        if (iconInstance) iconInstance.iconImage.overrideSprite = icon;
    }
    public void SetIconSize(Vector2 size)
    {
        if (iconInstance) iconInstance.iconImage.rectTransform.sizeDelta = size;
    }
    public void SetIconType(MapIconType iconType)
    {
        if (iconInstance) iconInstance.iconType = iconType;
    }
    public void SetIconValidDistance(float distance)
    {
        maxValidDistance = distance;
        distanceSqr = maxValidDistance * maxValidDistance;
    }

    public void ShowIcon()
    {
        if (forceHided) return;
        if (iconInstance && iconInstance.iconImage) iconInstance.iconImage.enabled = true;
    }
    public void HideIcon()
    {
        if (iconInstance && iconInstance.iconImage) iconInstance.iconImage.enabled = false;
    }

    private void OnDestroy()
    {
        if (MapManager.Instance) MapManager.Instance.RemoveMapIcon(this);
    }
}
