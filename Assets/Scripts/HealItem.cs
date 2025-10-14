using UnityEngine;

public class HealItem : MonoBehaviour
{
    [SerializeField] private GameObject healPickUpEffect;

    private void OnDestroy()
    {
        Instantiate(healPickUpEffect, transform.position, Quaternion.identity);
    }
}
