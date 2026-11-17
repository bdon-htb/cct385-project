using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ScoreVariableSO", menuName = "Scriptable Objects/ScoreVariableSO")]
public class ScoreVariableSO : ScriptableObject
{
    [SerializeField] private int value;

    public int Value => value;

    public event Action<int> OnValueChanged;
    public event Action<int> OnScoreChanged;
    public event Action OnReset;

    public void Add(int amount)
    {
        if (amount == 0)
            return;

        value += amount;

        OnValueChanged?.Invoke(value);
        OnScoreChanged?.Invoke(amount);
    }

    public void Reset()
    {
        value = 0;
        OnValueChanged?.Invoke(value);
        OnReset?.Invoke();
    }
}
