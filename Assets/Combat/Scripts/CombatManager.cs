using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CombatManager : MonoBehaviour
{
    public enum CombatState
    {
        PlayerCommand,
        SkillSelect,
        ItemSelect,
        TargetSelection,
        EnemyTurn,
        Busy,
        Victory
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

    // ========================= INPUT =========================
    [Header("Input")]
    public InputActionAsset inputActions;

    private InputActionMap combatMap;
    private InputAction confirm;
    private InputAction back;
    private InputAction left;
    private InputAction right;

    // ========================= UI =========================
    public BattleUIManager uiManager;

    // ========================= CAMERA =========================
    public BattleCameraController cameraController;

    // ========================= SPAWNS =========================
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;
    public GameObject playerBattleUnitPrefab;
    public List<EnemyData> possibleEnemies = new();

    // ========================= VICTORY (NEW) =========================
    public VictoryUI victoryUI;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip victoryMusic;

    // ========================= ENABLE INPUT =========================
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

        if (cameraController == null)
            cameraController = FindFirstObjectByType<BattleCameraController>();

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

    // ========================= COMBAT FLOW =========================
    private IEnumerator StartCombatSequence()
    {
        state = CombatState.Busy;
        cameraController.SetCamera(BattleCameraController.CameraState.BattleStart);

        yield return new WaitForSeconds(2f);

        EnterPlayerCommand();
    }

    private void EnterPlayerCommand()
    {
        state = CombatState.PlayerCommand;

        uiManager.ShowCommandPanel(true);
        selectedCommand = PlayerCommand.Attack;

        RefreshCommandAvailability();

        cameraController.SetCamera(BattleCameraController.CameraState.PlayerCommand);
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

        cameraController.SetCamera(BattleCameraController.CameraState.TargetSelection);
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

        cameraController.UpdateTargetSelectionCamera(
            enemySpawnPoints[target.spawnIndex],
            playerSpawnPoints[0],
            instant
        );
    }

    // ========================= ATTACK =========================
    private void ConfirmAttack()
    {
        BattleUnit attacker = playerUnits[0];
        BattleUnit target   = enemyUnits[currentTargetIndex];

        if (attacker == null || target == null || target.IsDead)
            return;

        state = CombatState.Busy;

        cameraController.SetCamera(BattleCameraController.CameraState.Attack);
        StartCoroutine(AttackSequence(attacker, target));
    }

    private IEnumerator AttackSequence(BattleUnit attacker, BattleUnit target)
    {
        yield return attacker.PerformAttack(target);
        yield return new WaitForSeconds(0.25f);

        if (AllEnemiesDead())
        {
            yield return HandleVictory();
            yield break;
        }

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
        cameraController.SetCamera(BattleCameraController.CameraState.Attack);

        StartCoroutine(SkillSequence(user, target));
    }

    private IEnumerator SkillSequence(BattleUnit user, BattleUnit target)
    {
        yield return user.PerformSkill(target);

        if (AllEnemiesDead())
        {
            yield return HandleVictory();
            yield break;
        }

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

            cameraController.SetupEnemyAttackCamera(
                enemySpawnPoints[enemy.spawnIndex],
                playerSpawnPoints[0]
            );

            cameraController.SetCamera(
                BattleCameraController.CameraState.EnemyAttack
            );

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                yield return ai.TakeTurn(target);

            yield return new WaitForSeconds(0.4f);
        }

        if (AllEnemiesDead())
        {
            yield return HandleVictory();
            yield break;
        }

        EnterPlayerCommand();
    }

    // ========================= VICTORY LOGIC =========================
    private bool AllEnemiesDead()
    {
        foreach (var e in enemyUnits)
        {
            if (e != null && !e.IsDead)
                return false;
        }
        return true;
    }

    private IEnumerator HandleVictory()
    {
        state = CombatState.Victory;

        cameraController.SetCamera(BattleCameraController.CameraState.Victory);

        if (musicSource != null && victoryMusic != null)
        {
            musicSource.Stop();
            musicSource.clip = victoryMusic;
            musicSource.Play();
        }

        BattleUnit player = playerUnits[0];
        if (player != null && player.animator != null)
            player.animator.SetTrigger("Win");

        // wait until Win animation completes (with timeout)
        float timeout = 4f;
        float t = 0f;

        while (t < timeout)
        {
            var info = player.animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Win") && info.normalizedTime >= 1f)
                break;

            t += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.25f);

        // ⭐ NEW — Disable player BattleUnit AFTER Win animation
        if (player != null)
            player.gameObject.SetActive(false);

        int totalExp = 0;
        foreach (var e in enemyUnits)
        {
            if (e != null && e.enemyData != null)
                totalExp += e.enemyData.expReward;
        }

        victoryUI.Show(totalExp);
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
