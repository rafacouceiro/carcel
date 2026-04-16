using System;
using System.IO;
using UnityEngine;

namespace AgenticPrison.Communication {

    // Logger dedicado para el protocolo Contract Net.
    // Se limpia al inicio de cada sesión de juego y escribe en fipa_contracts.log.
    public static class FIPALogger {

        static string _logPath;
        static bool   _ready;

        // Se ejecuta antes de cargar la escena, una vez por sesión de juego
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() {
            string dir = Path.Combine(Application.dataPath, "Logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            _logPath = Path.Combine(dir, "fipa_contracts.log");
            _ready   = true;

            // Limpiar el archivo y escribir cabecera
            File.WriteAllText(_logPath,
                $"=== AgenticPrison FIPA Contracts Log — {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        }

        // Registra un evento de protocolo
        // Formato: [HH:mm:ss.fff] [ConvId8] [AgentId] PERFORMATIVE content
        public static void Log(string agentId, string convId, Performative perf, string content = "") {
            if (!_ready) return;

            string shortId = convId != null && convId.Length >= 8
                ? convId.Substring(0, 8)
                : (convId ?? "--------");

            string line = string.Format("[{0}] [{1}] [{2}] {3} {4}",
                DateTime.Now.ToString("HH:mm:ss.fff"),
                shortId,
                agentId,
                perf.ToString().ToUpper(),
                content);

            try {
                File.AppendAllText(_logPath, line + "\n");
            } catch (Exception e) {
                Debug.LogWarning("[FIPALogger] No se pudo escribir en fipa_contracts.log: " + e.Message);
            }
        }
    }
}
