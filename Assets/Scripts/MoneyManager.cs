using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    private int money = 0;
    [SerializeField] private int startMoney = 15;

    public delegate void MoneyChangedHandler(int amount);
    public event MoneyChangedHandler OnMoneyChanged;

    private void Awake() {
        if(Instance != null) {
            Destroy(this);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void Start() {
        money = startMoney;
        OnMoneyChanged?.Invoke(money);
    }

    public void AddMoney(int amount) {
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }
}