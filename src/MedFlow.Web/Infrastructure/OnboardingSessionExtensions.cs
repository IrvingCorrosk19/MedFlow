using System.Text.Json;
using MedFlow.Web.Models.Onboarding;

namespace MedFlow.Web.Infrastructure;

public static class OnboardingSessionExtensions
{
    private const string Key = "MedFlow.Onboarding.State";

    public static OnboardingSessionState? GetOnboardingState(this ISession session)
    {
        var json = session.GetString(Key);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<OnboardingSessionState>(json);
    }

    public static void SetOnboardingState(this ISession session, OnboardingSessionState state)
    {
        session.SetString(Key, JsonSerializer.Serialize(state));
    }

    public static void ClearOnboardingState(this ISession session) => session.Remove(Key);
}
