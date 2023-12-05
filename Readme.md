# Neon Management System (NeonMuon)

# Development

* Copy the contents of `appsettings.Example.json` into the user secrets file and tweak as needed.
    * Linux: `~/.microsoft/usersecrets/NeonMuon/secrets.json`
    * Windows: `%APPDATA%\Microsoft\UserSecrets\NeonMuon\secrets.json`
* Generate random base64 encoded secrets with OpenSSL.
    * 128 bits: `openssl rand -base64 16`
    * 256 bits: `openssl rand -base64 32`
    * 512 bits: `openssl rand -base64 64`
* [Npgsql connection string parameters](https://www.npgsql.org/doc/connection-string-parameters.html)
