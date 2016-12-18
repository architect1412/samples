using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace ExchangeRateUpdater
{
    public class ExchangeRateProvider
    {
		private NumberFormatInfo _numberFormat = new NumberFormatInfo();

		/// <summary>
		/// Should return exchange rates among the specified currencies that are defined by the source. But only those defined
		/// by the source, do not return calculated exchange rates. E.g. if the source contains "EUR/USD" but not "USD/EUR",
		/// do not return exchange rate "USD/EUR" with value calculated as 1 / "EUR/USD". If the source does not provide
		/// some of the currencies, ignore them.
		/// </summary>
		public IEnumerable<ExchangeRate> GetExchangeRates(IEnumerable<Currency> currencies)
        {
			IList<ExchangeRate> result = new List<ExchangeRate>();
			Currency HungarianForint = new Currency("HUF");

			hu.mnb.www.MNBArfolyamServiceSoapImpl proxy = new hu.mnb.www.MNBArfolyamServiceSoapImpl();
			hu.mnb.www.GetCurrentExchangeRatesRequestBody request = new hu.mnb.www.GetCurrentExchangeRatesRequestBody();
			hu.mnb.www.GetCurrentExchangeRatesResponseBody response = proxy.GetCurrentExchangeRates(request);
			string xml = response.GetCurrentExchangeRatesResult;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xml);
			if(doc.DocumentElement.ChildNodes.Count > 0)
			{
				XmlNode container = doc.DocumentElement.ChildNodes[0];
				foreach(XmlNode child in container.ChildNodes)
				{
					string currencyCode = child.Attributes["curr"].Value;
					Currency sourceCurrency = FindCurrency(currencies, currencyCode);
					if(sourceCurrency != null)
					{
						int unit;
						int.TryParse(child.Attributes["unit"].Value, out unit);
						decimal value;
						TryParseDecimal(child.InnerText, out value);
						if(unit > 0)
							value /= unit;
						ExchangeRate rate = new ExchangeRate(sourceCurrency, HungarianForint, value);
						result.Add(rate);
					}
				}
			}
            return result;
        }

		private Currency FindCurrency(IEnumerable<Currency> currencies, string currencyCode)
		{
			foreach(Currency currency in currencies)
			{
				if(currency.Code == currencyCode)
					return currency;
			}
			return null;
		}

		private bool TryParseDecimal(string s, out decimal result)
		{
			NumberStyles style = NumberStyles.AllowDecimalPoint;
			_numberFormat.NumberDecimalSeparator = ",";
			if(decimal.TryParse(s, style, _numberFormat, out result))
				return true;
			_numberFormat.NumberDecimalSeparator = ".";
			if(decimal.TryParse(s, style, _numberFormat, out result))
				return true;
			return false;
		}
	}
}
