using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CombatManager : MonoBehaviour
{
    public enum CombatState
    {
        PlayerCommand,
        SkillSelect,
        ItemSelect,
        TargetSelection,
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
        LupusIra
    }

    public List<BattleUnit> playerUnits = new();
    public List<BattleUnit> enemyUnits = new();

    // Combined turn queue sorted by Speed
    private List<BattleUnit> turnOrder = new();
    private int currentTurnIndex = 0;

    private int currentTargetIndex = -1;

    [Header("Player Skills")]
    public List<SkillData> availableSkills = new();
    private int selectedSkillIndex = 0;

    [Header("Player Items")]
    public List<ItemData> availableItems = new();
    private int selectedItemIndex = 0;

    [Header("Input")]
    public InputActionAsset inputActions;

    private InputActionMap combatMap;
    private InputAction confirm;
    private InputAction back;
    private InputAction left;
    private InputAction right;

    public BattleUIManager uiManager;
    public BattleCameraController cameraController;

    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;
    public GameObject playerBattleUnitPrefab;
    public List<EnemyData> possibleEnemies = new();

    public VictoryUI victoryUI;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip victoryMusic;

    private void Awake()
    {
        // Important: do NOT enable maps here; GameManager owns map enabling.
        if (inputActions == null)
        {
            Debug.LogError("[CombatManager] InputActions not assigned.");
            return;
        }

        combatMap = inputActions.FindActionMap("CombatControls", true);

        confirm = combatMap.FindAction("Confirm", true);
        back    = combatMap.FindAction("Back", true);
        left    = combatMap.FindAction("TargetLeft", true);
        right   = combatMap.FindAction("TargetRight", true);
    }

    private void OnEnable()
    {
        if (confirm != null) confirm.performed += OnConfirm;
        if (back != null)    back.performed    += OnBack;
        if (left != null)    left.performed    += OnLeft;
        if (right != null)   right.performed   += OnRight;
    }

    private void OnDisable()
    {
        if (confirm != null) confirm.performed -= OnConfirm;
        if (back != null)    back.performed    -= OnBack;
        if (left != null)    left.performed    -= OnLeft;
        if (right != null)   right.performed   -= OnRight;
    }

    private void Start()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<BattleUIManager>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<BattleCameraController>();

        // Switch to Combat controls at battle start
        GameManager.Instance.EnableCombatControls();

        SpawnPlayerUnits();
        SpawnEnemyUnits();
        SyncPlayerUI();

        StartCoroutine(StartCombatSequence());
    }

    private IEnumerator StartCombatSequence()
    {
        state = CombatState.Busy;
        cameraController.SetCamera(BattleCameraController.CameraState.BattleStart);

        yield return new WaitForSeconds(2f);

        BuildTurnOrder();
        StartNextTurn();
    }

    // =========================
    // TURN ORDER (Speed Based)
    // =========================
    private void BuildTurnOrder()
    {
        turnOrder.Clear();

        foreach (var p in playerUnits)
            if (p != null && !p.IsDead) turnOrder.Add(p);

        foreach (var e in enemyUnits)
            if (e != null && !e.IsDead) turnOrder.Add(e);

        // Sort DESC by speed — Player wins ties
        turnOrder.Sort((a, b) =>
        {
            int sa = a.isEnemy ? a.enemyData.speed : a.characterData.speed;
            int sb = b.isEnemy ? b.enemyData.speed : b.characterData.speed;

            if (sb == sa)
            {
                if (!a.isEnemy && b.isEnemy) return -1;
                if (a.isEnemy && !b.isEnemy) return 1;
            }

            return sb.CompareTo(sa);
        });

        currentTurnIndex = 0;
    }

    private void StartNextTurn()
    {
        if (AllEnemiesDead())
        {
            StartCoroutine(HandleVictory());
            return;
        }

        if (turnOrder.Count == 0 || currentTurnIndex >= turnOrder.Count)
            BuildTurnOrder();

        BattleUnit current = turnOrder[currentTurnIndex];

        if (current == null || current.IsDead)
        {
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        if (!current.isEnemy)
        {
            EnterPlayerCommand();
        }
        else
        {
            StartCoroutine(EnemyTurn(current));
        }
    }

    private IEnumerator EnemyTurn(BattleUnit enemy)
    {
        state = CombatState.Busy;

        BattleUnit target = playerUnits[0];

        cameraController.SetupEnemyAttackCamera(
            enemySpawnPoints[enemy.spawnIndex],
            playerSpawnPoints[0]
        );

        cameraController.SetCamera(BattleCameraController.CameraState.EnemyAttack);

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
            yield return ai.TakeTurn(target);

        yield return new WaitForSeconds(0.35f);

        currentTurnIndex++;
        StartNextTurn();
    }

    // =========================
    // UI + PLAYER COMMAND
    // =========================
    private void SyncPlayerUI()
    {
        if (playerUnits.Count == 0 || uiManager == null) return;

        var p = playerUnits[0];
        uiManager.UpdateHealth(p.characterData.currentHealth, p.characterData.maxHealth);
        uiManager.UpdateAP(p.characterData.currentAP, p.characterData.maxAP);
        RefreshCommandAvailability();
    }

    private void RefreshCommandAvailability()
    {
        if (playerUnits.Count == 0 || uiManager == null) return;

        BattleUnit player = playerUnits[0];
        bool canUseSkill = false;

        foreach (var skill in availableSkills)
        {
            if (player.CanAffordAP(skill.skillPointCost))
            {
                canUseSkill = true;
                break;
            }
        }

        uiManager.SetCommandInteractable(uiManager.weaponSkillsLabelGroup, canUseSkill);
    }

    private void EnterPlayerCommand()
    {
        state = CombatState.PlayerCommand;
        uiManager.ShowCommandPanel(true);
        selectedCommand = PlayerCommand.Attack;

        RefreshCommandAvailability();
        cameraController.SetCamera(BattleCameraController.CameraState.PlayerCommand);
    }

    // =========================
    // INPUT
    // =========================
    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.PlayerCommand)
        {
            switch (selectedCommand)
            {
                case PlayerCommand.Attack:
                    EnterTargetSelection();
                    return;

                case PlayerCommand.Skill:
                    state = CombatState.SkillSelect;
                    return;

                case PlayerCommand.Item:
                    state = CombatState.ItemSelect;
                    return;

                case PlayerCommand.LupusIra:
                    Debug.Log("Lupus Ira (not wired yet)");
                    return;
            }
        }
        else if (state == CombatState.SkillSelect)
        {
            EnterTargetSelection();
            return;
        }
        else if (state == CombatState.TargetSelection)
        {
            if (selectedCommand == PlayerCommand.Attack)
                ConfirmAttack();
            else if (selectedCommand == PlayerCommand.Skill)
                ConfirmSkill();

            return;
        }
        else if (state == CombatState.Victory)
        {
            // Confirm returns to overworld after victory UI
            ReturnToOverworld();
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

    // =========================
    // TARGETING
    // =========================
    private void EnterTargetSelection()
    {
        if (enemyUnits.Count == 0) return;

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
        if (enemyUnits.Count == 0) return;

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
        if (target == null) return;

        cameraController.UpdateTargetSelectionCamera(
            enemySpawnPoints[target.spawnIndex],
            playerSpawnPoints[0],
            instant
        );
    }

    // =========================
    // PLAYER ACTIONS
    // =========================
    private void ConfirmAttack()
    {
        BattleUnit attacker = playerUnits[0];
        BattleUnit target = enemyUnits[currentTargetIndex];

        if (attacker == null || target == null || target.IsDead)
            return;

        state = CombatState.Busy;
        cameraController.SetCamera(BattleCameraController.CameraState.Attack);

        StartCoroutine(AttackSequence(attacker, target));
    }

    private IEnumerator AttackSequence(BattleUnit attacker, BattleUnit target)
    {
        yield return attacker.PerformAttack(target);

        if (AllEnemiesDead())
        {
            yield return HandleVictory();
            yield break;
        }

        currentTurnIndex++;
        StartNextTurn();
    }

    private void ConfirmSkill()
    {
        BattleUnit user = playerUnits[0];
        BattleUnit target = enemyUnits[currentTargetIndex];
        SkillData skill = availableSkills[selectedSkillIndex];

        if (!user.CanAffordAP(skill.skillPointCost))
        {
            EnterPlayerCommand();
            return;
        }

        user.SpendAP(skill.skillPointCost);
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

        currentTurnIndex++;
        StartNextTurn();
    }

    // =========================
    // VICTORY (RESTORED ORIGINAL FLOW)
    // =========================
    private bool AllEnemiesDead()
    {
        foreach (var e in enemyUnits)
            if (e != null && !e.IsDead) return false;

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

        var player = playerUnits.Count > 0 ? playerUnits[0] : null;

        // Play win animation and wait for it to finish (up to ~4s)
        if (player != null && player.animator != null)
        {
            player.animator.SetTrigger("Win");

            float t = 0f;
            while (t < 4f)
            {
                var info = player.animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("Win") && info.normalizedTime >= 1f)
                    break;

                t += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.25f);

        // Hide battle player model + status panel (matches your original)
        if (player != null)
            player.gameObject.SetActive(false);

        GameObject statusPanel = GameObject.Find("PlayerStatusPanel");
        if (statusPanel != null)
            statusPanel.SetActive(false);

        // Calculate EXP
        int totalExp = 0;
        foreach (var e in enemyUnits)
            if (e != null && e.enemyData != null)
                totalExp += e.enemyData.expReward;

        // NOW show victory UI (after animation finishes)
        if (victoryUI != null)
            victoryUI.Show(totalExp);
    }

    // =========================
    // RETURN TO OVERWORLD (RESTORE POSITION + INPUT)
    // =========================
    private void ReturnToOverworld()
    {
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        string scene = GameManager.Instance.returnSceneName;

        // Load overworld
        yield return SceneManager.LoadSceneAsync(scene);

        // Wait a frame so the Player object exists
        yield return null;

        // Restore position/rotation
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            // If you use CharacterController / NavMeshAgent, disable it briefly so it doesn't snap you back
            var cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var agent = playerObj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            playerObj.transform.SetPositionAndRotation(
                GameManager.Instance.returnPosition,
                GameManager.Instance.returnRotation
            );

            if (agent != null) agent.enabled = true;
            if (cc != null) cc.enabled = true;
        }
        else
        {
            Debug.LogWarning("[CombatManager] Could not find Player with tag 'Player' in overworld scene.");
        }

        // Restore overworld input
        GameManager.Instance.EnablePlayerControls();
    }

    // =========================
    // SPAWNING
    // =========================
    private void SpawnPlayerUnits()
    {
        var party = GameManager.Instance.partyData.currentParty;

        // Force AP reset to 1 when combat starts
        if (party != null && party.Count > 0 && party[0] != null)
            party[0].currentAP = 1;

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
