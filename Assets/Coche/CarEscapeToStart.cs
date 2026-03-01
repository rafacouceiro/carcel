using UnityEngine;
using AgenticPrison.Physical;

public class EndGameOnCarIfEscapeReady : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!EscapeState.CanEscape)
            return;

        Debug.Log("PARTIDA TERMINADA");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}