/// <summary>
	  /// Converts an encrypted <see cref="ConfigurationProperty"/> to a <see cref="string"/>. NOTE: This function should be
	  /// avoided if possible, as under the covers it converts a <see cref="SecureString"/> to a <see cref="string"/> object
	  /// which defeats the purpose of a <see cref="SecureString"/>. Use this only when you have an uncontrolled dependency on
	  /// needing a <see cref="string"/>.
	  /// </summary>
	  /// <param name="encryptedProperty">The <see cref="ConfigurationProperty"/> to decrypt to a <see cref="string"/></param>
	  /// <returns></returns>
public static string DecryptValueAsString(this ConfigurationProperty encryptedProperty)
{
	SecureString secureString = encryptedProperty.DecryptValue();

	// Just using the network credential class as a convenient way of converting a secure string to a string
	return new NetworkCredential(string.Empty, secureString).Password;
}