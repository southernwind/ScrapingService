using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

using DataBase;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ScrapingService.Composition;

namespace ScrapingService {
	public class Cron : IDisposable {
		private readonly IEnumerable<IScrapingServiceTarget> _targets;
		private bool _disposedValue;
		private readonly HomeServerDbContext _dbContext;
		private readonly CompositeDisposable _disposable = new();
		private readonly ILogger<Cron> _logger;

		public Cron(ILogger<Cron> logger, IServiceProvider serviceProvider, HomeServerDbContext dbContext) {
			this._targets = serviceProvider.GetServices<IScrapingServiceTarget>();
			this._dbContext = dbContext;
			this._logger = logger;
		}
		public Task StartAsync() {
			this._disposable.Add(
				Observable
					.Timer(TimeSpan.Zero, TimeSpan.FromDays(1))
					.Subscribe(async _ => {
						var ipList = await this._dbContext.InvestmentProducts.Where(x => x.Enable).ToArrayAsync();
						foreach (var ip in ipList) {
							try {
								await this._targets.Single(x => x.GetType().FullName == ip.Type)
									.ExecuteAsync(ip.InvestmentProductId, ip.Key);
							} catch (Exception ex) {
								this._logger.LogWarning(0, "取得エラー", ex);
							}
						}
					}));
			return Task.CompletedTask;

		}

		protected virtual void Dispose(bool disposing) {
			if (this._disposedValue) {
				return;
			}

			if (disposing) {
				this._disposable.Dispose();
			}
			this._disposedValue = true;
		}

		public void Dispose() {
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}
	}
}
