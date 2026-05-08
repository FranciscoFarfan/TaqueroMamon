using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// LeaderboardManager — Maneja el top 10 de puntajes persistente.
///
/// Guarda y lee un archivo JSON en Application.persistentDataPath.
/// Provee métodos para verificar si un score califica, insertar,
/// y obtener la lista formateada.
///
/// Uso:
///   LeaderboardManager.Instance.IsTopTen(score)
///   LeaderboardManager.Instance.AddScore("ABC", 150)
///   LeaderboardManager.Instance.GetFormattedLeaderboard()
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═══════════════════════════════════════════════════════════════════════════

    public static LeaderboardManager Instance { get; private set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CONFIGURACIÓN
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("Configuración")]
    [Tooltip("Cantidad máxima de entradas en el leaderboard.")]
    [SerializeField] private int maxEntries = 10;

    [Tooltip("Nombre del archivo JSON donde se guardan los puntajes.")]
    [SerializeField] private string fileName = "leaderboard.json";

    // ═══════════════════════════════════════════════════════════════════════════
    //  DATOS
    // ═══════════════════════════════════════════════════════════════════════════

    [Serializable]
    public class ScoreEntry
    {
        public string playerName;
        public int score;
        public string date;

        public ScoreEntry(string name, int score)
        {
            playerName = name;
            this.score = score;
            date = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }

    [Serializable]
    private class LeaderboardData
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    private LeaderboardData _data;
    private string _filePath;

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═══════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _filePath = Path.Combine(Application.persistentDataPath, fileName);
        LoadLeaderboard();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica si un puntaje califica para entrar al top 10.
    /// Retorna true si hay menos de maxEntries O si el score es mayor
    /// que el último lugar.
    /// </summary>
    public bool IsTopTen(int score)
    {
        if (_data.entries.Count < maxEntries) return true;
        return score > _data.entries[_data.entries.Count - 1].score;
    }

    /// <summary>
    /// Agrega un nuevo puntaje al leaderboard, lo ordena de mayor a menor,
    /// recorta a maxEntries, y lo guarda en disco.
    /// </summary>
    public void AddScore(string playerName, int score)
    {
        // Sanitizar nombre a 3 caracteres
        playerName = playerName.ToUpper().Trim();
        if (playerName.Length > 3) playerName = playerName.Substring(0, 3);
        while (playerName.Length < 3) playerName += "X";

        ScoreEntry entry = new ScoreEntry(playerName, score);
        _data.entries.Add(entry);

        // Ordenar de mayor a menor
        _data.entries.Sort((a, b) => b.score.CompareTo(a.score));

        // Recortar a maxEntries
        if (_data.entries.Count > maxEntries)
            _data.entries.RemoveRange(maxEntries, _data.entries.Count - maxEntries);

        SaveLeaderboard();
        Debug.Log($"[LeaderboardManager] Score guardado: {playerName} — ${score}");
    }

    /// <summary>Retorna la lista ordenada de puntajes.</summary>
    public List<ScoreEntry> GetTopScores()
    {
        return new List<ScoreEntry>(_data.entries);
    }

    /// <summary>
    /// Retorna un string formateado para mostrar en un TMP_Text.
    /// Formato:
    ///   1. ABC  $150
    ///   2. DEF  $120
    ///   ...
    /// Si no hay entradas, retorna un mensaje indicándolo.
    /// </summary>
    public string GetFormattedLeaderboard()
    {
        if (_data.entries.Count == 0)
            return "No hay puntajes aún.\n¡Sé el primero!";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < _data.entries.Count; i++)
        {
            ScoreEntry e = _data.entries[i];
            sb.AppendLine($"{i + 1,2}. {e.playerName}  ${e.score}");
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Retorna cuántas entradas hay actualmente.</summary>
    public int EntryCount => _data.entries.Count;

    // ═══════════════════════════════════════════════════════════════════════════
    //  PERSISTENCIA
    // ═══════════════════════════════════════════════════════════════════════════

    private void LoadLeaderboard()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                _data = JsonUtility.FromJson<LeaderboardData>(json);
                if (_data == null) _data = new LeaderboardData();
                Debug.Log($"[LeaderboardManager] Leaderboard cargado: {_data.entries.Count} entradas");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LeaderboardManager] Error al leer leaderboard: {e.Message}");
                _data = new LeaderboardData();
            }
        }
        else
        {
            _data = new LeaderboardData();
            Debug.Log("[LeaderboardManager] No se encontró archivo de leaderboard, creando nuevo.");
        }
    }

    private void SaveLeaderboard()
    {
        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(_filePath, json);
            Debug.Log($"[LeaderboardManager] Leaderboard guardado en: {_filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardManager] Error al guardar leaderboard: {e.Message}");
        }
    }
}
