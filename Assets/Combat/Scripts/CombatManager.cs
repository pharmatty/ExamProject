using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CombatManager : MonoBehaviour
{
    public enum CombatState
    {
        PlayerCommand,
        TargetSelection,
        EnemyTurn,
        Busy
    }

    public enum CameraState
    {
        BattleStart,
        PlayerCommand,
        TargetSelection,
        Attack
    }

    private CombatState state;

    [Header("Spawn Points")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("Prefabs")]
    public GameObject playerBattleUnitPrefab;

    [Header("Enemy Database")]
    public List<EnemyData> possibleEnemies = new();

    [Header("Runtime Units")]
    public List<BattleUnit> playerUnits = new();
    public List<BattleUnit> enemyUnits = new();

    [Header("Input")]
    public InputActionAsset inputActions;

    private InputActionMap combatMap;
    private InputAction confirm;
    private InputAction back;
    private InputAction left;
    private InputAction right;

    [Header("UI")]
    public BattleUIManager uiManager;

    [Header("Cinemachine Cameras")]
    public CinemachineCamera camBattleStart;
    public CinemachineCamera camPlayerCommand;
    public CinemachineCamera camTargetSelection;
    public CinemachineCamera camAttack;

    [Header("Target Selection Camera Settings")]
    public float targetCamDistance = 6f;
    public float targetCamHeight = 2f;

    [Header("Target Camera Smoothing")]
    public float cameraMoveSmoothTime = 0.12f;
    public float cameraRotateSpeed = 14f;

    private Vector3 desiredCamPosition;
    private Vector3 camVelocity;
    private Transform desiredLookTarget;

    private int currentTargetIndex = -1;

    private void OnEnable()
    {
        combatMap = inputActions.FindActionMap("CombatControls", true);
        combatMap.Enable();

        confirm = combatMap.FindAction("Confirm");
        back = combatMap.FindAction("Back");
        left = combatMap.FindAction("TargetLeft");
        right = combatMap.FindAction("TargetRight");

        confirm.performed += OnConfirm;
        back.performed += OnBack;
        left.performed += OnLeft;
        right.performed += OnRight;
    }

    private void OnDisable()
    {
        confirm.performed -= OnConfirm;
        back.performed -= OnBack;
        left.performed -= OnLeft;
        right.performed -= OnRight;
        combatMap.Disable();
    }

    private void Start()
    {
        // ✅ NEW: Safe UI lookup (only if not assigned in Inspector)
        if (uiManager == null)
            uiManager = FindFirstObjectByType<BattleUIManager>();

        SpawnPlayerUnits();
        SpawnEnemyUnits();

        // ✅ NEW: Sync HP UI once at battle start
        SyncPlayerHealthUI();

        StartCoroutine(StartCombatSequence());
    }

    // ✅ NEW: minimal helper, no behavior change
    private void SyncPlayerHealthUI()
    {
        if (uiManager == null) return;
        if (playerUnits.Count == 0) return;

        BattleUnit player = playerUnits[0];

        uiManager.UpdateHealth(
            player.characterData.currentHealth,
            player.characterData.maxHealth
        );
    }

    private void Update()
    {
        if (state != CombatState.TargetSelection)
            return;

        camTargetSelection.transform.position = Vector3.SmoothDamp(
            camTargetSelection.transform.position,
            desiredCamPosition,
            ref camVelocity,
            cameraMoveSmoothTime
        );

        if (desiredLookTarget != null)
        {
            Vector3 lookDir = desiredLookTarget.position - camTargetSelection.transform.position;
            Quaternion targetRot = Quaternion.LookRotation(lookDir);

            camTargetSelection.transform.rotation = Quaternion.Slerp(
                camTargetSelection.transform.rotation,
                targetRot,
                Time.deltaTime * cameraRotateSpeed
            );
        }
    }

    private IEnumerator StartCombatSequence()
    {
        state = CombatState.Busy;
        SetCamera(CameraState.BattleStart);
        yield return new WaitForSeconds(4f);
        EnterPlayerCommand();
    }

    private void EnterPlayerCommand()
    {
        state = CombatState.PlayerCommand;
        uiManager?.ShowCommandPanel(true);
        uiManager?.HighlightCommand("Attack");
        SetCamera(CameraState.PlayerCommand);
    }

    private void EnterTargetSelection()
    {
        if (enemyUnits.Count == 0)
            return;

        state = CombatState.TargetSelection;
        uiManager?.ShowCommandPanel(false);
        SelectFirstAliveTarget();
        UpdateTargetSelectionCamera(true);
        SetCamera(CameraState.TargetSelection);
    }

    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.PlayerCommand)
            EnterTargetSelection();
        else if (state == CombatState.TargetSelection)
            ConfirmAttack();
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.TargetSelection)
            EnterPlayerCommand();
    }

    private void OnLeft(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.TargetSelection)
            CycleTarget(-1);
    }

    private void OnRight(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.TargetSelection)
            CycleTarget(1);
    }

    private void SelectFirstAliveTarget()
    {
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (!enemyUnits[i].IsDead)
            {
                currentTargetIndex = i;
                return;
            }
        }
        currentTargetIndex = -1;
    }

    private void CycleTarget(int direction)
    {
        int startIndex = currentTargetIndex;

        do
        {
            currentTargetIndex += direction;

            if (currentTargetIndex < 0)
                currentTargetIndex = enemyUnits.Count - 1;
            if (currentTargetIndex >= enemyUnits.Count)
                currentTargetIndex = 0;

        } while (enemyUnits[currentTargetIndex].IsDead &&
                 currentTargetIndex != startIndex);

        UpdateTargetSelectionCamera(false);
    }

    private void UpdateTargetSelectionCamera(bool instant)
    {
        if (currentTargetIndex < 0 ||
            currentTargetIndex >= enemySpawnPoints.Length)
            return;

        Transform targetPoint = enemySpawnPoints[currentTargetIndex];

        desiredCamPosition =
            targetPoint.position -
            targetPoint.forward * targetCamDistance +
            Vector3.up * targetCamHeight;

        desiredLookTarget = targetPoint;

        if (instant)
        {
            camTargetSelection.transform.position = desiredCamPosition;
            camTargetSelection.transform.LookAt(targetPoint.position);
            camVelocity = Vector3.zero;
        }
    }

    private void ConfirmAttack()
    {
        if (currentTargetIndex < 0 || currentTargetIndex >= enemyUnits.Count)
            return;

        BattleUnit attacker = playerUnits[0];
        BattleUnit target = enemyUnits[currentTargetIndex];

        state = CombatState.Busy;
        SetCamera(CameraState.Attack);
        StartCoroutine(AttackSequence(attacker, target));
    }

    private IEnumerator AttackSequence(BattleUnit attacker, BattleUnit target)
    {
        yield return StartCoroutine(attacker.PerformAttack(target));
        yield return new WaitForSeconds(0.25f);

        // If all enemies are dead, stop here (victory flow can be added later)
        if (enemyUnits.TrueForAll(e => e == null || e.IsDead))
            yield break;

        yield return StartCoroutine(EnemyTurnSequence());

        // If all players are dead, stop here (defeat flow can be added later)
        if (playerUnits.TrueForAll(p => p == null || p.IsDead))
            yield break;

        EnterPlayerCommand();
    }

    // =========================
    // ENEMY TURN SEQUENCE (NEW, SAFE)
    // =========================
    private IEnumerator EnemyTurnSequence()
    {
        state = CombatState.EnemyTurn;

        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null)
                continue;

            // Target the first alive player (expand later)
            BattleUnit target = playerUnits.Find(p => p != null && !p.IsDead);
            if (target == null)
                yield break;

            SetCamera(CameraState.Attack);

            yield return StartCoroutine(ai.TakeTurn(target));
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void SetCamera(CameraState cam)
    {
        camBattleStart.Priority = 0;
        camPlayerCommand.Priority = 0;
        camTargetSelection.Priority = 0;
        camAttack.Priority = 0;

        switch (cam)
        {
            case CameraState.BattleStart: camBattleStart.Priority = 10; break;
            case CameraState.PlayerCommand: camPlayerCommand.Priority = 10; break;
            case CameraState.TargetSelection: camTargetSelection.Priority = 10; break;
            case CameraState.Attack: camAttack.Priority = 10; break;
        }
    }

    private void SpawnPlayerUnits()
    {
        var party = GameManager.Instance.partyData.currentParty;

        for (int i = 0; i < party.Count && i < playerSpawnPoints.Length; i++)
        {
            GameObject obj = Instantiate(
                playerBattleUnitPrefab,
                playerSpawnPoints[i].position,
                playerSpawnPoints[i].rotation
            );

            BattleUnit unit = obj.GetComponent<BattleUnit>();
            unit.Initialize(party[i], null, false);
            playerUnits.Add(unit);
        }
    }

    private void SpawnEnemyUnits()
    {
        int count = Random.Range(1, enemySpawnPoints.Length + 1);

        for (int i = 0; i < count; i++)
        {
            EnemyData data = possibleEnemies[Random.Range(0, possibleEnemies.Count)];

            GameObject obj = Instantiate(
                data.enemyPrefab,
                enemySpawnPoints[i].position,
                enemySpawnPoints[i].rotation
            );

            BattleUnit unit = obj.GetComponent<BattleUnit>();
            unit.Initialize(null, data, true);
            enemyUnits.Add(unit);
        }
    }
}
