﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("ZetanStudio/AI/A*寻路单位")]
public class AStarUnit : MonoBehaviour
{
    [SerializeField, Tooltip("以单元格倍数为单位")]
    private int unitSize = 1;

    [SerializeField]
    private Transform target;

    [SerializeField]
    private Vector3 targetFootOffset;

    public Vector3 TargetPosition
    {
        get
        {
            if (target)
                return target.position + targetFootOffset;
            return OffsetPosition;
        }
    }

    [SerializeField, Tooltip("目标离开原位置多远之后开始修正跟随？")]
    private float targetFollowStartDistance = 5;

    [SerializeField]
    private Vector3 footOffset;

    public Vector3 OffsetPosition
    {
        get { return transform.position + footOffset; }
        private set { transform.position = value - footOffset; }
    }

    [SerializeField, Tooltip("位置近似范围半径，最小建议值为0.1")]
    private float fixedOffset = 0.125f;

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("无条件位移(推荐)[物理模拟差]", "刚体位移[性能消耗高]", "控制器位移[未使用]")]
#endif
    private MoveType moveType = MoveType.MovePosition;

    [SerializeField]
    private new Rigidbody rigidbody;

    [SerializeField]
    private new Rigidbody2D rigidbody2D;

    [SerializeField]
    private CharacterController controller;

    public float moveSpeed = 10f;

    public float turnSpeed = 10f;

    [SerializeField]
    private float maxMoveSlope = 45.0f;

    [SerializeField]
    public float stopDistance = 1;

    #region 实时变量成员
    private Vector3[] path;
    private int targetWaypointIndex;
    private Vector3 oldPosition;

    private bool isFollowingPath;
    public bool IsFollowingPath
    {
        get => isFollowingPath;
        set
        {
            bool origin = isFollowingPath;
            isFollowingPath = value;
            if (!origin && isFollowingPath) StartFollowingPath();
            else if (origin && !isFollowingPath)
            {
                if (pathFollowCoroutine != null) StopCoroutine(pathFollowCoroutine);
                pathFollowCoroutine = null;
            }
        }
    }

    private Coroutine pathFollowCoroutine;

    private bool isFollowingTarget;
    public bool IsFollowingTarget
    {
        get => isFollowingTarget;
        set
        {
            bool origin = isFollowingTarget;
            isFollowingTarget = value;
            if (!origin && isFollowingTarget) StartFollowingTarget();
            else if (origin && !isFollowingTarget)
            {
                if (targetFollowCoroutine != null) StopCoroutine(targetFollowCoroutine);
                targetFollowCoroutine = null;
                IsFollowingPath = false;
            }
        }
    }

    private Coroutine targetFollowCoroutine;

    public bool IsStop { get; private set; }

    public Vector3 DesiredVelocity//类似于NavMeshAgent.desiredVelocity
    {
        get
        {
            if (path == null || path.Length < 1)
            {
                return Vector3.zero;
            }
            if (targetWaypointIndex >= 0 && targetWaypointIndex < path.Length)
            {
                IsStop = false;
                Vector3 targetWaypoint = path[targetWaypointIndex];
                if (!IsFollowingPath && OffsetPosition.x >= targetWaypoint.x - fixedOffset && OffsetPosition.x <= targetWaypoint.x + fixedOffset
                    && (AStarManager.Instance.ThreeD ? true : OffsetPosition.y >= targetWaypoint.y - fixedOffset && OffsetPosition.y <= targetWaypoint.y + fixedOffset)
                    && (AStarManager.Instance.ThreeD ? OffsetPosition.z >= targetWaypoint.z - fixedOffset && OffsetPosition.z <= targetWaypoint.z + fixedOffset : true))
                //因为多数情况无法准确定位，所以用近似值表示达到目标航点
                {
                    targetWaypointIndex++;
                    if (targetWaypointIndex >= path.Length) return Vector3.zero;
                    if (!AStarManager.Instance.WorldPointWalkable(path[targetWaypointIndex], unitSize))
                        RequestPath(Destination);
                }
                if (targetWaypointIndex < path.Length)
                {
                    if (AStarManager.Instance.ThreeD && MyTools.Slope(transform.position, path[targetWaypointIndex]) > maxMoveSlope)
                        return Vector3.zero;
                    if (Vector3.Distance(OffsetPosition, Destination) <= stopDistance)
                    {
                        ResetPath();
                        return Vector3.zero;
                    }
                    return (path[targetWaypointIndex] - OffsetPosition).normalized * moveSpeed;
                }
            }
            return Vector3.zero;
        }
    }

    public Vector3 Velocity => IsFollowingTarget || IsFollowingPath ? (OffsetPosition - oldPosition).normalized * moveSpeed : Vector3.zero;

    private Vector3 Destination
    {
        get
        {
            if (path == null || path.Length < 1) return OffsetPosition;
            return path[path.Length - 1];
        }
    }
    #endregion

    [SerializeField]
    private LineRenderer pathRenderer;

    #region MonoBehaviour

    [SerializeField]
    private bool drawGizmos;
    private Vector3 gizmosTargetPos = default;

    private void Awake()
    {
        ShowPath(false);
    }

    private void Start()
    {
        SetTarget(target, targetFootOffset, true);
    }

    private void Update()
    {
        if (pathRenderer && path != null && path.Length > 0)
        {
            LinkedList<Vector3> leftPoint = new LinkedList<Vector3>(path.Skip(targetWaypointIndex).ToArray());
            leftPoint.AddFirst(OffsetPosition);
            pathRenderer.positionCount = leftPoint.Count;
            pathRenderer.SetPositions(leftPoint.ToArray());
            if (Vector3.Distance(OffsetPosition, Destination) < stopDistance) ShowPath(false);
        }
    }

    private void LateUpdate()
    {
        oldPosition = OffsetPosition;
        if (Vector3.Distance(OffsetPosition, Destination) <= stopDistance)
        {
            IsStop = true;
            ResetPath();
            ShowPath(false);
        }
        else IsStop = false;
    }

    private void OnEnable()
    {
        if (!AStarManager.Instance) enabled = false;
        oldPosition = OffsetPosition;
    }

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            if (path != null)
                for (int i = targetWaypointIndex; i >= 0 && i < path.Length; i++)
                {
                    if (i != path.Length - 1) Gizmos.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
                    else Gizmos.color = new Color(Color.green.r, Color.green.g, Color.green.b, 0.5f);
                    Gizmos.DrawSphere(path[i], 0.25f);

                    if (i == targetWaypointIndex)
                        Gizmos.DrawLine(OffsetPosition, path[i]);
                    else Gizmos.DrawLine(path[i - 1], path[i]);
                }
            if (gizmosTargetPos != default && AStarManager.Instance)
            {
                Gizmos.color = new Color(Color.black.r, Color.black.g, Color.black.b, 0.75f);
                Gizmos.DrawCube(new Vector3(gizmosTargetPos.x, gizmosTargetPos.y, 0), Vector3.one * AStarManager.Instance.BaseCellSize * unitSize * 0.95f);
            }
        }
    }
    #endregion

    #region 路径相关
    public void SetPath(IEnumerable<Vector3> newPath, bool followImmediate = false)
    {
        if (newPath == null || !AStarManager.Instance) return;
        ResetPath();
        IsFollowingPath = followImmediate;
        OnPathFound(newPath, true);
    }

    public void ResetPath()
    {
        path = null;
        if (pathFollowCoroutine != null) StopCoroutine(pathFollowCoroutine);
        pathFollowCoroutine = null;
        targetWaypointIndex = 0;
        isFollowingPath = false;
        //ShowPath(false);
        gizmosTargetPos = default;
    }

    private void RequestPath(Vector3 destination)
    {
        if (!AStarManager.Instance || destination == default || destination == OffsetPosition) return;
        AStarManager.Instance.RequestPath(new PathRequest(OffsetPosition, destination, unitSize, OnPathFound));
    }

    private void OnPathFound(IEnumerable<Vector3> newPath, bool findSuccessfully)
    {
        if (findSuccessfully && newPath != null && newPath.Count() > 0)
        {
            path = newPath.ToArray();
            if (pathRenderer)
            {
                LinkedList<Vector3> path = new LinkedList<Vector3>(this.path);
                path.AddFirst(OffsetPosition);
                pathRenderer.positionCount = path.Count;
                pathRenderer.SetPositions(path.ToArray());
            }
            targetWaypointIndex = 0;
            if (IsFollowingPath) StartFollowingPath();
        }
    }

    private void StartFollowingPath()
    {
        if (pathFollowCoroutine != null) StopCoroutine(pathFollowCoroutine);
        pathFollowCoroutine = StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath()
    {
        if (path == null || path.Length < 1 || !IsFollowingPath) yield break; //yield break相当于普通函数空return
        Vector3 targetWaypoint = path[0];
        while (IsFollowingPath)//模拟更新函数
        {
            IsStop = false;
            if (path.Length < 1)//如果在追踪过程中，路线没了，直接退出追踪
            {
                ResetPath();
                yield break;
            }
            if (OffsetPosition.x >= targetWaypoint.x - fixedOffset && OffsetPosition.x <= targetWaypoint.x + fixedOffset//因为很多情况下无法准确定位，所以用近似值
                && (AStarManager.Instance.ThreeD ? true : OffsetPosition.y >= targetWaypoint.y - fixedOffset && OffsetPosition.y <= targetWaypoint.y + fixedOffset)
                && (AStarManager.Instance.ThreeD ? OffsetPosition.z >= targetWaypoint.z - fixedOffset && OffsetPosition.z <= targetWaypoint.z + fixedOffset : true))
            {
                targetWaypointIndex++;//标至下一个航点
                if (targetWaypointIndex >= path.Length) yield break;
                targetWaypoint = path[targetWaypointIndex];
                if (!AStarManager.Instance.WorldPointWalkable(targetWaypoint, unitSize))//自动修复路线
                {
                    Debug.Log("Auto fix path");
                    RequestPath(Destination);
                    yield break;
                }
            }
            if (AStarManager.Instance.ThreeD && MyTools.Slope(OffsetPosition, targetWaypoint) > maxMoveSlope) yield break;
            if (Vector3.Distance(OffsetPosition, Destination) <= stopDistance)
            {
                ResetPath();
                //Debug.Log("Stop");
                ShowPath(false);
                yield break;
            }
            if (moveType == MoveType.MovePosition)
            {
                OffsetPosition = Vector3.MoveTowards(OffsetPosition, targetWaypoint, Time.deltaTime * moveSpeed);
                if (AStarManager.Instance.ThreeD)
                {
                    Vector3 targetDir = new Vector3(DesiredVelocity.x, 0, DesiredVelocity.z);//获取平面上的朝向
                    Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);//计算绕Vector3.up使transform.forward对齐targetDir所需的旋转量
                    Quaternion newQuaternion = Quaternion.Lerp(rigidbody.rotation, targetRotation, turnSpeed * Time.deltaTime);
                    transform.Rotate(newQuaternion.eulerAngles);
                }
                yield return null;//相当于以上步骤在Update()里执行，至于为什么不直接放在Update()里，一句两句说不清楚，感兴趣的自己试一试有什么区别
            }
            else
            {
                if (AStarManager.Instance.ThreeD && rigidbody)
                {
                    Vector3 targetDir = new Vector3(DesiredVelocity.x, 0, DesiredVelocity.z);
                    Quaternion targetRotation = Quaternion.LookRotation(targetDir, Vector3.up);
                    Quaternion newQuaternion = Quaternion.Lerp(rigidbody.rotation, targetRotation, turnSpeed * Time.deltaTime);
                    rigidbody.MoveRotation(newQuaternion);
                    rigidbody.MovePosition(rigidbody.position + DesiredVelocity * Time.deltaTime);
                }
                else if (!AStarManager.Instance.ThreeD && rigidbody2D)
                {
                    rigidbody2D.MovePosition((Vector3)rigidbody2D.position + DesiredVelocity * Time.deltaTime);
                }
                yield return new WaitForFixedUpdate();//相当于以上步骤在FiexdUpdate()里执行
            }
        }
    }

    public void ShowPath(bool value)
    {
        if (!pathRenderer) return;
        pathRenderer.positionCount = 0;
        pathRenderer.enabled = value;
    }
    #endregion

    #region 目标相关
    public void SetDestination(Vector3 destination, bool gotoImmediately = true, bool force = true)//类似NavMeshAgent.SetDestination()，但不建议逐帧使用
    {
        if (drawGizmos) gizmosTargetPos = destination;
        if (!AStarManager.Instance || destination == default) return;
        if (drawGizmos)
        {
            AStarNode goal = AStarManager.Instance.WorldPointToNode(destination, unitSize);
            if (goal) gizmosTargetPos = goal;
        }
        if (!AStarManager.Instance.WorldPointWalkable(destination, unitSize) && !force)
        {
            ResetPath();
            return;
        }
        RequestPath(destination);
        IsFollowingPath = gotoImmediately;
    }

    public void SetTarget(Transform target, Vector3 footOffset = default, bool followImmediately = false)
    {
        if (!target || !AStarManager.Instance) return;
        this.target = target;
        if (footOffset == default) targetFootOffset = Vector3.zero;
        if (!followImmediately) SetDestination(TargetPosition, false);
        else IsFollowingTarget = true;
    }

    public void ResetTarget()
    {
        target = null;
        if (targetFollowCoroutine != null) StopCoroutine(targetFollowCoroutine);
        targetFollowCoroutine = null;
        IsFollowingTarget = false;
    }

    private void StartFollowingTarget()
    {
        if (targetFollowCoroutine != null) StopCoroutine(targetFollowCoroutine);
        targetFollowCoroutine = StartCoroutine(FollowTarget());
    }

    private IEnumerator FollowTarget()
    {
        if (!target) yield break;
        Vector3 oldTargetPosition = TargetPosition;
        SetDestination(TargetPosition);
        while (target)
        {
            yield return new WaitForSeconds(0.15f);
            if (Vector3.Distance(oldTargetPosition, TargetPosition) > targetFollowStartDistance)
            {
                SetDestination(TargetPosition);
                oldTargetPosition = TargetPosition;
            }
        }
    }
    #endregion

    private enum MoveType
    {
        MovePosition,
        MoveRigidbody,
        MoveController
    }
}