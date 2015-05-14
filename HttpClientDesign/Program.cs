using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Runtime.Serialization;

namespace HttpClientDesign
{
	public interface ISomeDomainService
	{
		Task<string> GetSomeThingsValueAsync(string id);
	}

	[Serializable]
	public class SomeThingDoesntExistException : Exception
	{
		public SomeThingDoesntExistException() { }
		protected SomeThingDoesntExistException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	} 

	public class SomeDomainService : ISomeDomainService
	{
		private readonly HttpClient _httpClient;

		public SomeDomainService(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<string> GetSomeThingsValueAsync(string id)
		{
			return await _httpClient.GetStringAsync($"/things/{id}");
		}
	}

	public class Tests
	{
		[Fact]
		public async Task GettingSomeThingsValueReturnsExpectedValue()
		{
			// Arrange
			var mockHttpMessageHandler = new MockHttpMessageHandler();
			mockHttpMessageHandler.Expect("http://unittest/things/123")
				.Respond(new StringContent("expected value"));

			SomeDomainService sut = new SomeDomainService(new HttpClient(mockHttpMessageHandler)
			{
				BaseAddress = new Uri("http://unittest")
			});

			// Act
			var value = await sut.GetSomeThingsValueAsync("123");

			// Assert
			value.Should().Be("expected value");
	    }

		[Fact]
		public void GettingSomeThingsValueForIdThatDoesntExistThrowsExpectedException()
		{
			// Arrange
			var mockHttpMessageHandler = new MockHttpMessageHandler();

			SomeDomainService sut = new SomeDomainService(new HttpClient(mockHttpMessageHandler)
			{
				BaseAddress = new Uri("http://unittest")
			});

			// Act
			Func<Task> action = async () => await sut.GetSomeThingsValueAsync("SomeIdThatDoesntExist");

			// Assert
			action.ShouldThrow<SomeThingDoesntExistException>();
		}
	}

	public class Program
	{
		public static void Main(string[] args)
		{ }
	}
}
