using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using ScrapingService.Composition;

namespace ScrapingService {
	public class Cron : IDisposable {
		private readonly IEnumerable<IScrapingServiceTarget> _targets;
		private bool _disposedValue;

		public Cron(IServiceProvider serviceProvider) {
			this._targets = serviceProvider.GetServices<IScrapingServiceTarget>();
		}
		public async Task StartAsync() {
			foreach (var target in this._targets) {
				await target.ExecuteAsync();
			}
		}
		protected virtual void Dispose(bool disposing) {
			if (this._disposedValue) {
				return;
			}

			if (disposing) {
				foreach (var target in this._targets) {
					target.Dispose();
				}
			}
			this._disposedValue = true;
		}

		public void Dispose() {
			this.Dispose(true);
			System.GC.SuppressFinalize(this);
		}
	}
}
