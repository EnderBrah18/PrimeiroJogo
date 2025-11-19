using UnityEngine;

public class InteractionHandler : MonoBehaviour
{
    private ShopBase currentVendor;

    void OnEnable()
    {
        // Registrar callback para quando o ShopManager aparecer
        ShopManager.OnShopManagerReady += RegisterWhenReady;
    }

    void OnDisable()
    {
        // Remover callback
        ShopManager.OnShopManagerReady -= RegisterWhenReady;
    }

    void Start()
    {
        TryRegister();
    }

    private void TryRegister()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RegisterInteractionHandler(this);
            Debug.Log("InteractionHandler registrado no ShopManager.");
        }
        else
        {
            Debug.LogWarning("ShopManager.Instance é NULL no Start(): aguardando ShopManager aparecer...");
        }
    }

    private void RegisterWhenReady()
    {
        TryRegister();
    }

    public void Interact()
    {
        if (currentVendor != null)
        {
            Debug.Log("Interagindo com vendedor: " + currentVendor.name);
            currentVendor.OpenShop();
        }
        else
        {
            Debug.Log("Interação ignorada: nenhum vendedor na área.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var vendor = other.GetComponent<ShopBase>();

        if (vendor != null)
        {
            currentVendor = vendor;
            Debug.Log($"Player ENTROU na área de interação do vendedor: {vendor.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var vendor = other.GetComponent<ShopBase>();

        if (vendor != null && vendor == currentVendor)
        {
            Debug.Log($"Player SAIU da área do vendedor: {currentVendor.name}");
            currentVendor = null;
        }
    }
}
