using System;

namespace ScrapingService.Composition {
	public class ScrapingException : Exception {
		public ScrapingException() {
		}
		public ScrapingException(string message) : base(message) {
		}
		public ScrapingException(string message, Exception innerException) : base(message, innerException) { }
	}
}
