using System;
using UnityEngine;

public class ArcheryTarget : MonoBehaviour
{
    [Serializable]
    private struct ScoringRing
    {
        [Min(0)] public float radius;
        [Min(0)] public int points;
    }

    [Serializable]
    private struct DistanceBonus
    {
        [Min(0)] public float minDistance;
        [Min(0)] public float multiplier;
    }

    private static readonly Color[] ringColors =
    {
        Color.red,
        Color.yellow,
        Color.green,
        Color.cyan,
        Color.magenta
    };

    [SerializeField] ScoreVariableSO score;
    [SerializeField] Transform bullseye;
    [SerializeField] private ScoringRing[] scoringRings;
    [SerializeField] private DistanceBonus[] distanceBonuses;

    private void OnDrawGizmosSelected()
    {
        if (bullseye == null || scoringRings == null)
            return;

        for (int i = 0; i < scoringRings.Length; i++)
        {
            Gizmos.color = ringColors[i % ringColors.Length];
            DrawRing(scoringRings[i].radius);
        }
    }

    private void DrawRing(float radius)
    {
        const int segments = 64;

        Vector3 previousPoint = bullseye.TransformPoint(
            new Vector3(radius, 0f, 0f)
        );

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 localPoint = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            Vector3 currentPoint = bullseye.TransformPoint(localPoint);

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }

    private float GetDistanceMultiplier(float distance)
    {
        float multiplier = 1f;

        foreach (DistanceBonus bonus in distanceBonuses)
        {
            if (distance >= bonus.minDistance)
                multiplier = bonus.multiplier;
        }

        return multiplier;
    }

    private int CalculateScore(Vector3 collisionPoint)
    {
        Vector3 localHit = bullseye.InverseTransformPoint(collisionPoint);

        float distance = new Vector2(localHit.x, localHit.y).magnitude;

        foreach (ScoringRing ring in scoringRings)
        {
            if (distance <= ring.radius)
                return ring.points;
        }

        return 0;
    }

    public void OnArrowHit(Vector3 collisionPoint, Vector3 firedFrom)
    {
        int baseScore = CalculateScore(collisionPoint);

        float distance = Vector3.Distance(firedFrom, collisionPoint);
        float multiplier = GetDistanceMultiplier(distance);

        int finalScore = Mathf.RoundToInt(baseScore * multiplier);

        score.Add(finalScore);
    }

}
