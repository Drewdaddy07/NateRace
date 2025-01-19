using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableCoin : Collectable
{
    [SerializeField] private int value = 1;

    public override void Collect() {
        MoneyManager.Instance.AddMoney(value);
        base.Collect();
    }
}