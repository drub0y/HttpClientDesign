using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace HttpClientDesign
{
    public interface ISomeDomainService
    {
        Task<string> GetSomeThingsValueAsync(string id);
    }

    public class SomeDomainService : ISomeDomainService
    {
        private readonly HttpClient _httpClient;

        public SomeDomainService()
            : this(new HttpClient())
        {

        }

        public SomeDomainService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetSomeThingsValueAsync(string id)
        {
            HttpResponseMessage responseMessage = await _httpClient.GetAsync("/things/" + Uri.EscapeUriString(id));

            if(responseMessage.IsSuccessStatusCode)
            {
                return await responseMessage.Content.ReadAsStringAsync();
            }
            else
            {
                switch(responseMessage.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        throw new SomeThingDoesntExistException(id);

                    // any other cases you want to might want to handle specifically for your domain

                    default:
                        // Fallback to leave
                        throw new HttpCommunicationException(responseMessage.StatusCode, responseMessage.ReasonPhrase);
                }
            }
        }
    }

    [Serializable]
    public class SomeThingDoesntExistException : Exception
    {
        public SomeThingDoesntExistException(string id)
        {
            this.Id = id;
        }
        protected SomeThingDoesntExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public string Id
        {
            get;
            private set;
        }
    }

    [Serializable]
    public class HttpCommunicationException : Exception
    {
        public HttpCommunicationException(HttpStatusCode statusCode, string reasonPhrase) : base(string.Format("HTTP communication failure: {0} - {1}", statusCode, reasonPhrase))
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
        }

        protected HttpCommunicationException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }

        public HttpStatusCode StatusCode
        {
            get;
            private set;
        }

        public string ReasonPhrase
        {
            get;
            private set;
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
        {
        }
    }
}
