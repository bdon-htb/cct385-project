using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private ScoreVariableSO score;

    void Start()
    {
        score.Reset();
    }

    public void AwardPoints(int points)
    {
        score.Add(points);
        Debug.Log($"Score: {score.Value}");
    }
}
