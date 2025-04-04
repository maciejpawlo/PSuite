﻿using System.Text.Json.Serialization;

namespace PSuite.Modules.Configuration.Core.Keycloak.Requests;

internal record class KeycloakUser(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("username")] string UserName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("enabled")] bool Enabled,
    List<string> RequiredActions,
    Credentials? Credentials);

internal class KeycloakUserBuilder
{
    private readonly Guid _id;
    private readonly string _firstName;
    private readonly string _lastName;
    private readonly string _userName;
    private bool _isEnalbled;
    private string? _email;
    private List<string> _requiredActions = [];
    private Credentials? _credentials;

    internal KeycloakUserBuilder(Guid id, string firstName, string lastName)
    {
        _id = id;
        _firstName = firstName;
        _lastName = lastName;
        _isEnalbled = false;
        _userName = $"{firstName}_{lastName}";
    }

    internal KeycloakUserBuilder WithCredentials(Credentials credentials)
    {
        _credentials = credentials;
        return this;
    }

    internal KeycloakUserBuilder WithRequiredActions(params string[] requiredActions)
    {
        _requiredActions = [.. requiredActions];
        return this;
    }

    internal KeycloakUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    internal KeycloakUserBuilder Enabled()
    {
        _isEnalbled = true;
        return this;
    }

    internal KeycloakUser Build() 
    {
        return new KeycloakUser(_id, _firstName, _lastName, _userName, _email, _isEnalbled, _requiredActions, _credentials);
    }
}

internal record class Credentials(
    string Type, 
    string Value, 
    [property: JsonPropertyName("temporary")] bool IsTemporary
);

internal class CredentialsType
{
    public const string Password = "password";
}

internal class RequiredActions
{
    public const string UpdatePassword = "UPDATE_PASSWORD";
    public const string UpdateEmail = "UPDATE_EMAIL";
}

internal class Roles
{
    public const string Employee = "employee";
    public const string Manager = "manager";
}