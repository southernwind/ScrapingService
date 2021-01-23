using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

using ScrapingLibrary;

using ScrapingService.Composition;

namespace ScrapingService.Targets {
	public abstract class YahooFinanceBase : IScrapingServiceTarget {
		private readonly HttpClientWrapper _httpClient;

		public YahooFinanceBase() {
			this._httpClient = new HttpClientWrapper();
		}

		protected async Task<List<YahooFinanceRecord>> GetRecords(string key) {
			var unixTime = (int)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			var days30 = 120 * 24 * 60 * 60;
			var url = $"https://query1.finance.yahoo.com/v7/finance/download/{key}?period1={unixTime - days30}&period2={unixTime}&interval=1d&events=history&includeAdjustedClose=true";
			return await this._httpClient.GetAsync(url).ToCsvRecordAsync<YahooFinanceRecord>(new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = true });
		}


		protected virtual void Dispose(bool disposing) {
		}

		void IDisposable.Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public abstract Task ExecuteAsync(int investmentProductId, string key);
	}

	public class YahooFinanceRecord {
		public DateTime Date {
			get;
			set;
		}
		public double Open {
			get;
			set;
		}
		public double High {
			get;
			set;
		}
		public double Low {
			get;
			set;
		}
		public double Close {
			get;
			set;
		}
		[Name("Adj Close")]
		public double AdjClose {
			get;
			set;
		}
		public double Volume {
			get;
			set;
		}

	}
}
