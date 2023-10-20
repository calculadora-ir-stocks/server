namespace Core.Models.Api.Responses
{
    public record SignUpResponse(Guid AccountId, string Jwt);
}
