using UnityEngine;

public class ArcheryAnimationEvents : MonoBehaviour
{
    [SerializeField] private PlayerArchery playerArchery;

    public void OnDrawComplete()
    {
        playerArchery.OnDrawComplete();
    }

    public void OnRecoilComplete()
    {
        playerArchery.OnRecoilComplete();
    }
}