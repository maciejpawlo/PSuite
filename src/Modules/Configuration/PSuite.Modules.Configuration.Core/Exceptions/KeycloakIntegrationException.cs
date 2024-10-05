using PSuite.Shared.Abstractions.Exceptions;

namespace PSuite.Modules.Configuration.Core.Exceptions;

public class KeycloakIntegrationException(Exception innerException) : PSuiteException("An error occured while communicating with keycloak", ExceptionCategory.ExternalServiceIntegration, innerException)
{

}
