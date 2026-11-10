namespace MiniNaturalFarms.Application.Auth;

public class AuthResult
{
    public bool Succeeded { get; private set; }
    public AuthResponse? Data { get; private set; }
    public List<string> Errors { get; private set; } = new();

    public static AuthResult Ok(AuthResponse data) => new() { Succeeded = true, Data = data };

    public static AuthResult Fail(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}
