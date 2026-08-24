using System.Collections.Generic;
using GamePush;
using GamePushEditor;
using NUnit.Framework;

namespace GamePush.Tests
{
    public sealed class ProductCatalogMapperTests
    {
        private const string SampleCsv =
            "id,tag,yandexId,icon,isSubscription,trialPeriod,period,basePrice,baseCurrency,baseRealPrice,baseRealCurrency,names.en,names.ru,descriptions.en,descriptions.ru,prices.CUSTOM,prices.OK,prices.PARTNER,prices.TELEGRAM,prices.VK,prices.YANDEX,realPrices.RUB,realPrices.USD\n" +
            "2894,skin2,2,https://cdn.eponesh.com/icon.webp,false,3,30,0,,0,,Pomni,Помни,Character skin,Облик для персонажа,5,50,0.5,5,2,50,50,0.5";

        [Test]
        public void ParseOfficialCsvMapsPomniProduct()
        {
            List<FetchProducts> products = GP_ProductCatalogMapper.Parse(SampleCsv, Platform.YANDEX);

            Assert.That(products, Has.Count.EqualTo(1));
            Assert.That(products[0].id, Is.EqualTo(2894));
            Assert.That(products[0].tag, Is.EqualTo("skin2"));
            Assert.That(products[0].name, Is.EqualTo("Помни"));
            Assert.That(products[0].description, Is.EqualTo("Облик для персонажа"));
            Assert.That(products[0].price, Is.EqualTo(50));
            Assert.That(products[0].currency, Is.EqualTo("YAN"));
            Assert.That(products[0].isSubscription, Is.False);
            Assert.That(products[0].period, Is.EqualTo(30));
            Assert.That(products[0].trialPeriod, Is.EqualTo(3));
        }

        [Test]
        public void ParseNestedJsonUsesRussianNameAndRubFallback()
        {
            const string json =
                "[{\"id\":2894,\"tag\":\"skin2\",\"names\":{\"en\":\"Pomni\",\"ru\":\"Помни\"},\"descriptions\":{\"en\":\"Character skin\",\"ru\":\"Облик для персонажа\"},\"realPrices\":{\"RUB\":50},\"prices\":{\"CUSTOM\":5}}]";

            List<FetchProducts> products = GP_ProductCatalogMapper.Parse(json, Platform.NONE);

            Assert.That(products, Has.Count.EqualTo(1));
            Assert.That(products[0].name, Is.EqualTo("Помни"));
            Assert.That(products[0].price, Is.EqualTo(50));
            Assert.That(products[0].currency, Is.EqualTo("RUB"));
        }

        [Test]
        public void ParseSdkFlatJsonKeepsExistingShape()
        {
            const string json =
                "[{\"id\":115,\"tag\":\"VIP\",\"name\":\"VIP Status\",\"description\":\"No ads\",\"price\":199,\"currency\":\"YAN\",\"isSubscription\":true,\"period\":30,\"trialPeriod\":7}]";

            List<FetchProducts> products = GP_ProductCatalogMapper.Parse(json, Platform.YANDEX);

            Assert.That(products, Has.Count.EqualTo(1));
            Assert.That(products[0].tag, Is.EqualTo("VIP"));
            Assert.That(products[0].name, Is.EqualTo("VIP Status"));
            Assert.That(products[0].price, Is.EqualTo(199));
            Assert.That(products[0].isSubscription, Is.True);
        }
    }
}
