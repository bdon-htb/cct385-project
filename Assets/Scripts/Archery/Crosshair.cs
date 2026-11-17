using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Crosshair : MonoBehaviour
{
    [SerializeField] private PlayerArchery playerArchery;
    [SerializeField] private PlayerCamera playerCamera;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Start()
    {
        image.enabled = playerCamera.IsFirstPerson && playerArchery.IsHoldingBow();
    }

    private void OnEnable()
    {
        playerArchery.OnStateChanged += UpdateCrosshair;
        playerCamera.OnPerspectiveChanged += UpdateCrosshair;
    }

    private void OnDisable()
    {
        playerArchery.OnStateChanged -= UpdateCrosshair;
        playerCamera.OnPerspectiveChanged -= UpdateCrosshair;
    }

    private void UpdateCrosshair(PlayerArchery.ArcheryState state)
    {
        image.enabled = playerCamera.IsFirstPerson && playerArchery.IsHoldingBow();
    }

    private void UpdateCrosshair(bool isFirstPerson)
    {
        image.enabled = isFirstPerson && playerArchery.IsHoldingBow();
    }
}
