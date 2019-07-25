﻿using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;

public class AmountManager : SingletonMonoBehaviour<AmountManager>
{
    [SerializeField]
    private AmountUI UI;

    [SerializeField]
    private Vector2 defaultOffset = new Vector2(-100, 100);

    public long Amount { get; private set; }

    public bool IsUIOpen
    {
        get
        {
            if (!UI || !UI.gameObject) return false;
            else if (UI.amountWindow.alpha > 0) return true;
            else return false;
        }
    }

    private readonly UnityEvent onConfirm = new UnityEvent();

    private long min;
    private long max;

    public void NewAmount(UnityAction confirmAction, long max, long min = 0)
    {
        if (max < min)
        {
            max = max + min;
            min = max - min;
            max = max - min;
        }
        this.max = max;
        this.min = min;
        Amount = max >= 0 ? 0 : min;
        UI.amount.text = Amount.ToString();
        onConfirm.RemoveAllListeners();
        if (confirmAction != null) onConfirm.AddListener(confirmAction);
        UI.windowCanvas.sortingOrder = WindowsManager.Instance.TopOrder + 1;
        UI.amountWindow.alpha = 1;
        UI.amountWindow.blocksRaycasts = true;
    }

    public void Number(int num)
    {
        long.TryParse(UI.amount.text, out long current);
        if (UI.amount.text.Length < UI.amount.characterLimit - 1)
        {
            current = current * 10 + num;
            if (current < min) current = min;
            else if (current > max) current = max;
        }
        else current = Amount;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Plus()
    {
        long.TryParse(UI.amount.text, out long current);
        if (current < max && UI.amount.text.Length < UI.amount.characterLimit - 1)
            current++;
        else current = max;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Minus()
    {
        long.TryParse(UI.amount.text, out long current);
        if (current > min && UI.amount.text.Length < UI.amount.characterLimit - 1)
            current--;
        else current = min;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Max()
    {
        Amount = max;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Clear()
    {
        Amount = min;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Confirm()
    {
        UI.amountWindow.alpha = 0;
        UI.amountWindow.blocksRaycasts = false;
        onConfirm?.Invoke();
    }

    public void Cancel()
    {
        UI.amountWindow.alpha = 0;
        UI.amountWindow.blocksRaycasts = false;
    }

    public void SetPosition(Vector2 target, Vector2 offset)
    {
        UI.amountWindow.GetComponent<RectTransform>().position = target + offset;
    }

    public void SetPosition(Vector2 target)
    {
        UI.amountWindow.GetComponent<RectTransform>().position = target + defaultOffset;
    }

    public void FixAmount()
    {
        UI.amount.text = System.Text.RegularExpressions.Regex.Replace(UI.amount.text, @"[^0-9]+", "");
        long.TryParse(UI.amount.text, out long current);
        if (!(current <= max && UI.amount.text.Length < UI.amount.characterLimit - 1))
            current = max;
        else if (!(current >= min && UI.amount.text.Length < UI.amount.characterLimit - 1))
            current = min;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
        if (Amount < 1) UI.confirm.interactable = false;
        else UI.confirm.interactable = true;
    }
}