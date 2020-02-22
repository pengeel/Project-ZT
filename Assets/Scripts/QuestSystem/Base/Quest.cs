﻿using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "quest", menuName = "ZetanStudio/任务/任务", order = 1)]
public class Quest : ScriptableObject
{
    [SerializeField]
    private string _ID;
    public string ID => _ID;

    [SerializeField, TextArea(2, 3)]
    private string title = string.Empty;
    public string Title => title;

    [SerializeField, TextArea(5, 5)]
    private string description;
    public string Description => description;

    [SerializeField]
    private bool abandonable = true;
    public bool Abandonable => abandonable;

    [SerializeField]
    private QuestGroup group;
    public QuestGroup Group => group;

    [SerializeField]
    private List<QuestAcceptCondition> acceptConditions = new List<QuestAcceptCondition>();
    public List<QuestAcceptCondition> AcceptConditions => acceptConditions;

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("普通", "主线", "反复")]
#endif
    private QuestType questType;
    public QuestType QuestType => questType;

    [SerializeField]
    private int repeatFrequancy = 1;
    public int RepeatFrequancy => repeatFrequancy;

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("分", "时", "天", "周", "月", "年")]
#endif
    private TimeUnit timeUnit = TimeUnit.Day;
    public TimeUnit TimeUnit => timeUnit;

    [SerializeField]
    private string conditionRelational;
    public string ConditionRelational => conditionRelational;

    [SerializeField]
    private Dialogue beginDialogue;
    public Dialogue BeginDialogue => beginDialogue;
    [SerializeField]
    private Dialogue ongoingDialogue;
    public Dialogue OngoingDialogue => ongoingDialogue;
    [SerializeField]
    private Dialogue completeDialogue;
    public Dialogue CompleteDialogue => completeDialogue;

    [SerializeField]
    private int rewardMoney;
    public int RewardMoney => rewardMoney;

    [SerializeField]
    private int rewardEXP;
    public int RewardEXP => rewardEXP;

    [SerializeField]
    private List<ItemInfo> rewardItems = new List<ItemInfo>();
    public List<ItemInfo> RewardItems => rewardItems;

    [SerializeField]
    private TalkerInformation _NPCToSubmit;
    public TalkerInformation NPCToSubmit => _NPCToSubmit;

    [SerializeField]
    private bool cmpltObjctvInOrder = false;
    public bool CmpltObjctvInOrder => cmpltObjctvInOrder;

    [System.NonSerialized]
    private List<Objective> objectiveInstances = new List<Objective>();//存储所有目标，在运行时用到，初始化时自动填，不用人为干预，详见QuestGiver类
    public List<Objective> ObjectiveInstances => objectiveInstances;

    [SerializeField]
    private List<CollectObjective> collectObjectives = new List<CollectObjective>();
    public List<CollectObjective> CollectObjectives => collectObjectives;

    [SerializeField]
    private List<KillObjective> killObjectives = new List<KillObjective>();
    public List<KillObjective> KillObjectives => killObjectives;

    [SerializeField]
    private List<TalkObjective> talkObjectives = new List<TalkObjective>();
    public List<TalkObjective> TalkObjectives => talkObjectives;

    [SerializeField]
    private List<MoveObjective> moveObjectives = new List<MoveObjective>();
    public List<MoveObjective> MoveObjectives => moveObjectives;

    [SerializeField]
    private List<SubmitObjective> submitObjectives = new List<SubmitObjective>();
    public List<SubmitObjective> SubmitObjectives => submitObjectives;

    [SerializeField]
    private List<CustomObjective> customObjectives = new List<CustomObjective>();
    public List<CustomObjective> CustomObjectives => customObjectives;

    [HideInInspector]
    public TalkerData originalQuestHolder;

    [HideInInspector]
    public TalkerData currentQuestHolder;

    public bool IsOngoing { get; set; }//任务是否正在执行，在运行时用到

    public bool IsComplete
    {
        get
        {
            if (ObjectiveInstances.Exists(x => !x.IsComplete))
                return false;
            return true;
        }
    }

    public bool IsValid
    {
        get
        {
            if (ObjectiveInstances.Count < 1) return false;
            if (string.IsNullOrEmpty(ID) || string.IsNullOrEmpty(Title)) return false;
            if (NPCToSubmit && !GameManager.TalkerDatas.ContainsKey(NPCToSubmit.ID)) return false;
            foreach (var co in CollectObjectives)
                if (!co.IsValid) return false;
            foreach (var ko in KillObjectives)
                if (!ko.IsValid) return false;
                else if (!GameManager.Enemies.ContainsKey(ko.Enemy.ID)) return false;
            foreach (var to in TalkObjectives)
                if (!to.IsValid) return false;
                else if (!GameManager.TalkerDatas.ContainsKey(to.NPCToTalk.ID)) return false;
            foreach (var mo in MoveObjectives)
                if (!mo.IsValid) return false;
                else if (!GameManager.QuestPoints.ContainsKey(mo.PointID)) return false;
            foreach (var so in SubmitObjectives)
                if (!so.IsValid) return false;
                else if (!GameManager.TalkerDatas.ContainsKey(so.NPCToSubmit.ID)) return false;
            foreach (var cuo in CustomObjectives)
                if (!cuo.IsValid) return false;
            return true;
        }
    }

    public bool IsFinished
    {
        get
        {
            return IsComplete && !IsOngoing;
        }
    }

    public bool AcceptAble
    {
        get
        {
            bool calFailed = false;
            if (string.IsNullOrEmpty(conditionRelational)) return AcceptConditions.TrueForAll(x => x.IsEligible);
            if (AcceptConditions.Count < 1) calFailed = true;
            else
            {
                Debug.Log(Title);
                var cr = conditionRelational.Replace(" ", "").ToCharArray();//删除所有空格才开始计算
                List<string> RPN = new List<string>();//逆波兰表达式
                string indexStr = string.Empty;//数字串
                Stack<char> optStack = new Stack<char>();//运算符栈
                for (int i = 0; i < cr.Length; i++)
                {
                    char c = cr[i];
                    string item;
                    if (c < '0' || c > '9')
                    {
                        if (!string.IsNullOrEmpty(indexStr))
                        {
                            item = indexStr;
                            indexStr = string.Empty;
                            GetRPNItem(item);
                        }
                        if (c == '(' || c == ')' || c == '+' || c == '*' || c == '~')
                        {
                            item = c + "";
                            GetRPNItem(item);
                        }
                        else
                        {
                            calFailed = true;
                            break;
                        }//既不是数字也不是运算符，直接放弃计算
                    }
                    else
                    {
                        indexStr += c;//拼接数字
                        if (i + 1 >= cr.Length)
                        {
                            item = indexStr;
                            indexStr = string.Empty;
                            GetRPNItem(item);
                        }
                    }
                }
                while (optStack.Count > 0)
                    RPN.Add(optStack.Pop() + "");
                Stack<bool> values = new Stack<bool>();
                foreach (var item in RPN)
                {
                    Debug.Log(item);
                    if (int.TryParse(item, out int index))
                    {
                        if (index >= 0 && index < AcceptConditions.Count)
                            values.Push(AcceptConditions[index].IsEligible);
                        else
                        {
                            //Debug.Log("return 1");
                            return true;
                        }
                    }
                    else if (values.Count > 1)
                    {
                        if (item == "+") values.Push(values.Pop() | values.Pop());
                        else if (item == "~") values.Push(!values.Pop());
                        else if (item == "*") values.Push(values.Pop() & values.Pop());
                    }
                    else if (item == "~") values.Push(!values.Pop());
                }
                if (values.Count == 1)
                {
                    //Debug.Log("return 2");
                    return values.Pop();
                }

                void GetRPNItem(string item)
                {
                    //Debug.Log(item);
                    if (item == "+" || item == "*" || item == "~")//遇到运算符
                    {
                        char opt = item[0];
                        if (optStack.Count < 1) optStack.Push(opt);//栈空则直接入栈
                        else while (optStack.Count > 0)//栈不空则出栈所有优先级大于或等于opt的运算符后才入栈opt
                            {
                                char top = optStack.Peek();
                                if (top + "" == item || top == '~' || top == '*' && opt == '+')
                                {
                                    RPN.Add(optStack.Pop() + "");
                                    if (optStack.Count < 1)
                                    {
                                        optStack.Push(opt);
                                        break;
                                    }
                                }
                                else
                                {
                                    optStack.Push(opt);
                                    break;
                                }
                            }
                    }
                    else if (item == "(") optStack.Push('(');
                    else if (item == ")")
                    {
                        while (optStack.Count > 0)
                        {
                            char opt = optStack.Pop();
                            if (opt == '(') break;
                            else RPN.Add(opt + "");
                        }
                    }
                    else if (int.TryParse(item, out _)) RPN.Add(item);//遇到数字
                }
            }
            if (!calFailed)
            {
                //Debug.Log("return 3");
                return true;
            }
            else
            {
                foreach (QuestAcceptCondition qac in AcceptConditions)
                    if (!qac.IsEligible)
                    {
                        //Debug.Log("return 4");
                        return false;
                    }
                //Debug.Log("return 5");
                return true;
            }
        }
    }

    /// <summary>
    /// 判断该任务是否需要某个道具，用于丢弃某个道具时，判断能不能丢
    /// </summary>
    /// <param name="item">所需判定的道具</param>
    /// <param name="leftAmount">所需判定的数量</param>
    /// <returns>是否需要</returns>
    public bool RequiredItem(ItemBase item, int leftAmount)
    {
        if (CmpltObjctvInOrder)
        {
            foreach (Objective o in ObjectiveInstances)
            {
                //当目标是收集类目标且在提交任务同时会失去相应道具时，才进行判断
                if (o is CollectObjective && item == (o as CollectObjective).Item && (o as CollectObjective).LoseItemAtSbmt)
                {
                    if (o.IsComplete && o.InOrder)
                    {
                        //如果剩余的道具数量不足以维持该目标完成状态
                        if (o.Amount > leftAmount)
                        {
                            Objective tempObj = o.NextObjective;
                            while (tempObj != null)
                            {
                                //则判断是否有后置目标在进行，以保证在打破该目标的完成状态时，后置目标不受影响
                                if (tempObj.CurrentAmount > 0 && tempObj.OrderIndex > o.OrderIndex)
                                {
                                    //Debug.Log("Required");
                                    return true;
                                }
                                tempObj = tempObj.NextObjective;
                            }
                        }
                        //Debug.Log("NotRequired3");
                        return false;
                    }
                    //Debug.Log("NotRequired2");
                    return false;
                }
            }
        }
        //Debug.Log("NotRequired1");
        return false;
    }

    /// <summary>
    /// 是否在收集某个道具
    /// </summary>
    /// <param name="itemID">许判断的道具</param>
    /// <returns>是否在收集</returns>
    public bool CollectingItem(ItemBase item)
    {
        return collectObjectives.Exists(x => x.Item == item && !x.IsComplete);
    }
}

public enum QuestType
{
    Normal,
    Main,
    Repeated,
}

#region 任务条件
/// <summary>
/// 任务接取条件
/// </summary>
[System.Serializable]
public class QuestAcceptCondition
{
    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("等级等于", "等级大于", "等级小于", "完成任务", "拥有道具", "触发器开启", "触发器关闭")]
#endif
    private QuestCondition acceptCondition = QuestCondition.CompleteQuest;
    public QuestCondition AcceptCondition => acceptCondition;

    [SerializeField]
    private int level = 1;
    public int Level => level;

    [SerializeField]
    private Quest completeQuest;
    public Quest CompleteQuest => completeQuest;

    [SerializeField]
    private ItemBase ownedItem;
    public ItemBase OwnedItem => ownedItem;

    [SerializeField]
    private string triggerName;
    public string TriggerName => triggerName;

    /// <summary>
    /// 是否符合条件
    /// </summary>
    public bool IsEligible
    {
        get
        {
            switch (AcceptCondition)
            {
                case QuestCondition.CompleteQuest: return QuestManager.Instance.HasCompleteQuestWithID(CompleteQuest.ID);
                case QuestCondition.HasItem: return BackpackManager.Instance.HasItemWithID(OwnedItem.ID);
                case QuestCondition.LevelEquals: return PlayerManager.Instance.PlayerInfo.level == level;
                case QuestCondition.LevelLargeThen: return PlayerManager.Instance.PlayerInfo.level > level;
                //case QuestCondition.LevelLargeOrEqualsThen: return PlayerManager.Instance.PlayerInfo.level >= level;
                case QuestCondition.LevelLessThen: return PlayerManager.Instance.PlayerInfo.level < level;
                //case QuestCondition.LevelLessOrEqualsThen: return PlayerManager.Instance.PlayerInfo.level <= level;
                case QuestCondition.TriggerSet:
                    var state = TriggerManager.Instance.GetTriggerState(triggerName);
                    return state != TriggerState.NotExist ? (state == TriggerState.On ? true : false) : false;
                case QuestCondition.TriggerReset:
                    state = TriggerManager.Instance.GetTriggerState(triggerName);
                    return state != TriggerState.NotExist ? (state == TriggerState.Off ? true : false) : false;
                default: return true;
            }
        }
    }
}

public enum QuestCondition
{
    LevelEquals,
    LevelLargeThen,
    LevelLessThen,
    //LevelLargeOrEqualsThen,
    //LevelLessOrEqualsThen,
    CompleteQuest,
    HasItem,
    TriggerSet,
    TriggerReset
}
#endregion

#region 任务目标
public delegate void ObjectiveStateListner(Objective objective, bool cmpltStateBef);
/// <summary>
/// 任务目标
/// </summary>
public abstract class Objective
{
    [SerializeField]
    private string displayName = string.Empty;
    public string DisplayName => displayName;

    [SerializeField]
    private bool display = true;
    public bool Display
    {
        get
        {
            if (runtimeParent && !runtimeParent.CmpltObjctvInOrder)
                return true;
            return display;
        }
    }

    [SerializeField]
    private bool showMapIcon = true;
    public bool ShowMapIcon => this is CollectObjective || this is CustomObjective ? false : showMapIcon;

    [SerializeField]
    private int amount = 1;
    public int Amount => amount;

    private int currentAmount;
    public int CurrentAmount
    {
        get
        {
            return currentAmount;
        }
        set
        {
            bool befCmplt = IsComplete;
            if (value < amount && value >= 0)
                currentAmount = value;
            else if (value < 0)
            {
                currentAmount = 0;
            }
            else currentAmount = amount;
            if (!befCmplt && IsComplete)
                UpdateNextCollectObjectives();
            OnStateChangeEvent?.Invoke(this, befCmplt);
        }
    }

    [SerializeField]
    private bool inOrder;
    public bool InOrder => inOrder;

    [SerializeField]
    private int orderIndex = 1;
    public int OrderIndex => orderIndex;

    public bool IsComplete
    {
        get
        {
            if (currentAmount >= amount)
                return true;
            return false;
        }
    }

    public bool IsValid
    {
        get
        {
            if (Amount < 0) return false;
            if (this is CollectObjective && !(this as CollectObjective).Item)
                return false;
            if (this is KillObjective)
            {
                var ko = this as KillObjective;
                if (ko.ObjectiveType == KillObjectiveType.Specific && !ko.Enemy)
                    return false;
                else if (ko.ObjectiveType == KillObjectiveType.Race && !ko.Race)
                    return false;
            }
            if (this is TalkObjective && (!(this as TalkObjective).NPCToTalk || !(this as TalkObjective).Dialogue))
                return false;
            if (this is MoveObjective && string.IsNullOrEmpty((this as MoveObjective).PointID))
                return false;
            if (this is SubmitObjective)
            {
                var so = this as SubmitObjective;
                if (!so.NPCToSubmit || !so.ItemToSubmit || string.IsNullOrEmpty(so.WordsWhenSubmit))
                    return false;
            }
            if (this is CustomObjective && string.IsNullOrEmpty((this as CustomObjective).TriggerName))
                return false;
            return true;
        }
    }

    [System.NonSerialized]
    public Objective PrevObjective;
    [System.NonSerialized]
    public Objective NextObjective;

    [HideInInspector]
    public string runtimeID;

    [HideInInspector]
    public Quest runtimeParent;

    [HideInInspector]
    public ObjectiveStateListner OnStateChangeEvent;

    protected virtual void UpdateAmountUp(int amount = 1)
    {
        if (IsComplete) return;
        if (!InOrder) CurrentAmount += amount;
        else if (AllPrevObjCmplt) CurrentAmount += amount;
        if (CurrentAmount > 0)
        {
            string message = DisplayName + (IsComplete ? "(完成)" : "[" + currentAmount + "/" + Amount + "]");
            MessageManager.Instance.NewMessage(message);
        }
        if (runtimeParent.IsComplete)
            MessageManager.Instance.NewMessage("[任务]" + runtimeParent.Title + "(已完成)");
    }

    public bool AllPrevObjCmplt//判定所有前置目标是否都完成
    {
        get
        {
            Objective tempObj = PrevObjective;
            while (tempObj != null)
            {
                if (!tempObj.IsComplete && tempObj.OrderIndex < OrderIndex)
                {
                    return false;
                }
                tempObj = tempObj.PrevObjective;
            }
            return true;
        }
    }
    public bool HasNextObjOngoing//判定是否有后置目标正在进行
    {
        get
        {
            Objective tempObj = NextObjective;
            while (tempObj != null)
            {
                if (tempObj.CurrentAmount > 0 && tempObj.OrderIndex > OrderIndex)
                {
                    return true;
                }
                tempObj = tempObj.NextObjective;
            }
            return false;
        }
    }

    /// <summary>
    /// 可并行？
    /// </summary>
    public bool Parallel
    {
        get
        {
            if (!InOrder) return true;//不按顺序，说明可以并行执行
            if (PrevObjective && PrevObjective.OrderIndex == OrderIndex) return true;//有前置目标，而且顺序码与前置目标相同，说明可以并行执行
            if (NextObjective && NextObjective.OrderIndex == OrderIndex) return true;//有后置目标，而且顺序码与后置目标相同，说明可以并行执行
            return false;
        }
    }

    /// <summary>
    /// 更新某个收集类任务目标，用于在其他前置目标完成时，更新后置收集类目标
    /// </summary>
    void UpdateNextCollectObjectives()
    {
        Objective tempObj = NextObjective;
        CollectObjective co;
        while (tempObj != null)
        {
            if (!(tempObj is CollectObjective) && tempObj.InOrder && tempObj.NextObjective != null && tempObj.NextObjective.InOrder && tempObj.OrderIndex < tempObj.NextObjective.OrderIndex)
            {
                //若相邻后置目标不是收集类目标，该后置目标按顺序执行，其相邻后置也按顺序执行，且两者不可同时执行，则说明无法继续更新后置的收集类目标
                return;
            }
            if (tempObj is CollectObjective)
            {
                co = tempObj as CollectObjective;
                if (co.CheckBagAtStart) co.CurrentAmount = BackpackManager.Instance.GetItemAmount(co.Item.ID);
            }
            tempObj = tempObj.NextObjective;
        }
    }

    public static implicit operator bool(Objective self)
    {
        return self != null;
    }
}
/// <summary>
/// 收集类目标
/// </summary>
[System.Serializable]
public class CollectObjective : Objective
{
    [SerializeField]
    private ItemBase item;
    public ItemBase Item
    {
        get
        {
            return item;
        }
    }

    [SerializeField]
    private bool checkBagAtStart = true;//用于标识是否在目标开始执行时检查背包道具看是否满足目标，否则目标重头开始计数
    public bool CheckBagAtStart
    {
        get
        {
            return checkBagAtStart;
        }
    }
    [SerializeField]
    private bool loseItemAtSbmt = true;//用于标识是否在提交任务时失去相应道具
    public bool LoseItemAtSbmt
    {
        get
        {
            return loseItemAtSbmt;
        }
    }

    public void UpdateCollectAmount(ItemBase item, int leftAmount)//得道具时用到
    {
        if (item == Item)
        {
            if (IsComplete) return;
            if (!InOrder) CurrentAmount = leftAmount;
            else if (AllPrevObjCmplt) CurrentAmount = leftAmount;
            if (CurrentAmount > 0)
            {
                string message = DisplayName + (IsComplete ? "(完成)" : "[" + CurrentAmount + "/" + Amount + "]");
                MessageManager.Instance.NewMessage(message);
            }
            if (runtimeParent.IsComplete)
                MessageManager.Instance.NewMessage("[任务]" + runtimeParent.Title + "(已完成)");
        }
    }

    public void UpdateCollectAmountDown(ItemBase item, int leftAmount)//丢道具时用到
    {
        if (item == Item && AllPrevObjCmplt && !HasNextObjOngoing && LoseItemAtSbmt)
            //前置目标都完成且没有后置目标在进行时，才允许更新；在提交任务时不需要提交相应道具，也不会更新减少值。
            CurrentAmount = leftAmount;
    }
}
/// <summary>
/// 打怪类目标
/// </summary>
[System.Serializable]
public class KillObjective : Objective
{
    //[SerializeField]
    //private string enemyID;
    //public string EnemyID
    //{
    //    get
    //    {
    //        return enemyID;
    //    }
    //}

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("特定敌人", "特定种群", "任意敌人")]
#endif
    private KillObjectiveType objectiveType;
    public KillObjectiveType ObjectiveType
    {
        get
        {
            return objectiveType;
        }
    }

    [SerializeField]
    private EnemyInformation enemy;
    public EnemyInformation Enemy
    {
        get
        {
            return enemy;
        }
    }

    [SerializeField]
    private EnemyRace race;
    public EnemyRace Race
    {
        get
        {
            return race;
        }
    }

    public void UpdateKillAmount()
    {
        UpdateAmountUp();
    }
}
public enum KillObjectiveType
{
    /// <summary>
    /// 特定敌人
    /// </summary>
    Specific,

    /// <summary>
    /// 特定种族
    /// </summary>
    Race,

    /// <summary>
    /// 任意
    /// </summary>
    Any
}
/// <summary>
/// 谈话类目标
/// </summary>
[System.Serializable]
public class TalkObjective : Objective
{
    [SerializeField]
    private TalkerInformation _NPCToTalk;
    public TalkerInformation NPCToTalk
    {
        get
        {
            return _NPCToTalk;
        }
    }

    [SerializeField]
    private Dialogue dialogue;
    public Dialogue Dialogue
    {
        get
        {
            return dialogue;
        }
    }

    public void UpdateTalkState()
    {
        UpdateAmountUp();
    }
}
/// <summary>
/// 移动到点类目标
/// </summary>
[System.Serializable]
public class MoveObjective : Objective
{
    [SerializeField]
    private string pointID = string.Empty;
    public string PointID
    {
        get
        {
            return pointID;
        }
    }

    public void UpdateMoveState(QuestPoint point)
    {
        if (point.ID == PointID) UpdateAmountUp();
    }
}
/// <summary>
/// 提交类目标
/// </summary>
[System.Serializable]
public class SubmitObjective : Objective
{
    [SerializeField]
    private TalkerInformation _NPCToSubmit;
    public TalkerInformation NPCToSubmit
    {
        get
        {
            return _NPCToSubmit;
        }
    }

    [SerializeField]
    private ItemBase itemToSubmit;
    public ItemBase ItemToSubmit
    {
        get
        {
            return itemToSubmit;
        }
    }

    [SerializeField]
    private string wordsWhenSubmit;
    public string WordsWhenSubmit
    {
        get
        {
            return wordsWhenSubmit;
        }
    }

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("提交处的NPC", "玩家")]
#endif
    private TalkerType talkerType;
    public TalkerType TalkerType
    {
        get
        {
            return talkerType;
        }
    }

    public void UpdateSubmitState(int amount = 1)
    {
        UpdateAmountUp(amount);
    }
}
/// <summary>
/// 自定义目标
/// </summary>
[System.Serializable]
public class CustomObjective : Objective
{
    [SerializeField]
    private string triggerName;
    public string TriggerName
    {
        get
        {
            return triggerName;
        }
    }

    [SerializeField]
    private bool checkStateAtAcpt = true;//用于标识是否在接取任务时检触发器状态看是否满足目标，否则目标重头开始等待触发
    public bool CheckStateAtAcpt
    {
        get
        {
            return checkStateAtAcpt;
        }
    }

    public void UpdateTriggerState(string name, bool state)
    {
        if (name != TriggerName) return;
        if (state) UpdateAmountUp();
        else if (AllPrevObjCmplt && !HasNextObjOngoing) CurrentAmount--;
    }
}
#endregion