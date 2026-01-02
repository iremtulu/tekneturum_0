using Kaya_Otel.ViewModels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kaya_Otel.Services
{
    public interface IInstagramService
    {
        Task<IReadOnlyList<InstagramPostViewModel>> GetLatestPostsAsync(int take = 4);
    }

    /// <summary>
    /// Gerçek Instagram Graph API entegrasyonu.
    /// Facebook Developer hesabı ve Instagram Business hesabı gerektirir.
    /// </summary>
    public class InstagramGraphService : IInstagramService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly string _instagramBusinessAccountId;
        private readonly ILogger<InstagramGraphService> _logger;

        public InstagramGraphService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<InstagramGraphService> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _accessToken = configuration["Instagram:AccessToken"] ?? string.Empty;
            _instagramBusinessAccountId = configuration["Instagram:BusinessAccountId"] ?? string.Empty;
            _logger = logger;
        }

        public async Task<IReadOnlyList<InstagramPostViewModel>> GetLatestPostsAsync(int take = 4)
        {
            // Eğer token yoksa boş liste döndür
            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogWarning("Instagram API access token yapılandırılmamış.");
                return Array.Empty<InstagramPostViewModel>().ToList().AsReadOnly();
            }

            // BusinessAccountId yoksa 'me' endpointini kullan (Basic Display senaryosu)
            if (string.IsNullOrEmpty(_instagramBusinessAccountId))
            {
                _logger.LogWarning("Instagram BusinessAccountId yapılandırılmamış, 'me' endpointi kullanılacak.");
            }

            try
            {
                // Instagram Graph API endpoint
                var accountIdOrMe = string.IsNullOrEmpty(_instagramBusinessAccountId)
                    ? "me"
                    : _instagramBusinessAccountId;

                var url = $"https://graph.instagram.com/{accountIdOrMe}/media?fields=id,caption,media_type,media_url,permalink,thumbnail_url&access_token={_accessToken}&limit={take}";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Instagram API hatası: {response.StatusCode}");
                    return Array.Empty<InstagramPostViewModel>().ToList().AsReadOnly();
                }

                var json = await response.Content.ReadAsStringAsync();

                // Gelen ham JSON'u logla (sadece geliştirme için)
                _logger.LogInformation("Instagram API raw response: {Json}", json);

                var apiResponse = JsonSerializer.Deserialize<InstagramApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    return Array.Empty<InstagramPostViewModel>().ToList().AsReadOnly();
                }

                // Önce sadece resim ve albümleri dene; hiç yoksa tüm medyayı göster
                var mediaItems = apiResponse.Data;

                var filtered = mediaItems
                    .Where(p => string.Equals(p.MediaType, "IMAGE", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(p.MediaType, "CAROUSEL_ALBUM", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!filtered.Any())
                {
                    filtered = mediaItems.ToList();
                }

                var posts = filtered
                    .Take(take)
                    .Select(p => new InstagramPostViewModel
                    {
                        Caption = p.Caption ?? string.Empty,
                        ImageUrl = p.MediaUrl ?? p.ThumbnailUrl ?? string.Empty,
                        Permalink = p.Permalink ?? "https://www.instagram.com/tourkekova/"
                    })
                    .ToList()
                    .AsReadOnly();

                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Instagram API çağrısı sırasında hata oluştu.");
                return Array.Empty<InstagramPostViewModel>().ToList().AsReadOnly();
            }
        }

        private class InstagramApiResponse
        {
            public List<InstagramMedia>? Data { get; set; }
        }

        private class InstagramMedia
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            
            [JsonPropertyName("caption")]
            public string? Caption { get; set; }
            
            [JsonPropertyName("media_type")]
            public string? MediaType { get; set; }
            
            [JsonPropertyName("media_url")]
            public string? MediaUrl { get; set; }
            
            [JsonPropertyName("thumbnail_url")]
            public string? ThumbnailUrl { get; set; }
            
            [JsonPropertyName("permalink")]
            public string? Permalink { get; set; }
        }
    }

    /// <summary>
    /// Gerçek Instagram Graph API entegrasyonu yerine sahte veri döndüren servis.
    /// Token yapılandırılmadığında kullanılır.
    /// </summary>
    public class FakeInstagramService : IInstagramService
    {
        public Task<IReadOnlyList<InstagramPostViewModel>> GetLatestPostsAsync(int take = 4)
        {
            var posts = Enumerable.Range(1, take)
                .Select(i => new InstagramPostViewModel
                {
                    Caption = $"Instagram örnek gönderi {i}",
                    ImageUrl = $"/images/instagram{i}.jpg",
                    Permalink = "https://www.instagram.com/tourkekova/"
                })
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<InstagramPostViewModel>>(posts);
        }
    }
}

