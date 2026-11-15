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

    public void OnBowStringAttach()
    {
        playerArchery.OnBowStringAttach();    
    }

    public void OnBowStringRelease()
    {
        playerArchery.OnBowStringRelease();
    }
}