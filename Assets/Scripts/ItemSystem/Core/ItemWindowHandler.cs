﻿using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ItemWindowHandler : MonoBehaviour
{
    private static ItemWindowHandler instance;
    public static ItemWindowHandler Instance
    {
        get
        {
            if (!instance || !instance.gameObject)
                instance = FindObjectOfType<ItemWindowHandler>();
            return instance;
        }
    }

    [SerializeField]
    private ItemWindowUI UI;

    [SerializeField]
    private ItemWindowBaseUI subUI;

    private bool IsPause;

    public bool IsHeld { get; private set; }

    public ItemInfo MItemInfo { get; private set; } = null;

    public ItemAgentType ItemAgentType { get; private set; } = ItemAgentType.None;

    public void OpenItemWindow(ItemAgent itemAgent)
    {
        //UI.windowsRect.position = new Vector3(Input.mousePosition.x - UI.windowsRect.sizeDelta.x, UI.windowsRect.position.y);
        if (itemAgent.MItemInfo == null || !itemAgent.MItemInfo.Item || IsPause) return;
        MItemInfo = itemAgent.MItemInfo;
        ItemAgentType = itemAgent.agentType;
        UI.icon.overrideSprite = MItemInfo.Item.Icon;
        UI.nameText.text = MItemInfo.ItemName;
        UI.nameText.color = itemAgent.currentQualityColor;
        UI.typeText.text = GetItemTypeString(MItemInfo.Item.ItemType);
        UI.priceText.text = MItemInfo.Item.SellAble ? MItemInfo.Item.SellPrice + GameManager.Instance.CoinName : "不可出售";
        UI.weightText.text = "重量：" + MItemInfo.Item.Weight.ToString("F2") + "WL";
        UI.descriptionText.text = MItemInfo.Item.Description;
        switch (MItemInfo.Item.ItemType)
        {
            case ItemType.Weapon:
                WeaponItem weapon = MItemInfo.Item as WeaponItem;
                UI.effectText.text = (weapon.ATK > 0 ? "攻击力+" + weapon.ATK + "\n" : string.Empty) +
                    (weapon.DEF > 0 ? "防御力力+" + weapon.DEF + "\n" : string.Empty) +
                    (weapon.Hit > 0 ? "命中+" + weapon.Hit + "\n" : string.Empty);
                if (weapon.Powerup.IsEffective)
                {
                    MyTools.SetActive(UI.mulFunTitle.gameObject, true);
                    MyTools.SetActive(UI.mulFunText.gameObject, true);
                    UI.mulFunTitle.text = "-附加能力";
                    UI.mulFunText.text = weapon.Powerup.ToString();
                }
                else
                {
                    MyTools.SetActive(UI.mulFunTitle.gameObject, false);
                    MyTools.SetActive(UI.mulFunText.gameObject, false);
                    UI.mulFunTitle.text = string.Empty;
                    UI.mulFunText.text = string.Empty;
                }
                if (weapon.GemSlotAmout > 0)
                    MyTools.SetActive(UI.gemstone_1.gameObject, true);
                else
                    MyTools.SetActive(UI.gemstone_1.gameObject, false);
                if (weapon.GemSlotAmout > 1)
                    MyTools.SetActive(UI.gemstone_2.gameObject, true);
                else
                    MyTools.SetActive(UI.gemstone_2.gameObject, false);
                MyTools.SetActive(UI.durability.gameObject, true);
                if (PlayerManager.Instance.PlayerInfo.HasPrimaryWeapon)
                    OpenSubItemWindow(PlayerManager.Instance.PlayerInfo.primaryWeapon);
                else if (PlayerManager.Instance.PlayerInfo.HasSecondaryWeapon)
                    OpenSubItemWindow(PlayerManager.Instance.PlayerInfo.secondaryWeapon);
                break;
            case ItemType.Bag:
                UI.effectText.text = GameManager.Instance.BackpackName + "容量+" + (MItemInfo.Item as BagItem).ExpandSize;
                MyTools.SetActive(UI.mulFunTitle.gameObject, false);
                MyTools.SetActive(UI.mulFunText.gameObject, false);
                MyTools.SetActive(UI.gemstone_1.gameObject, false);
                MyTools.SetActive(UI.gemstone_2.gameObject, false);
                MyTools.SetActive(UI.durability.gameObject, false);
                break;
            default:
                UI.effectText.text = string.Empty;
                MyTools.SetActive(UI.mulFunTitle.gameObject, false);
                MyTools.SetActive(UI.mulFunText.gameObject, false);
                MyTools.SetActive(UI.gemstone_1.gameObject, false);
                MyTools.SetActive(UI.gemstone_2.gameObject, false);
                MyTools.SetActive(UI.durability.gameObject, false);
                break;
        }
        UI.itemWindow.alpha = 1;
        UI.itemWindow.blocksRaycasts = false;
#if UNITY_ANDROID
        UI.itemWindow.blocksRaycasts = true;
        UI.buttonAreaCanvas.alpha = 1;
        UI.buttonAreaCanvas.blocksRaycasts = true;
        UI.mulFunButton.onClick.RemoveAllListeners();
        MyTools.SetActive(UI.closeButton.gameObject, true);
#endif
        switch (itemAgent.agentType)
        {
            case ItemAgentType.Backpack:
                UI.priceTitle.text = "贩卖价格";
#if UNITY_ANDROID
                MyTools.SetActive(UI.buttonsArea, true);
                MyTools.SetActive(UI.discardButton.gameObject, MItemInfo.Item.DiscardAble);
                MyTools.SetActive(UI.mulFunButton.gameObject, false);
                UI.mulFunButton.onClick.RemoveAllListeners();
                if (!WarehouseManager.Instance.IsUIOpen && !ShopManager.Instance.IsUIOpen)
                {
                    if (MItemInfo.Item.Useable)
                    {
                        MyTools.SetActive(UI.mulFunButton.gameObject, true);
                        UI.mulFunButton.GetComponentInChildren<Text>().text = MItemInfo.Item.IsEquipment ? "装备" : "使用";
                        UI.mulFunButton.onClick.AddListener(UseCurrenItem);
                    }
                }
                else if (WarehouseManager.Instance.IsUIOpen)
                {
                    MyTools.SetActive(UI.mulFunButton.gameObject, true);
                    UI.mulFunButton.GetComponentInChildren<Text>().text = "存入";
                    UI.mulFunButton.onClick.AddListener(StoreCurrentItem);
                }
                else if (ShopManager.Instance.IsUIOpen)
                {
                    if (MItemInfo.Item.SellAble)
                    {
                        MyTools.SetActive(UI.mulFunButton.gameObject, true);
                        UI.mulFunButton.GetComponentInChildren<Text>().text = "出售";
                        UI.mulFunButton.onClick.AddListener(SellOrPurchaseCurrentItem);
                    }
                    MyTools.SetActive(UI.discardButton.gameObject, false);
                }
#endif
                break;
            case ItemAgentType.Warehouse:
                UI.priceTitle.text = "贩卖价格";
#if UNITY_ANDROID
                MyTools.SetActive(UI.buttonsArea, true);
                MyTools.SetActive(UI.discardButton.gameObject, false);
                MyTools.SetActive(UI.mulFunButton.gameObject, true);
                UI.mulFunButton.onClick.RemoveAllListeners();
                UI.mulFunButton.onClick.AddListener(TakeOutCurrentItem);
                UI.mulFunButton.GetComponentInChildren<Text>().text = "取出";
#endif
                break;
            case ItemAgentType.Making:
                UI.priceTitle.text = "贩卖价格";
#if UNITY_ANDROID
                MyTools.SetActive(UI.buttonsArea, true);
                MyTools.SetActive(UI.discardButton.gameObject, false);
                MyTools.SetActive(UI.mulFunButton.gameObject, MItemInfo.Amount > 0);
                UI.mulFunButton.onClick.RemoveAllListeners();
                UI.mulFunButton.onClick.AddListener(MakeCurrentItem);
                UI.mulFunButton.GetComponentInChildren<Text>().text = "制作";
#endif
                break;
            case ItemAgentType.ShopSelling:
                UI.priceTitle.text = "售价";
                if (ShopManager.Instance.GetMerchandiseAgentByItem(MItemInfo))
                    UI.priceText.text = ShopManager.Instance.GetMerchandiseAgentByItem(MItemInfo).merchandiseInfo.SellPrice.ToString() + GameManager.Instance.CoinName;
                else CloseItemWindow();
#if UNITY_ANDROID
                MyTools.SetActive(UI.buttonsArea, true);
                MyTools.SetActive(UI.discardButton.gameObject, false);
                MyTools.SetActive(UI.mulFunButton.gameObject, true);
                UI.mulFunButton.onClick.RemoveAllListeners();
                UI.mulFunButton.onClick.AddListener(SellOrPurchaseCurrentItem);
                UI.mulFunButton.GetComponentInChildren<Text>().text = "购买";
#endif
                break;
            case ItemAgentType.ShopBuying:
                UI.priceTitle.text = "收购价";
                if (ShopManager.Instance.GetMerchandiseAgentByItem(MItemInfo))
                    UI.priceText.text = ShopManager.Instance.GetMerchandiseAgentByItem(MItemInfo).merchandiseInfo.PurchasePrice.ToString() + GameManager.Instance.CoinName;
                else CloseItemWindow();
#if UNITY_ANDROID
                MyTools.SetActive(UI.buttonsArea, true);
                MyTools.SetActive(UI.discardButton.gameObject, false);
                MyTools.SetActive(UI.mulFunButton.gameObject, true);
                UI.mulFunButton.onClick.RemoveAllListeners();
                UI.mulFunButton.onClick.AddListener(SellOrPurchaseCurrentItem);
                UI.mulFunButton.GetComponentInChildren<Text>().text = "出售";
#endif
                break;
            default:
#if UNITY_ANDROID
                MyTools.SetActive(UI.closeButton.gameObject, true);
#endif
                MyTools.SetActive(UI.discardButton.gameObject, false);
                MyTools.SetActive(UI.mulFunButton.gameObject, false);
                UI.mulFunButton.onClick.RemoveAllListeners();
                break;
        }
        UI.windowCanvas.sortingOrder = WindowsManager.Instance.TopOrder + 1;
    }

    public void OpenSubItemWindow(ItemInfo equiped)
    {
        if (equiped == null || !equiped.Item || IsPause) return;
        subUI.icon.overrideSprite = equiped.Item.Icon;
        subUI.nameText.text = equiped.ItemName;
        if (GameManager.Instance.QualityColors.Count >= 5)
            subUI.nameText.color = GameManager.Instance.QualityColors[(int)equiped.Item.Quality];
        subUI.typeText.text = GetItemTypeString(equiped.Item.ItemType);
        subUI.priceTitle.text = "贩卖价格";
        subUI.priceText.text = equiped.Item.SellAble ? equiped.Item.SellPrice + GameManager.Instance.CoinName : "不可出售";
        subUI.weightText.text = "重量：" + equiped.Item.Weight.ToString("F2") + "WL";
        subUI.descriptionText.text = equiped.Item.Description;
        switch (equiped.Item.ItemType)
        {
            case ItemType.Weapon:
                WeaponItem weapon = equiped.Item as WeaponItem;
                subUI.effectText.text = (weapon.ATK > 0 ? "攻击力+" + weapon.ATK + "\n" : string.Empty) +
                    (weapon.DEF > 0 ? "防御力力+" + weapon.DEF + "\n" : string.Empty) +
                    (weapon.Hit > 0 ? "命中+" + weapon.Hit + "\n" : string.Empty);
                if (weapon.Powerup.IsEffective)
                {
                    MyTools.SetActive(subUI.mulFunTitle.gameObject, true);
                    MyTools.SetActive(subUI.mulFunText.gameObject, true);
                    subUI.mulFunTitle.text = "-附加能力";
                    subUI.mulFunText.text = weapon.Powerup.ToString();
                }
                else
                {
                    MyTools.SetActive(subUI.mulFunTitle.gameObject, false);
                    MyTools.SetActive(subUI.mulFunText.gameObject, false);
                    subUI.mulFunTitle.text = string.Empty;
                    subUI.mulFunText.text = string.Empty;
                }
                if (weapon.GemSlotAmout > 0)
                    MyTools.SetActive(subUI.gemstone_1.gameObject, true);
                else
                    MyTools.SetActive(subUI.gemstone_1.gameObject, false);
                if (weapon.GemSlotAmout > 1)
                    MyTools.SetActive(subUI.gemstone_2.gameObject, true);
                else
                    MyTools.SetActive(subUI.gemstone_2.gameObject, false);
                MyTools.SetActive(subUI.durability.gameObject, true);
                break;
            default:
                subUI.effectText.text = string.Empty;
                MyTools.SetActive(subUI.mulFunTitle.gameObject, false);
                MyTools.SetActive(subUI.mulFunText.gameObject, false);
                MyTools.SetActive(subUI.gemstone_1.gameObject, false);
                MyTools.SetActive(subUI.gemstone_2.gameObject, false);
                MyTools.SetActive(subUI.durability.gameObject, false);
                break;
        }
        subUI.itemWindow.alpha = 1;
        subUI.itemWindow.blocksRaycasts = false;
        subUI.windowCanvas.sortingOrder = WindowsManager.Instance.TopOrder + 1;
    }

    public static string GetItemTypeString(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
            case ItemType.Armor:
            case ItemType.Jewelry:
            case ItemType.Tool:
                return "装备";
            case ItemType.Quest:
            case ItemType.Valuables:
                return "特殊";
            case ItemType.Material:
                return "材料";
            case ItemType.Box:
            case ItemType.Bag:
            case ItemType.Medicine:
            case ItemType.Elixir:
            case ItemType.Cuisine:
                return "消耗品";
            default: return "普通";
        }
    }

    public void CloseItemWindow()
    {
        UI.itemWindow.alpha = 0;
        UI.itemWindow.blocksRaycasts = false;
        MItemInfo = null;
        UI.nameText.text = null;
        UI.descriptionText.text = string.Empty;
        UI.priceText.text = string.Empty;
        UI.descriptionText.text = string.Empty;
        MyTools.SetActive(UI.durability.gameObject, false);
        CloseSubWindow();
#if UNITY_ANDROID
        UI.buttonAreaCanvas.alpha = 0;
        UI.buttonAreaCanvas.blocksRaycasts = false;
        MyTools.SetActive(UI.closeButton.gameObject, false);
#endif
    }

    public void CloseSubWindow()
    {
        subUI.itemWindow.alpha = 0;
        subUI.itemWindow.blocksRaycasts = false;
        subUI.nameText.text = null;
        subUI.descriptionText.text = string.Empty;
        subUI.priceText.text = string.Empty;
        subUI.descriptionText.text = string.Empty;
        MyTools.SetActive(subUI.durability.gameObject, false);
    }

    public void UseCurrenItem()
    {
        BackpackManager.Instance.GetItemAgentByInfo(MItemInfo).OnUse();
        CloseItemWindow();
    }

    public void DiscardCurrentItem()
    {
        BackpackManager.Instance.DiscardItem(MItemInfo);
        AmountHandler.Instance.SetPosition(new Vector3(Screen.width / 2, Screen.height / 2), Vector3.zero);
        CloseItemWindow();
    }

    public void StoreCurrentItem()
    {
        WarehouseManager.Instance.StoreItem(MItemInfo);
        CloseItemWindow();
    }

    public void TakeOutCurrentItem()
    {
        WarehouseManager.Instance.TakeOutItem(MItemInfo);
        CloseItemWindow();
    }

    public void SellOrPurchaseCurrentItem()
    {
        MerchandiseAgent agent = ShopManager.Instance.GetMerchandiseAgentByItem(MItemInfo);
        if (agent) agent.OnSellOrPurchase();
        else ShopManager.Instance.PurchaseItem(MItemInfo);
        CloseItemWindow();
    }

    public void MakeCurrentItem()
    {
        MakingManager.Instance.MakeCurrent();
        CloseItemWindow();
    }

    public void PauseShowing(bool pause)
    {
        IsPause = pause;
        CloseItemWindow();
    }

    public void Hold()
    {
        IsHeld = true;
    }

    public void Dehold()
    {
        IsHeld = false;
    }

    public void SetUI(ItemWindowUI UI, ItemWindowBaseUI subUI)
    {
        this.UI = UI;
        this.subUI = subUI;
    }
}