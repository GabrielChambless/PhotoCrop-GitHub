using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [SerializeField] private LevelDataLibrary levelDataLibrary;
    [SerializeField] private GameObject menuControllerPrefab;
    [SerializeField] private GameObject gameStateManagerPrefab;
    [SerializeField] private GameObject levelSelectorControllerPrefab;

    public static bool LoadedGameDataExists;

    private string keyFilePath;
    private string ivFilePath;
    private string saveFilePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (LevelDataLibrary.Instance == null)
            {
                LevelDataLibrary.Instance = levelDataLibrary;
            }

            if (MenuController.Instance == null)
            {
                Instantiate(menuControllerPrefab);
            }

            if (GameStateManager.Instance == null)
            {
                Instantiate(gameStateManagerPrefab);
            }

            if (LevelSelectorController.Instance == null)
            {
                Instantiate(levelSelectorControllerPrefab);
            }

            InitializeSaveSystem();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void SaveGameData()
    {
        GameData gameDataToSave = GameStats.GetDataToSave();

        SaveGameData(gameDataToSave, saveFilePath, keyFilePath, ivFilePath);
    }

    public void LoadGameData()
    {
        GameData loadedGameData = LoadGameData(saveFilePath, keyFilePath, ivFilePath);
        Debug.Log(Application.persistentDataPath);

        // Check if game data was loaded successfully
        if (loadedGameData != null)
        {
            // Use loadedGameData to restore game state
            ApplyLoadedGameData(loadedGameData);

            return;
        }

        // No saved game data found, initialize a new game
        InitializeNewGame();
    }


    // Setup methods
    private void InitializeSaveSystem()
    {
        string baseDirectory = Path.Combine(Application.persistentDataPath, "Data");    // Folder name

        Directory.CreateDirectory(baseDirectory);

        keyFilePath = Path.Combine(baseDirectory, "gameKey.txt");
        ivFilePath = Path.Combine(baseDirectory, "gameIV.txt");
        saveFilePath = Path.Combine(baseDirectory, "saveGame.json");

        InitializeEncryption(keyFilePath, ivFilePath);

        LoadGameData();
    }

    private void ApplyLoadedGameData(GameData loadedGameData)
    {
        Debug.Log("Existing game data was found. Applying loaded data to game state.");
        LoadedGameDataExists = true;

        // Apply loaded GameData properties
        GameStats.ApplyLoadedGameData(loadedGameData);
    }

    private void InitializeNewGame()
    {
        // Initialize a new game state
        Debug.Log("No existing game data was found. Starting a new game state.");

        LoadedGameDataExists = false;
    }


    // Internal methods
    private static string SerializeData(GameData data, string keyFilePath, string ivFilePath)
    {
        string json = JsonUtility.ToJson(data);
        return Encrypt(json, keyFilePath, ivFilePath);
    }

    private static void SaveGameData(GameData data, string filePath, string keyFilePath, string ivFilePath)
    {
        string contentToWrite = SerializeData(data, keyFilePath, ivFilePath);
        File.WriteAllText(filePath, contentToWrite);
        Debug.Log($"Game data saved to '{filePath}'.");
    }

    private static GameData LoadGameData(string filePath, string keyFilePath, string ivFilePath)
    {
        if (File.Exists(filePath))
        {
            string encryptedContent = File.ReadAllText(filePath);
            string json = Decrypt(encryptedContent, keyFilePath, ivFilePath);
            return JsonUtility.FromJson<GameData>(json);
        }
        return null;
    }

    private static void GenerateAndSaveKeyAndIV(string keyFilePath, string ivFilePath)
    {
        using (Aes aesAlg = Aes.Create())
        {
            // Generate key and IV
            aesAlg.GenerateKey();
            aesAlg.GenerateIV();

            // Convert to Base64 strings for easy text storage
            string keyAsBase64 = Convert.ToBase64String(aesAlg.Key);
            string ivAsBase64 = Convert.ToBase64String(aesAlg.IV);

            // Write to files
            File.WriteAllText(keyFilePath, keyAsBase64);
            File.WriteAllText(ivFilePath, ivAsBase64);
        }
    }

    private static void InitializeEncryption(string keyFilePath, string ivFilePath)
    {
        // Check if the key and IV files already exist
        if (!File.Exists(keyFilePath) || !File.Exists(ivFilePath))
        {
            // Generate and save the key and IV if either file doesn't exist
            GenerateAndSaveKeyAndIV(keyFilePath, ivFilePath);
        }
    }

    private static byte[] GetKeyFromFile(string filePath)
    {
        string fileContent = File.ReadAllText(filePath);
        return Convert.FromBase64String(fileContent);
    }

    private static byte[] GetIVFromFile(string filePath)
    {
        string fileContent = File.ReadAllText(filePath);
        return Convert.FromBase64String(fileContent);
    }

    private static string Encrypt(string input, string keyFilePath, string ivFilePath)
    {
        byte[] key = GetKeyFromFile(keyFilePath);
        byte[] iv = GetIVFromFile(ivFilePath);

        using (Aes aesAlg = Aes.Create())
        {
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(key, iv);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(input);
                    }
                }

                byte[] encrypted = memoryStream.ToArray();
                return Convert.ToBase64String(encrypted);
            }
        }
    }

    private static string Decrypt(string input, string keyFilePath, string ivFilePath)
    {
        byte[] fullCipher = Convert.FromBase64String(input);
        byte[] key = GetKeyFromFile(keyFilePath);
        byte[] iv = GetIVFromFile(ivFilePath);

        using (Aes aesAlg = Aes.Create())
        {
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(key, iv);

            using (MemoryStream memoryStream = new MemoryStream(fullCipher))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
