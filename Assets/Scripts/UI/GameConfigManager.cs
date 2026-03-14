using System;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[System.Serializable]
public class GameConfigData
{
    public float moveSpeed = 2f;
    public float jumpForce = 4f;
    public float dashSpeed = 10f;
    public float dashDuration = 0.1f;
    public float dashCooldown = 1f;
    
    public float maxChargeTime = 5f;
    public float chargeMoveSpeedMultiplier = 0.3f;
    
    public float playerMaxHp = 100f;
    public float blockMoveSpeedMultiplier = 0.3f;
    public float enemyAttackInterval = 10f;
    public float perfectBlockWindow = 1f;
    public float blockFailureKnockback = 2f;
    public float hitKnockback = 4f;
    public float knockbackForce = 2f;
}

public class GameConfigManager : MonoBehaviour
{
    public PlayerMovement playerMovement;
    
    public TMP_InputField moveSpeedInput;
    public TMP_InputField jumpForceInput;
    public TMP_InputField dashSpeedInput;
    public TMP_InputField dashDurationInput;
    public TMP_InputField dashCooldownInput;
    public TMP_InputField maxChargeTimeInput;
    public TMP_InputField chargeMoveSpeedMultiplierInput;
    public TMP_InputField playerMaxHpInput;
    public TMP_InputField blockMoveSpeedMultiplierInput;
    public TMP_InputField enemyAttackIntervalInput;
    public TMP_InputField perfectBlockWindowInput;
    public TMP_InputField blockFailureKnockbackInput;
    public TMP_InputField hitKnockbackInput;
    public TMP_InputField knockbackForceInput;
    
    public UnityEngine.UI.Button applyAndSaveButton;
    
    public GameObject configUI; 
    
    public string configFileName = "GameConfig.json";
    public string editorConfigPath = "UserSettings/GameConfig.json";
    
    public bool disablePlayerInputOnPause = true;
    
    private GameConfigData currentConfig;
    private bool isUIOpen = false;
    private float originalTimeScale = 1f;
    private PlayerInput playerInput;
    
    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        currentConfig = new GameConfigData();
        
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();
    }
    
    private void Start()
    {
        LoadAndApplyConfig();
        SyncUIToConfig();
        
        if (applyAndSaveButton != null)
            applyAndSaveButton.onClick.AddListener(OnApplyAndSaveClicked);
        
        if (configUI != null)
            configUI.SetActive(false);
    }
    
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleUI();
        }
    }
    
    public void ToggleUI()
    {
        if (isUIOpen)
            CloseUI();
        else
            OpenUI();
    }
    
    public void OpenUI()
    {
        if (configUI == null) return;
        
        isUIOpen = true;
        configUI.SetActive(true);
        
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        if (disablePlayerInputOnPause && playerInput != null)
        {
            playerInput.enabled = false;
        }
        
        FocusFirstInputField();
    }
    
    public void CloseUI()
    {
        if (configUI == null) return;
        
        isUIOpen = false;
        configUI.SetActive(false);
        
        Time.timeScale = originalTimeScale;
        
        if (disablePlayerInputOnPause && playerInput != null)
        {
            playerInput.enabled = true;
        }
    }
    
    private void FocusFirstInputField()
    {
        if (moveSpeedInput != null)
        {
            moveSpeedInput.ActivateInputField();
            moveSpeedInput.Select();
        }
    }
    
    private void OnApplyAndSaveClicked()
    {
        if (!SyncFromUI())
        {
            Debug.LogWarning("[Config] Input validation failed.");
            return;
        }
        
        ApplyToPlayerMovement();
        
        if (SaveToFile(currentConfig))
        {
            Debug.Log("[Config] Applied and saved successfully!");
        }
    }
    
    private void LoadAndApplyConfig()
    {
        if (TryLoadFromFile(out GameConfigData loadedConfig))
        {
            currentConfig = loadedConfig;
            ApplyToPlayerMovement();
        }
        else
        {
            SyncFromPlayerMovement();
        }
    }
    
    private void SyncFromPlayerMovement()
    {
        if (playerMovement == null) return;
        
        currentConfig.moveSpeed = playerMovement.moveSpeed;
        currentConfig.jumpForce = playerMovement.jumpForce;
        currentConfig.dashSpeed = playerMovement.dashSpeed;
        currentConfig.dashDuration = playerMovement.dashDuration;
        currentConfig.dashCooldown = playerMovement.dashCooldown;
        currentConfig.maxChargeTime = playerMovement.maxChargeTime;
        currentConfig.chargeMoveSpeedMultiplier = playerMovement.chargeMoveSpeedMultiplier;
        currentConfig.playerMaxHp = playerMovement.playerMaxHp;
        currentConfig.blockMoveSpeedMultiplier = playerMovement.blockMoveSpeedMultiplier;
        currentConfig.enemyAttackInterval = playerMovement.enemyAttackInterval;
        currentConfig.perfectBlockWindow = playerMovement.perfectBlockWindow;
        currentConfig.blockFailureKnockback = playerMovement.blockFailureKnockback;
        currentConfig.hitKnockback = playerMovement.hitKnockback;
        currentConfig.knockbackForce = playerMovement.knockbackForce;
    }
    
    private void ApplyToPlayerMovement()
    {
        if (playerMovement == null) return;
        
        playerMovement.moveSpeed = currentConfig.moveSpeed;
        playerMovement.jumpForce = currentConfig.jumpForce;
        playerMovement.dashSpeed = currentConfig.dashSpeed;
        playerMovement.dashDuration = currentConfig.dashDuration;
        playerMovement.dashCooldown = currentConfig.dashCooldown;
        playerMovement.maxChargeTime = currentConfig.maxChargeTime;
        playerMovement.chargeMoveSpeedMultiplier = currentConfig.chargeMoveSpeedMultiplier;
        playerMovement.playerMaxHp = currentConfig.playerMaxHp;
        playerMovement.blockMoveSpeedMultiplier = currentConfig.blockMoveSpeedMultiplier;
        playerMovement.enemyAttackInterval = currentConfig.enemyAttackInterval;
        playerMovement.perfectBlockWindow = currentConfig.perfectBlockWindow;
        playerMovement.blockFailureKnockback = currentConfig.blockFailureKnockback;
        playerMovement.hitKnockback = currentConfig.hitKnockback;
        playerMovement.knockbackForce = currentConfig.knockbackForce;
        
        if (playerMovement.playerData != null)
        {
            playerMovement.playerData.maxHP = currentConfig.playerMaxHp;
            if (playerMovement.playerData.HP > playerMovement.playerData.maxHP)
                playerMovement.playerData.HP = playerMovement.playerData.maxHP;
        }
    }
    
    private void SyncUIToConfig()
    {
        SetInput(moveSpeedInput, currentConfig.moveSpeed);
        SetInput(jumpForceInput, currentConfig.jumpForce);
        SetInput(dashSpeedInput, currentConfig.dashSpeed);
        SetInput(dashDurationInput, currentConfig.dashDuration);
        SetInput(dashCooldownInput, currentConfig.dashCooldown);
        SetInput(maxChargeTimeInput, currentConfig.maxChargeTime);
        SetInput(chargeMoveSpeedMultiplierInput, currentConfig.chargeMoveSpeedMultiplier);
        SetInput(playerMaxHpInput, currentConfig.playerMaxHp);
        SetInput(blockMoveSpeedMultiplierInput, currentConfig.blockMoveSpeedMultiplier);
        SetInput(enemyAttackIntervalInput, currentConfig.enemyAttackInterval);
        SetInput(perfectBlockWindowInput, currentConfig.perfectBlockWindow);
        SetInput(blockFailureKnockbackInput, currentConfig.blockFailureKnockback);
        SetInput(hitKnockbackInput, currentConfig.hitKnockback);
        SetInput(knockbackForceInput, currentConfig.knockbackForce);
    }
    
    private bool SyncFromUI()
    {
        bool valid = true;
        valid &= TryParseInput(moveSpeedInput, ref currentConfig.moveSpeed, "moveSpeed");
        valid &= TryParseInput(jumpForceInput, ref currentConfig.jumpForce, "jumpForce");
        valid &= TryParseInput(dashSpeedInput, ref currentConfig.dashSpeed, "dashSpeed");
        valid &= TryParseInput(dashDurationInput, ref currentConfig.dashDuration, "dashDuration");
        valid &= TryParseInput(dashCooldownInput, ref currentConfig.dashCooldown, "dashCooldown");
        valid &= TryParseInput(maxChargeTimeInput, ref currentConfig.maxChargeTime, "maxChargeTime");
        valid &= TryParseInput(chargeMoveSpeedMultiplierInput, ref currentConfig.chargeMoveSpeedMultiplier, "chargeMoveSpeedMultiplier");
        valid &= TryParseInput(playerMaxHpInput, ref currentConfig.playerMaxHp, "playerMaxHp");
        valid &= TryParseInput(blockMoveSpeedMultiplierInput, ref currentConfig.blockMoveSpeedMultiplier, "blockMoveSpeedMultiplier");
        valid &= TryParseInput(enemyAttackIntervalInput, ref currentConfig.enemyAttackInterval, "enemyAttackInterval");
        valid &= TryParseInput(perfectBlockWindowInput, ref currentConfig.perfectBlockWindow, "perfectBlockWindow");
        valid &= TryParseInput(blockFailureKnockbackInput, ref currentConfig.blockFailureKnockback, "blockFailureKnockback");
        valid &= TryParseInput(hitKnockbackInput, ref currentConfig.hitKnockback, "hitKnockback");
        valid &= TryParseInput(knockbackForceInput, ref currentConfig.knockbackForce, "knockbackForce");
        return valid;
    }
    
    private string GetConfigFilePath()
    {
        #if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "..", editorConfigPath);
        #else
            return Path.Combine(Application.persistentDataPath, configFileName);
        #endif
    }
    
    private bool SaveToFile(GameConfigData config)
    {
        try
        {
            string json = JsonUtility.ToJson(config, true);
            string filePath = GetConfigFilePath();
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Config] Save failed: {e.Message}");
            return false;
        }
    }
    
    private bool TryLoadFromFile(out GameConfigData config)
    {
        config = null;
        try
        {
            string filePath = GetConfigFilePath();
            if (!File.Exists(filePath)) return false;
            string json = File.ReadAllText(filePath);
            config = JsonUtility.FromJson<GameConfigData>(json);
            return config != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Config] Load failed: {e.Message}");
            return false;
        }
    }
    
    private void SetInput(TMP_InputField input, float value)
    {
        if (input != null) input.text = value.ToString("F2");
    }
    
    private bool TryParseInput(TMP_InputField input, ref float result, string fieldName)
    {
        if (input == null) return false;
        if (float.TryParse(input.text, out float parsedValue))
        {
            if (parsedValue < 0f && fieldName != "knockbackForce")
            {
                input.text = result.ToString("F2");
                input.ActivateInputField();
                return false;
            }
            result = parsedValue;
            return true;
        }
        else
        {
            input.text = result.ToString("F2");
            input.ActivateInputField();
            return false;
        }
    }
    
    private void OnDestroy()
    {
        Time.timeScale = originalTimeScale;
        if (playerInput != null)
            playerInput.enabled = true;
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isUIOpen)
        {
            Time.timeScale = originalTimeScale;
        }
    }
}