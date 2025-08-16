using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class IAPManager : MonoBehaviour, IStoreListener
{
    private static IStoreController storeController;
    private static IExtensionProvider extensionProvider;

    // Product IDs (match IAP Catalog + Store)
    public const string DIAMONDS_PACK1 = "diamonds_pack1";
    public const string DIAMONDS_PACK2 = "diamonds_pack2";
    public const string DIAMONDS_PACK3 = "diamonds_pack3";
    public const string DIAMONDS_PACK4 = "diamonds_pack4";
    public const string DIAMONDS_PACK5 = "diamonds_pack5";
    public const string DIAMONDS_PACK6 = "diamonds_pack6";
    public const string MONTHLY_SUBSCRIPTION = "monthly_subscription";

    void Start()
    {
        if (storeController == null)
            InitializePurchasing();
    }

    public void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Diamond packs (consumables)
        builder.AddProduct(DIAMONDS_PACK1, ProductType.Consumable);
        builder.AddProduct(DIAMONDS_PACK2, ProductType.Consumable);
        builder.AddProduct(DIAMONDS_PACK3, ProductType.Consumable);
        builder.AddProduct(DIAMONDS_PACK4, ProductType.Consumable);
        builder.AddProduct(DIAMONDS_PACK5, ProductType.Consumable);
        builder.AddProduct(DIAMONDS_PACK6, ProductType.Consumable);

        // Monthly subscription (auto-renewing)
        builder.AddProduct(MONTHLY_SUBSCRIPTION, ProductType.Subscription);

        UnityPurchasing.Initialize(this, builder);
    }

    public void BuyProduct(string productId)
    {
        if (storeController != null && storeController.products.WithID(productId) != null)
        {
            storeController.InitiatePurchase(productId);
        }
        else
        {
            Debug.LogError("BuyProduct FAIL. Not initialized or product not found.");
        }
    }

    // Called when purchase is successful
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        switch (args.purchasedProduct.definition.id)
        {
            case DIAMONDS_PACK1: AddDiamonds(100); break;
            case DIAMONDS_PACK2: AddDiamonds(500); break;
            case DIAMONDS_PACK3: AddDiamonds(1200); break;
            case DIAMONDS_PACK4: AddDiamonds(2500); break;
            case DIAMONDS_PACK5: AddDiamonds(6500); break;
            case DIAMONDS_PACK6: AddDiamonds(14000); break;
            case MONTHLY_SUBSCRIPTION:
                UnlockSubscriptionFeatures();
                break;
        }
        return PurchaseProcessingResult.Complete;
    }

    private void AddDiamonds(int amount)
    {
        Debug.Log("Added " + amount + " diamonds!");
        // TODO: Add diamonds to player’s currency (Firebase or local save)
    }

    private void UnlockSubscriptionFeatures()
    {
        Debug.Log("Monthly Subscription activated!");
        // TODO: Mark subscription in player data (Firebase or local flag)
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Debug.Log("Unity IAP initialized successfully.");
        storeController = controller;
        extensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError("Unity IAP init failed: " + error);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError("IAP failed: " + error + " - " + message);
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError("Purchase failed: " + product.definition.id + " - " + failureReason);
    }
}
