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
        Attack,
        EnemyAttack
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
    public CinemachineCamera camEnemyAttack;

    [Header("Target Selection Camera")]
    public float targetCamDistance = 6f;
    public float targetCamHeight = 2f;
    public float enemyLookHeight = 1.2f;

    [Header("Enemy Attack Camera")]
    public float enemyAttackCamDistance = 4f;
    public float enemyAttackCamHeight = 2f;

    [Header("Camera Smoothing")]
    public float cameraMoveSmoothTime = 0.12f;
    public float cameraRotateSpeed = 14f;

    private Vector3 desiredCamPosition;
    private Vector3 camVelocity;
    private Transform desiredLookTarget;

    private int currentTargetIndex = -1;

    // =========================
    // INPUT
    // =========================
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

    // =========================
    // SETUP
    // =========================
    private void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<BattleUIManager>();

        SpawnPlayerUnits();
        SpawnEnemyUnits();
        SyncPlayerHealthUI();

        StartCoroutine(StartCombatSequence());
    }

    private void SyncPlayerHealthUI()
    {
        if (uiManager == null || playerUnits.Count == 0)
            return;

        BattleUnit player = playerUnits[0];
        uiManager.UpdateHealth(
            player.characterData.currentHealth,
            player.characterData.maxHealth
        );
    }

    // =========================
    // UPDATE (TARGET CAMERA)
    // =========================
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
            Vector3 lookPoint = desiredLookTarget.position + Vector3.up * enemyLookHeight;
            Quaternion rot = Quaternion.LookRotation(lookPoint - camTargetSelection.transform.position);

            camTargetSelection.transform.rotation = Quaternion.Slerp(
                camTargetSelection.transform.rotation,
                rot,
                Time.deltaTime * cameraRotateSpeed
            );
        }
    }

    // =========================
    // FLOW
    // =========================
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

    // =========================
    // INPUT HANDLERS
    // =========================
    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.PlayerCommand)
            EnterTargetSelection();
        else if (state == CombatState.TargetSelection)
            ConfirmAttack();
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        if (state != CombatState.TargetSelection)
            return;

        currentTargetIndex = -1;
        EnterPlayerCommand(); // ✅ returns camera + UI
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

    // =========================
    // TARGETING
    // =========================
    private void SelectFirstAliveTarget()
    {
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i] != null && !enemyUnits[i].IsDead)
            {
                currentTargetIndex = i;
                return;
            }
        }

        currentTargetIndex = -1;
    }

    private void CycleTarget(int direction)
    {
        if (enemyUnits.Count == 0)
            return;

        int startIndex = currentTargetIndex;
        if (startIndex < 0)
            startIndex = 0;

        do
        {
            currentTargetIndex += direction;

            if (currentTargetIndex < 0)
                currentTargetIndex = enemyUnits.Count - 1;
            if (currentTargetIndex >= enemyUnits.Count)
                currentTargetIndex = 0;

        } while ((enemyUnits[currentTargetIndex] == null || enemyUnits[currentTargetIndex].IsDead) &&
                 currentTargetIndex != startIndex);

        UpdateTargetSelectionCamera(false);
    }

    // =========================
    // TARGET SELECTION CAMERA (FIXED SIDE)
    // =========================
    private void UpdateTargetSelectionCamera(bool instant)
    {
        if (currentTargetIndex < 0 || currentTargetIndex >= enemyUnits.Count)
            return;

        BattleUnit targetUnit = enemyUnits[currentTargetIndex];
        if (targetUnit == null || targetUnit.spawnIndex < 0)
            return;

        Vector3 enemyPos = enemySpawnPoints[targetUnit.spawnIndex].position;
        Vector3 playerPos = playerSpawnPoints[0].position;

        Vector3 dirToPlayer = playerPos - enemyPos;
        dirToPlayer.y = 0f;
        dirToPlayer.Normalize();

        // ✅ CAMERA ON PLAYER SIDE OF ENEMY
        desiredCamPosition =
            enemyPos +
            dirToPlayer * targetCamDistance +
            Vector3.up * targetCamHeight;

        desiredLookTarget = enemySpawnPoints[targetUnit.spawnIndex];

        if (instant)
        {
            camTargetSelection.transform.position = desiredCamPosition;
            camTargetSelection.transform.LookAt(enemyPos + Vector3.up * enemyLookHeight);
            camVelocity = Vector3.zero;
        }
    }

    private void ConfirmAttack()
    {
        BattleUnit attacker = playerUnits[0];
        BattleUnit target = enemyUnits[currentTargetIndex];

        if (attacker == null || target == null || target.IsDead)
            return;

        state = CombatState.Busy;
        SetCamera(CameraState.Attack);
        StartCoroutine(AttackSequence(attacker, target));
    }

    private IEnumerator AttackSequence(BattleUnit attacker, BattleUnit target)
    {
        yield return StartCoroutine(attacker.PerformAttack(target));
        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(EnemyTurnSequence());
        EnterPlayerCommand();
    }

    // =========================
    // ENEMY TURN
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

            BattleUnit target = playerUnits.Find(p => p != null && !p.IsDead);
            if (target == null)
                yield break;

            SetupEnemyAttackCamera(enemy.spawnIndex);
            SetCamera(CameraState.EnemyAttack);

            yield return StartCoroutine(ai.TakeTurn(target));
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void SetupEnemyAttackCamera(int spawnIndex)
    {
        Vector3 enemyPos = enemySpawnPoints[spawnIndex].position;
        Vector3 playerPos = playerSpawnPoints[0].position;

        Vector3 dirToPlayer = playerPos - enemyPos;
        dirToPlayer.y = 0f;
        dirToPlayer.Normalize();

        camEnemyAttack.transform.position =
            enemyPos -
            dirToPlayer * enemyAttackCamDistance +
            Vector3.up * enemyAttackCamHeight;

        camEnemyAttack.transform.LookAt(playerPos + Vector3.up * enemyLookHeight);
    }

    // =========================
    // CAMERA SWITCHING
    // =========================
    private void SetCamera(CameraState cam)
    {
        camBattleStart.Priority = 0;
        camPlayerCommand.Priority = 0;
        camTargetSelection.Priority = 0;
        camAttack.Priority = 0;
        camEnemyAttack.Priority = 0;

        switch (cam)
        {
            case CameraState.BattleStart: camBattleStart.Priority = 10; break;
            case CameraState.PlayerCommand: camPlayerCommand.Priority = 10; break;
            case CameraState.TargetSelection: camTargetSelection.Priority = 10; break;
            case CameraState.Attack: camAttack.Priority = 10; break;
            case CameraState.EnemyAttack: camEnemyAttack.Priority = 10; break;
        }
    }

    // =========================
    // SPAWNING
    // =========================
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
            unit.spawnIndex = i;

            enemyUnits.Add(unit);
        }
    }
}