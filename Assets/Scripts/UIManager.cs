using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Money")]
    [SerializeField] private TMP_Text moneyText;

    private void Awake() {
        if (Instance != null) {
            Destroy(this);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void Start() {
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyUI;
    }

    public void UpdateMoneyUI(int amount) {
        moneyText.text = $"${amount}";
    }

    private void OnDisable() {
        MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyUI;
    }
}