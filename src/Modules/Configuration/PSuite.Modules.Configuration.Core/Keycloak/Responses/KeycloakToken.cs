﻿using System.Text.Json.Serialization;

namespace PSuite.Modules.Configuration.Core.Keycloak.Responses;

internal record KeycloakToken(
    [property: JsonPropertyName("access_token")] string AccessToken, 
    [property: JsonPropertyName("expires_in")] int ExpiresIn, 
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType, 
    [property: JsonPropertyName("not-before-policy")]int NotBeforePolicy,
    string Scope);