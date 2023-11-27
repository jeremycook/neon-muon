# Neon Management System (NeonMS)

# Development

* Copy the contents of `appsettings.Example.json` into the user secrets file and tweak as needed.
    * Linux: `~/.microsoft/usersecrets/NeonMS/secrets.json`
    * Windows: `%APPDATA%\Microsoft\UserSecrets\NeonMS\secrets.json`
* Generate random base64 encoded secrets with OpenSSL.
    * 128 bits: `openssl rand -base64 16`
    * 256 bits: `openssl rand -base64 32`
    * 512 bits: `openssl rand -base64 64`
* [Npgsql connection string parameters](https://www.npgsql.org/doc/connection-string-parameters.html)
