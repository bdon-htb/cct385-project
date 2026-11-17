using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private ScoreVariableSO score;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text scoreChangeText;
    [SerializeField, Min(1)] private int maxDigits = 6;
    [SerializeField, Min(0)] private float changeDisplayDuration = 2f;

    private int accumulatedChange;
    private Coroutine changeCoroutine;

    private void OnEnable()
    {
        score.OnValueChanged += UpdateScore;
        score.OnScoreChanged += ShowScoreChange;
        score.OnReset += ClearScoreChange;

        UpdateScore(score.Value);
        ClearScoreChange();
    }

    private void OnDisable()
    {
        score.OnValueChanged -= UpdateScore;
        score.OnScoreChanged -= ShowScoreChange;
        score.OnReset -= ClearScoreChange;

        ClearScoreChange();
    }

    private void UpdateScore(int value)
    {
        scoreText.text = value.ToString($"D{maxDigits}");
    }

    private void ShowScoreChange(int amount)
    {
        accumulatedChange += amount;

        if (changeCoroutine != null)
            StopCoroutine(changeCoroutine);

        changeCoroutine = StartCoroutine(DisplayScoreChange());
    }

    private IEnumerator DisplayScoreChange()
    {
        scoreChangeText.text = $"{accumulatedChange:+0;-0;0}";
        scoreChangeText.gameObject.SetActive(true);

        yield return new WaitForSeconds(changeDisplayDuration);

        scoreChangeText.gameObject.SetActive(false);
        accumulatedChange = 0;
        changeCoroutine = null;
    }

    private void ClearScoreChange()
    {
        if (changeCoroutine != null)
        {
            StopCoroutine(changeCoroutine);
            changeCoroutine = null;
        }

        accumulatedChange = 0;
        scoreChangeText.gameObject.SetActive(false);
    }
}