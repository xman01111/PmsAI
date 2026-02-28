using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PmsAI.Api.Options;
using PmsAI.Contracts.Pms;

namespace PmsAI.Api.Pms;

public class PmsAdapterClient : IPmsClient
{
    private readonly HttpClient _http;
    private readonly PmsAdapterOptions _options;
    private readonly ILogger<PmsAdapterClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public PmsAdapterClient(IHttpClientFactory httpClientFactory, IOptions<PmsAdapterOptions> options, ILogger<PmsAdapterClient> logger)
    {
        _http = httpClientFactory.CreateClient("PmsAdapter");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<ReservationDto>> GetReservationsAsync(Guid hotelId, ReservationQueryDto query, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl}/hotels/{hotelId}/reservations";
        var queryParams = BuildQueryString(new Dictionary<string, string?>
        {
            ["reservationNo"] = query.ReservationNo,
            ["guestName"] = query.GuestName,
            ["phone"] = query.Phone,
            ["arrivalDate"] = query.ArrivalDate
        });
        var response = await _http.GetAsync($"{url}?{queryParams}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<ReservationDto>>(json, _jsonOptions) ?? new();
    }

    public async Task<List<RoomStatusDto>> GetRoomStatusAsync(Guid hotelId, RoomStatusQueryDto query, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl}/hotels/{hotelId}/rooms/status";
        var queryParams = BuildQueryString(new Dictionary<string, string?>
        {
            ["date"] = query.Date
        });
        var response = await _http.GetAsync($"{url}?{queryParams}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<List<RoomStatusDto>>(json, _jsonOptions) ?? new();
    }

    public async Task<WorkOrderResultDto> CreateWorkOrderAsync(Guid hotelId, CreateWorkOrderDto dto, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl}/hotels/{hotelId}/work-orders";
        var json = JsonSerializer.Serialize(dto);
        var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WorkOrderResultDto>(responseJson, _jsonOptions) ?? new();
    }

    public async Task<LateCheckoutResultDto> RequestLateCheckoutAsync(Guid hotelId, LateCheckoutRequestDto dto, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl}/hotels/{hotelId}/late-checkout";
        var json = JsonSerializer.Serialize(dto);
        var response = await _http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<LateCheckoutResultDto>(responseJson, _jsonOptions) ?? new();
    }

    private static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        return string.Join("&", parts);
    }
}
