using UnityEngine;
using Unity.Cinemachine;

public class BattleCameraController : MonoBehaviour
{
    public enum CameraState
    {
        BattleStart,
        PlayerCommand,
        TargetSelection,
        Attack,
        EnemyAttack,
        Victory
    }

    [Header("Cameras")]
    public CinemachineCamera camBattleStart;
    public CinemachineCamera camPlayerCommand;
    public CinemachineCamera camTargetSelection;
    public CinemachineCamera camAttack;
    public CinemachineCamera camEnemyAttack;
    public CinemachineCamera camVictory; // CM_Win

    [Header("Target Selection Camera Settings")]
    public float targetCamDistance = 6f;
    public float targetCamHeight = 2f;
    public float enemyLookHeight = 1.2f;

    public float cameraMoveSmoothTime = 0.12f;
    public float cameraRotateSpeed = 14f;

    [Header("Enemy Attack Camera Settings")]
    public float enemyAttackCamDistance = 4f;
    public float enemyAttackCamHeight = 2f;

    private Vector3 desiredCamPosition;
    private Vector3 camVelocity;
    private Transform desiredLookTarget;

    private bool isTargetSelectionActive = false;

    private void LateUpdate()
    {
        if (!isTargetSelectionActive || camTargetSelection == null)
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

    // ========================= PUBLIC API =========================
    public void SetCamera(CameraState cam)
    {
        camBattleStart.Priority     = 0;
        camPlayerCommand.Priority   = 0;
        camTargetSelection.Priority = 0;
        camAttack.Priority          = 0;
        camEnemyAttack.Priority     = 0;
        camVictory.Priority         = 0;

        switch (cam)
        {
            case CameraState.BattleStart:     camBattleStart.Priority   = 10; break;
            case CameraState.PlayerCommand:   camPlayerCommand.Priority = 10; break;
            case CameraState.TargetSelection: camTargetSelection.Priority = 10; break;
            case CameraState.Attack:          camAttack.Priority        = 10; break;
            case CameraState.EnemyAttack:     camEnemyAttack.Priority   = 10; break;
            case CameraState.Victory:         camVictory.Priority       = 10; break;
        }

        isTargetSelectionActive = (cam == CameraState.TargetSelection);
    }

    public void UpdateTargetSelectionCamera(Transform enemySpawn, Transform playerSpawn, bool instant)
    {
        if (enemySpawn == null || playerSpawn == null || camTargetSelection == null)
            return;

        Vector3 enemyPos  = enemySpawn.position;
        Vector3 playerPos = playerSpawn.position;

        Vector3 dir = (playerPos - enemyPos);
        dir.y = 0;
        dir.Normalize();

        desiredCamPosition =
            enemyPos +
            dir * targetCamDistance +
            Vector3.up * targetCamHeight;

        desiredLookTarget = enemySpawn;

        if (instant)
        {
            camTargetSelection.transform.position = desiredCamPosition;
            camTargetSelection.transform.LookAt(enemyPos + Vector3.up * enemyLookHeight);
            camVelocity = Vector3.zero;
        }
    }

    public void SetupEnemyAttackCamera(Transform enemySpawn, Transform playerSpawn)
    {
        if (enemySpawn == null || playerSpawn == null || camEnemyAttack == null)
            return;

        Vector3 enemyPos  = enemySpawn.position;
        Vector3 playerPos = playerSpawn.position;

        Vector3 dir = (playerPos - enemyPos);
        dir.y = 0;
        dir.Normalize();

        camEnemyAttack.transform.position =
            enemyPos -
            dir * enemyAttackCamDistance +
            Vector3.up * enemyAttackCamHeight;

        camEnemyAttack.transform.LookAt(playerPos + Vector3.up * enemyLookHeight);
    }
}
