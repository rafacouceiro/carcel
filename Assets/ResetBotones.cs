using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleCombinationController : MonoBehaviour
{
    [Header("Botones (0..3) en orden")]
    public PuzzleProximityButton[] buttons = new PuzzleProximityButton[4];

    [Header("Combinación correcta: ON, OFF, ON, ON")]
    public bool[] correct = new bool[4] { true, false, true, true };

    [Header("Puertas a abrir")]
    public CellDoorSlide[] doorsToOpen;

    private static PuzzleCombinationController _instance;
    private bool _solved = false;

    void Awake()
    {
        _instance = this;
        // Asegura que empiezan apagados
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null) buttons[i].ForceOff();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ResetAll();
        }
    }

    public static void NotifyButtonChanged(PuzzleProximityButton _)
    {
        _instance?.CheckCombination();
    }

    private void ResetAll()
    {
        if (_solved) return; // si ya está resuelto, opcionalmente no resetees

        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null) buttons[i].ForceOff();
    }

    private void CheckCombination()
    {
        if (_solved) return;

        for (int i = 0; i < 4; i++)
        {
            if (buttons[i] == null) return; // faltan refs
            if (buttons[i].IsOn != correct[i]) return;
        }

        // ✅ Resuelto
        _solved = true;

        // Abrir puertas
        if (doorsToOpen != null)
        {
            foreach (var d in doorsToOpen)
                if (d != null) d.Open();
        }

        Debug.Log("Puzzle resuelto: puerta abierta.");
    }
}