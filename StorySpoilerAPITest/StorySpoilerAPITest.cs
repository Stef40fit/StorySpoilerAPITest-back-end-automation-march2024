using System.Net;
using System.Text.Json;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoilerAPITest.Models;

namespace StorySpoilerAPITest
{

    public class StorySpoilerAPITest
    {
        private RestClient client;
        private static string? lastCreatedStoryId;


        private const string BASEURL = "https://d5wfqm7y6yb3q.cloudfront.net";
        private const string USERNAME = "stefan+story";
        private const string PASSWORD = "123789";




        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(USERNAME, PASSWORD);

            var options = new RestClientOptions(BASEURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient authClient = new RestClient(BASEURL);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new
            {
                username,
                password
            });

            var response = authClient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("The JWT token is null or empty.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Authentication failed: {response.StatusCode}, {response.Content}");
            }
        }


        [Test, Order(1)]

        public void CreateStorySpoiler_WithRequiredFields_ShouldSucceed()
        {
            var storyRequest = new StoryDto()
            {
                Title = "New story",
                Description = "A detailed description of the story."

            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
            lastCreatedStoryId = createResponse.StoryId;

        }

        [Test, Order(2)]

        public void EditExistingStorySpoiler_ShouldSucceed()
        {

            var editRequest = new StoryDto()
            {
                Title = "Edited Story",
                Description = "Updated description."

            };

            var request = new RestRequest("/api/Story/Edit");
            request.AddQueryParameter("storyId", lastCreatedStoryId);
            request.AddJsonBody(editRequest);

            var response = client.Execute(request, Method.Put);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));


        }

        [Order(3)]
        [Test]
        public void DeleteExistingStorySpoiler_ShouldSucceed()
        {


            var request = new RestRequest($"/api/Story/Delete");
            request.AddQueryParameter("storyId", lastCreatedStoryId);


            var response = this.client.Execute(request, Method.Delete);
            var expectedResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(expectedResponse.Msg, Is.EqualTo("Deleted successfully!"));



        }

        [Order(4)]
        [Test]
        public void CreateStorySpoiler_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            // Try to create an idea request without title and description
            var storyRequest = new StoryDto
            {
                Title = "",
                Description = "",
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(5)]
        [Test]
        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            // Using an Story ID that is known to not exist.
            string nonExistingStoryId = "123";
            var storyRequest = new ApiResponseDto()
            {
                Msg = "Updated Title",
                StoryId = "Updated Description",
            };

            var request = new RestRequest($"/api/Story/Edit", Method.Put);
            request.AddQueryParameter("StoryId", nonExistingStoryId);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            string expectedResponse = "No spoilers...";
            Assert.That(response.Content, Does.Contain(expectedResponse));
        }

        [Order(6)]
        [Test]
        public void DeleteNonExistingIdea_ShouldReturnNotFound()
        {
            // Using a Story ID that is known to not exist.
            string nonExistingStoryId = "123";
            string jwtToken = GetJwtToken(USERNAME, PASSWORD);

            var request = new RestRequest($"/api/Story/Delete", Method.Delete);
            request.AddQueryParameter("storyId", nonExistingStoryId);
            request.AddHeader("Authorization", "Bearer " + jwtToken);
            var response = this.client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            string expectedMessage = "Unable to delete this story spoiler!";
            Assert.That(response.Content, Does.Contain(expectedMessage));
        }

    }
}