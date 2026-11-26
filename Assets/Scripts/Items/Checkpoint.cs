using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField, Range(1, 100)] private int checkpointNo;
    [SerializeField] private bool isFinishLine;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        // Notify the RaceUIManager that a player has hit a checkpoint
        RaceManager.Instance?.PlayerHitCheckpoint(other.name, isFinishLine ? 0 : checkpointNo);
    }
}
