using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

using Database.Tables;

using DataBase;

using Microsoft.EntityFrameworkCore;

using ScrapingLibrary;

using ScrapingService.Composition;

namespace ScrapingService.Targets {
	public class YahooFinance : IScrapingServiceTarget {
		private readonly HttpClientWrapper _httpClient;
		private readonly HomeServerDbContext _dbContext;

		public YahooFinance(HomeServerDbContext dbContext) {
			this._httpClient = new HttpClientWrapper();
			this._dbContext = dbContext;
		}

		public async Task ExecuteAsync(int investmentProductId, string key) {
			var unixTime = (int)DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			var days30 = 30 * 24 * 60 * 60;
			var url = $"https://query1.finance.yahoo.com/v7/finance/download/{key}?period1={unixTime - days30}&period2={unixTime}&interval=1d&events=history&includeAdjustedClose=true";
			var csv = await this._httpClient.GetAsync(url).ToCsvRecordAsync<YahooFinanceRecord>(new CsvConfiguration(CultureInfo.CurrentCulture) { HasHeaderRecord = true });
			var records = csv.Select(cr => new InvestmentProductRate {
				InvestmentProductId = investmentProductId,
				Date = cr.Date,
				Value = cr.AdjClose
			}).ToArray();
			if (!records.Any()) {
				throw new Exception("取得件数0件");
			}

			var existing = (await this._dbContext
					.InvestmentProductRates
					.Where(x =>
						x.InvestmentProductId == investmentProductId)
					.ToArrayAsync())
				.Where(x =>
					x.Date <= records.Max(r => r.Date) &&
					x.Date >= records.Min(r => r.Date))
				.ToArray();

			await this._dbContext.InvestmentProductRates.AddRangeAsync(records.Except(records.Where(x => existing.Any(e => e.InvestmentProductId == x.InvestmentProductId && e.Date == x.Date))));
			await this._dbContext.SaveChangesAsync();
		}

		protected virtual void Dispose(bool disposing) {
		}

		void IDisposable.Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
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
