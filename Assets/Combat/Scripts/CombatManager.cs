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

    [Header("UI")]
    public BattleUIManager uiManager;
    public VictoryUI victoryUI;
    public GameObject playerStatusPanel; 

    [Header("Camera")]
    public BattleCameraController cameraController;

    [Header("Spawning")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;
    public GameObject playerBattleUnitPrefab;
    public List<EnemyData> possibleEnemies = new();

    [Header("Optional Root To Hide On Victory")]
    public GameObject battleUnitRoot;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip victoryMusic;

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
        if (confirm != null) confirm.performed -= OnConfirm;
        if (back != null) back.performed -= OnBack;
        if (left != null) left.performed -= OnLeft;
        if (right != null) right.performed -= OnRight;

        if (combatMap != null)
            combatMap.Disable();
    }

    private void Start()
    {
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

    private void BuildTurnOrder()
    {
        turnOrder.Clear();

        foreach (var p in playerUnits)
            if (p != null && !p.IsDead) turnOrder.Add(p);

        foreach (var e in enemyUnits)
            if (e != null && !e.IsDead) turnOrder.Add(e);

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

        if (currentTurnIndex >= turnOrder.Count)
            BuildTurnOrder();

        BattleUnit current = turnOrder[currentTurnIndex];

        if (current == null || current.IsDead)
        {
            currentTurnIndex++;
            StartNextTurn();
            return;
        }

        if (!current.isEnemy)
            EnterPlayerCommand();
        else
            StartCoroutine(EnemyTurn(current));
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

        yield return enemy.GetComponent<EnemyAI>()?.TakeTurn(target);

        yield return new WaitForSeconds(0.35f);

        currentTurnIndex++;
        StartNextTurn();
    }

    private void SyncPlayerUI()
    {
        if (playerUnits.Count == 0 || playerUnits[0] == null)
            return;

        BattleUnit p = playerUnits[0];
        uiManager.UpdateHealth(p.currentHealth, p.characterData.maxHealth);
        uiManager.UpdateAP(p.currentAP, p.characterData.maxAP);
    }

    private void EnterPlayerCommand()
    {
        state = CombatState.PlayerCommand;
        selectedCommand = PlayerCommand.Attack;

        uiManager.ShowCommandPanel(true);
        cameraController.SetCamera(BattleCameraController.CameraState.PlayerCommand);
    }

    private void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.Victory)
        {
            Debug.Log("Confirm pressed on Victory");
            return;
        }

        if (state == CombatState.PlayerCommand)
        {
            EnterTargetSelection();
            return;
        }

        if (state == CombatState.TargetSelection)
        {
            ConfirmTarget();
            return;
        }

        if (state == CombatState.SkillSelect)
        {
            EnterTargetSelection();
            return;
        }

        if (state == CombatState.ItemSelect)
        {
            EnterTargetSelection();
            return;
        }
    }

    private void OnBack(InputAction.CallbackContext ctx)
    {
        if (state == CombatState.TargetSelection || state == CombatState.SkillSelect || state == CombatState.ItemSelect)
        {
            EnterPlayerCommand();
        }
    }

    private void OnLeft(InputAction.CallbackContext ctx)
    {
        if (state != CombatState.TargetSelection)
            return;

        CycleTarget(-1);
    }

    private void OnRight(InputAction.CallbackContext ctx)
    {
        if (state != CombatState.TargetSelection)
            return;

        CycleTarget(+1);
    }

    private void EnterTargetSelection()
    {
        state = CombatState.TargetSelection;

        uiManager.ShowCommandPanel(false);

        currentTargetIndex = GetNextAliveEnemyIndex(0, +1);

        cameraController.SetCamera(BattleCameraController.CameraState.TargetSelection);

        if (currentTargetIndex >= 0 && currentTargetIndex < enemyUnits.Count)
        {
            cameraController.UpdateTargetSelectionCamera(
                enemySpawnPoints[currentTargetIndex],
                playerSpawnPoints[0],
                true
            );
        }
    }

    private void CycleTarget(int dir)
    {
        if (enemyUnits.Count == 0)
            return;

        int start = currentTargetIndex;
        if (start < 0) start = 0;

        int idx = start;

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            idx = (idx + dir + enemyUnits.Count) % enemyUnits.Count;

            if (enemyUnits[idx] != null && !enemyUnits[idx].IsDead)
            {
                currentTargetIndex = idx;
                cameraController.UpdateTargetSelectionCamera(
                    enemySpawnPoints[currentTargetIndex],
                    playerSpawnPoints[0],
                    false
                );
                return;
            }
        }
    }

    private void ConfirmTarget()
    {
        if (currentTargetIndex < 0 || currentTargetIndex >= enemyUnits.Count)
            return;

        BattleUnit target = enemyUnits[currentTargetIndex];
        if (target == null || target.IsDead)
            return;

        StartCoroutine(PlayerAttack(target));
    }

    private int GetNextAliveEnemyIndex(int startIndex, int dir)
    {
        if (enemyUnits.Count == 0)
            return -1;

        int idx = Mathf.Clamp(startIndex, 0, enemyUnits.Count - 1);

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            int test = (idx + i * dir + enemyUnits.Count) % enemyUnits.Count;

            if (enemyUnits[test] != null && !enemyUnits[test].IsDead)
                return test;
        }

        return -1;
    }

    private IEnumerator PlayerAttack(BattleUnit target)
    {
        state = CombatState.Busy;

        cameraController.SetCamera(BattleCameraController.CameraState.Attack);

        yield return playerUnits[0].PerformAttack(target);

        SyncPlayerUI();

        yield return new WaitForSeconds(0.25f);

        currentTurnIndex++;
        StartNextTurn();
    }

    private bool AllEnemiesDead()
    {
        foreach (var e in enemyUnits)
            if (e != null && !e.IsDead) return false;
        return true;
    }

    
    private IEnumerator HandleVictory()
    {
        state = CombatState.Victory;

        uiManager.ShowCommandPanel(false);
        cameraController.SetCamera(BattleCameraController.CameraState.Victory);

        if (musicSource && victoryMusic)
        {
            musicSource.Stop();
            musicSource.clip = victoryMusic;
            musicSource.Play();
        }

        int totalExp = CalculateTotalExp();

        BattleUnit player = playerUnits.Count > 0 ? playerUnits[0] : null;

        if (player != null && player.animator != null)
        {
            Animator anim = player.animator;

            anim.ResetTrigger("Win");
            anim.SetTrigger("Win");

            
            yield return StartCoroutine(WaitForWinProgress(anim, 0, 0.90f));

            
            yield return new WaitForSeconds(1.0f);
        }

        if (battleUnitRoot != null)
            battleUnitRoot.SetActive(false);
        else
        {
            if (player != null) player.gameObject.SetActive(false);
            foreach (var e in enemyUnits)
                if (e != null) e.gameObject.SetActive(false);
        }

        if (playerStatusPanel != null)
            playerStatusPanel.SetActive(false);

        if (victoryUI != null)
            victoryUI.Show(totalExp);
        else
            Debug.LogError("[CombatManager] VictoryUI reference is NULL!");
    }

    private int CalculateTotalExp()
    {
        int exp = 0;

        foreach (var e in enemyUnits)
        {
            if (e == null || e.enemyData == null)
                continue;

            exp += e.enemyData.expReward;
        }

        return exp;
    }

    private IEnumerator WaitForWinProgress(Animator anim, int layer, float normalizedThreshold)
    {
        float timeout = 3f;
        float t = 0f;

        yield return null;

        while (t < timeout)
        {
            var info = anim.GetCurrentAnimatorStateInfo(layer);

            if (!anim.IsInTransition(layer) && info.normalizedTime >= normalizedThreshold)
                yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }

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
