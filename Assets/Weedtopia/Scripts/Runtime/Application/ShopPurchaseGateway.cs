using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Weedtopia.Domain;

namespace Weedtopia.Application
{
    public enum ShopPurchaseChannel { Test, Store }
    public sealed class ShopListing
    {
        public bool available;
        public string localizedPrice;
    }
    /// <summary>A store adapter returns approval only after receipt validation; opening the payment sheet is not approval.</summary>
    public sealed class ShopPurchaseAuthorization
    {
        public bool approved, cancelled;
        public string transactionId, productId;
        public ShopPurchaseChannel channel;
    }
    public interface IShopPurchaseGateway
    {
        bool Enabled { get; }
        bool IsTestMode { get; }
        ShopListing Listing(ShopProductDefinition product);
        Task<ShopPurchaseAuthorization> PurchaseAsync(string productId, CancellationToken cancellationToken);
    }
    public sealed class TestShopPurchaseGateway : IShopPurchaseGateway
    {
        private readonly Func<IFormatProvider> culture;
        public TestShopPurchaseGateway(Func<IFormatProvider> culture=null){this.culture=culture;}
        public bool Enabled => true;
        public bool IsTestMode => true;
        public static string CatalogPrice(ShopProductDefinition product,IFormatProvider culture=null) =>
            (product.euroCents / 100m).ToString("0.00", culture??CultureInfo.GetCultureInfo("de-DE")) + " €";
        public ShopListing Listing(ShopProductDefinition product) => new ShopListing { available = true, localizedPrice = CatalogPrice(product,culture?.Invoke()) };
        public Task<ShopPurchaseAuthorization> PurchaseAsync(string productId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ShopPurchaseAuthorization { approved = true, productId = productId,
                transactionId = "test_" + Guid.NewGuid().ToString("N"), channel = ShopPurchaseChannel.Test });
        }
    }
    /// <summary>The release default has no payment connection and cannot issue free grants.</summary>
    public sealed class UnavailableShopPurchaseGateway : IShopPurchaseGateway
    {
        private readonly Func<IFormatProvider> culture;
        public UnavailableShopPurchaseGateway(Func<IFormatProvider> culture=null){this.culture=culture;}
        public bool Enabled => false;
        public bool IsTestMode => false;
        public ShopListing Listing(ShopProductDefinition product) => new ShopListing { available = false, localizedPrice = TestShopPurchaseGateway.CatalogPrice(product,culture?.Invoke()) };
        public Task<ShopPurchaseAuthorization> PurchaseAsync(string productId, CancellationToken cancellationToken) =>
            Task.FromResult(new ShopPurchaseAuthorization { productId = productId });
    }
}
