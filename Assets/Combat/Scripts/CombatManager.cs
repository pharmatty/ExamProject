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
        SkillSelect,
        ItemSelect,
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
    private PlayerCommand selectedCommand = PlayerCommand.Attack;

    public enum PlayerCommand
    {
        Attack,
        Skill,
        Item,
        Escape
    }

    // ========================= UNITS =========================
    public List<BattleUnit> playerUnits = new();
    public List<BattleUnit> enemyUnits = new();
    private int currentTargetIndex = -1;

    // ========================= SKILLS / ITEMS =========================
    [Header("Player Skills")]
    public List<SkillData> availableSkills = new();
    private int selectedSkillIndex = 0;

    [Header("Player Items")]
    public List<ItemData> availableItems = new();
    private int selectedItemIndex = 0;

    // ========================= INPUT (RESTORED) =========================
    [Header("Input")]
    public InputActionAsset inputActions;

    private InputActionMap combatMap;
    private InputAction confirm;
    private InputAction back;
    private InputAction left;
    private InputAction right;

    // ========================= UI / CAMERAS =========================
    public BattleUIManager uiManager;

    public CinemachineCamera camBattleStart;
    public CinemachineCamera camPlayerCommand;
    public CinemachineCamera camTargetSelection;
    public CinemachineCamera camAttack;
    public CinemachineCamera camEnemyAttack;

    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;
    public GameObject playerBattleUnitPrefab;
    public List<EnemyData> possibleEnemies = new();

    private Vector3 desiredCamPosition;
    private Vector3 camVelocity;
    private Transform desiredLookTarget;

    public float targetCamDistance = 6f;
    public float targetCamHeight = 2f;
    public float enemyLookHeight = 1.2f;

    public float enemyAttackCamDistance = 4f;
    public float enemyAttackCamHeight = 2f;

    public float cameraMoveSmoothTime = 0.12f;
    public float cameraRotateSpeed = 14f;

    // ========================= INPUT ENABLE =========================
    private void OnEnable()
    {
        combatMap = inputActions.FindActionMap("CombatControls", true);
        combatMap.Enable();

        confirm = combatMap.FindAction("Confirm");
        back    = combatMap.FindAction("Back");
        left    = combatMap.FindAction("TargetLeft");
        right   = combatMap.FindAction("TargetRight");

        confirm.performed += OnConfirm;
        back.performed    += OnBack;
        left.performed    += OnLeft;
        right.performed   += OnRight;
    }

    private void OnDisable()
    {
        confirm.performed -= OnConfirm;
        back.performed    -= OnBack;
        left.performed    -= OnLeft;
        right.performed   -= OnRight;

        combatMap.Disable();
    }

    // ========================= STARTUP =========================
    private void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<BattleUIManager>();

        SpawnPlayerUnits();
        SpawnEnemyUnits();
        SyncPlayerUI();

        StartCoroutine(StartCombatSequence());
    }

    private void SyncPlayerUI()
    {
        if (playerUnits.Count == 0 || uiManager == null)
            return;

        var p = playerUnits[0];

        uiManager.UpdateHealth(p.characterData.currentHealth, p.characterData.maxHealth);
        uiManager.UpdateSP(p.characterData.currentSkillPoints, p.characterData.maxSkillPoints);

        RefreshCommandAvailability();
    }

    // ========================= SP → SKILL FADE =========================
    private void RefreshCommandAvailability()
    {
        if (playerUnits.Count == 0 || uiManager == null)
            return;

        BattleUnit player = playerUnits[0];

        bool canUseSkill = false;

        foreach (var skill in availableSkills)
        {
            if (player.CanAffordSP(skill.skillPointCost))
            {
                canUseSkill = true;
                break;
            }
        }

        uiManager.SetCommandInteractable(uiManager.skillsLabelGroup, canUseSkill);
    }

    // ========================= UPDATE CAMERA =========================
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

    // ========================= COMBAT FLOW =========================
    private IEnumerator StartCombatSequence()
    {
        state = CombatState.Busy;
        SetCamera(CameraState.BattleStart);

        yield return new WaitForSeconds(2f);

        EnterPlayerCommand();
    }

    private void EnterPlayerCommand()
    {
        state = CombatState.PlayerCommand;

        uiManager.ShowCommandPanel(true);
        selectedCommand = PlayerCommand.Attack;

        RefreshCommandAvailability();

        SetCamera(CameraState.PlayerCommand);
    }

    // ========================= INPUT =========================
    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.PlayerCommand)
        {
            switch (selectedCommand)
            {
                case PlayerCommand.Attack:
                    EnterTargetSelection();
                    break;

                case PlayerCommand.Skill:
                    state = CombatState.SkillSelect;
                    break;

                case PlayerCommand.Item:
                    state = CombatState.ItemSelect;
                    break;
            }
        }
        else if (state == CombatState.SkillSelect)
        {
            EnterTargetSelection();
        }
        else if (state == CombatState.TargetSelection)
        {
            if (selectedCommand == PlayerCommand.Attack)
                ConfirmAttack();
            else if (selectedCommand == PlayerCommand.Skill)
                ConfirmSkill();
        }
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.TargetSelection ||
            state == CombatState.SkillSelect ||
            state == CombatState.ItemSelect)
        {
            EnterPlayerCommand();
        }
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

    // ========================= TARGET SELECTION =========================
    private void EnterTargetSelection()
    {
        if (enemyUnits.Count == 0)
            return;

        state = CombatState.TargetSelection;
        uiManager.ShowCommandPanel(false);

        SelectFirstAliveTarget();
        UpdateTargetSelectionCamera(true);

        SetCamera(CameraState.TargetSelection);
    }

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

    private void CycleTarget(int dir)
    {
        if (enemyUnits.Count == 0)
            return;

        int start = currentTargetIndex;
        if (start < 0) start = 0;

        do
        {
            currentTargetIndex += dir;

            if (currentTargetIndex < 0)
                currentTargetIndex = enemyUnits.Count - 1;

            if (currentTargetIndex >= enemyUnits.Count)
                currentTargetIndex = 0;

        } while ((enemyUnits[currentTargetIndex] == null ||
                  enemyUnits[currentTargetIndex].IsDead) &&
                  currentTargetIndex != start);

        UpdateTargetSelectionCamera(false);
    }

    private void UpdateTargetSelectionCamera(bool instant)
    {
        BattleUnit target = enemyUnits[currentTargetIndex];
        if (target == null)
            return;

        Vector3 enemyPos  = enemySpawnPoints[target.spawnIndex].position;
        Vector3 playerPos = playerSpawnPoints[0].position;

        Vector3 dir = (playerPos - enemyPos);
        dir.y = 0;
        dir.Normalize();

        desiredCamPosition =
            enemyPos +
            dir * targetCamDistance +
            Vector3.up * targetCamHeight;

        desiredLookTarget = enemySpawnPoints[target.spawnIndex];

        if (instant)
        {
            camTargetSelection.transform.position = desiredCamPosition;
            camTargetSelection.transform.LookAt(enemyPos + Vector3.up * enemyLookHeight);
            camVelocity = Vector3.zero;
        }
    }

    // ========================= ATTACK =========================
    private void ConfirmAttack()
    {
        BattleUnit attacker = playerUnits[0];
        BattleUnit target   = enemyUnits[currentTargetIndex];

        if (attacker == null || target == null || target.IsDead)
            return;

        state = CombatState.Busy;

        SetCamera(CameraState.Attack);
        StartCoroutine(AttackSequence(attacker, target));
    }

    private IEnumerator AttackSequence(BattleUnit attacker, BattleUnit target)
    {
        yield return attacker.PerformAttack(target);
        yield return new WaitForSeconds(0.25f);

        yield return EnemyTurnSequence();

        RefreshCommandAvailability();
        EnterPlayerCommand();
    }

    // ========================= SKILL =========================
    private void ConfirmSkill()
    {
        BattleUnit user   = playerUnits[0];
        BattleUnit target = enemyUnits[currentTargetIndex];
        SkillData skill   = availableSkills[selectedSkillIndex];

        if (!user.CanAffordSP(skill.skillPointCost))
        {
            Debug.Log("Not enough SP for " + skill.skillName);
            EnterPlayerCommand();
            return;
        }

        user.SpendSP(skill.skillPointCost);
        RefreshCommandAvailability();

        state = CombatState.Busy;
        SetCamera(CameraState.Attack);

        StartCoroutine(SkillSequence(user, target));
    }

    private IEnumerator SkillSequence(BattleUnit user, BattleUnit target)
    {
        yield return user.PerformSkill(target);
        yield return EnemyTurnSequence();

        EnterPlayerCommand();
    }

    // ========================= ENEMY TURN =========================
    private IEnumerator EnemyTurnSequence()
    {
        state = CombatState.EnemyTurn;

        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            BattleUnit target = playerUnits[0];

            SetupEnemyAttackCamera(enemy.spawnIndex);
            SetCamera(CameraState.EnemyAttack);

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                yield return ai.TakeTurn(target);

            yield return new WaitForSeconds(0.4f);
        }
    }

    private void SetupEnemyAttackCamera(int spawnIndex)
    {
        Vector3 enemyPos  = enemySpawnPoints[spawnIndex].position;
        Vector3 playerPos = playerSpawnPoints[0].position;

        Vector3 dir = (playerPos - enemyPos);
        dir.y = 0;
        dir.Normalize();

        camEnemyAttack.transform.position =
            enemyPos -
            dir * enemyAttackCamDistance +
            Vector3.up * enemyAttackCamHeight;

        camEnemyAttack.transform.LookAt(playerPos + Vector3.up * enemyLookHeight);
    }

    // ========================= CAMERA SWITCH =========================
    private void SetCamera(CameraState cam)
    {
        camBattleStart.Priority     = 0;
        camPlayerCommand.Priority   = 0;
        camTargetSelection.Priority = 0;
        camAttack.Priority          = 0;
        camEnemyAttack.Priority     = 0;

        switch (cam)
        {
            case CameraState.BattleStart:     camBattleStart.Priority   = 10; break;
            case CameraState.PlayerCommand:   camPlayerCommand.Priority = 10; break;
            case CameraState.TargetSelection: camTargetSelection.Priority = 10; break;
            case CameraState.Attack:          camAttack.Priority        = 10; break;
            case CameraState.EnemyAttack:     camEnemyAttack.Priority   = 10; break;
        }
    }

    // ========================= SPAWNING =========================
    private void SpawnPlayerUnits()
    {
        var party = GameManager.Instance.partyData.currentParty;

        GameObject obj = Instantiate(
            playerBattleUnitPrefab,
            playerSpawnPoints[0].position,
            playerSpawnPoints[0].rotation
        );

        BattleUnit unit = obj.GetComponent<BattleUnit>();
        unit.Initialize(party[0], null, false);
        playerUnits.Add(unit);
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
