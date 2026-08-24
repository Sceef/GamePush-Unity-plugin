using System.Collections.Generic;
using GamePush.Utilities;
using NUnit.Framework;

namespace GamePush.Tests
{
    public sealed class PaymentsCacheTests
    {
        [Test]
        public void ProductAndPurchaseListsParseFromJavascriptSdkJson()
        {
            List<FetchProducts> products = UtilityJSON.GetList<FetchProducts>(
                "[{\"id\":115,\"tag\":\"VIP\",\"name\":\"VIP Status\",\"description\":\"No ads\",\"icon\":\"https://cdn.example/icon.png\",\"iconSmall\":\"https://cdn.example/icon-small.png\",\"price\":199,\"currency\":\"YAN\",\"currencySymbol\":\"YAN\",\"isSubscription\":true,\"period\":30,\"trialPeriod\":7}]");
            Assert.That(products, Has.Count.EqualTo(1));
            Assert.That(products[0].id, Is.EqualTo(115));
            Assert.That(products[0].tag, Is.EqualTo("VIP"));
            Assert.That(products[0].isSubscription, Is.True);
            Assert.That(products[0].period, Is.EqualTo(30));

            List<FetchPlayerPurchases> purchases = UtilityJSON.GetList<FetchPlayerPurchases>(
                "[{\"tag\":\"VIP\",\"productId\":115,\"payload\":\"{\\\"order_id\\\":213}\",\"createdAt\":\"2022-12-01T04:52:26+0000\",\"expiredAt\":\"2023-01-01T04:52:26+0000\",\"gift\":false,\"subscribed\":true}]");
            Assert.That(purchases, Has.Count.EqualTo(1));
            Assert.That(purchases[0].productId, Is.EqualTo(115));
            Assert.That(purchases[0].tag, Is.EqualTo("VIP"));
            Assert.That(purchases[0].subscribed, Is.True);
        }

        [Test]
        public void GetProductsAndPurchasesInEditorReturnListsWithoutFetch()
        {
            List<FetchProducts> products = GP_Payments.GetProducts();
            List<FetchPlayerPurchases> purchases = GP_Payments.GetPurchases();

            Assert.That(products, Is.Not.Null);
            Assert.That(purchases, Is.Not.Null);
            Assert.That(GP_Payments.Products, Is.SameAs(products));
            Assert.That(GP_Payments.Purchases, Is.SameAs(purchases));
            Assert.That(GP_Payments.Has("missing-product"), Is.False);
        }
    }
}
