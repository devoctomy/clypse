[![CI](https://github.com/devoctomy/clypse/actions/workflows/ci.yml/badge.svg)](https://github.com/devoctomy/clypse/actions/workflows/ci.yml)

# clypse
Clypse secrets management.

## Requirements



## Testing

### Unit Testing

You can simply run the unit tests from within Visual Studio or use the following command from the project root,

```
dotnet test clypse.core.UnitTests --no-build --configuration Release --verbosity normal --logger trx --results-directory ./TestResults/UnitTests
```

### Integration Testing

Integration testing requires the following environment variables

* CLYPSE_AWS_BUCKETNAME - Test bucket name
* CLYPSE_AWS_ACCESSKEY - IAM credentials for accessing the s3 bucket
* CLYPSE_AWS_SECRETACCESSKEY - IAM credentials for accessing the s3 bucket

The integration tests test the underlying core framework without any UI. The tests will create a vault, with a number of secrets and then clean up after itself.

### Manual Testing

You can either simply press play in Visual Studio, which should launch the portal in Debug build on port 7153, or you can perform the full WASM build which is *much* faster and is what gets published to production. The Debug build also has certain features cranked down in order to improve performance merely during running simple tests,

1. The default cryptographic Argon2id key derivation parameters are set to 64mb. This is still slow, but adequate for testing.
2. Weak password list is hardcoded to a single entry, as JS <-> C# interop is extremely slow when not running the full WASM build.

The easiest way serve the full WASM build locally you can use the dotnet-serve tool. To install it run the following command,

```
dotnet tool install -g dotnet-serve
```

Then run the following to publish the build locally and serve it over SSL.

```
dotnet tool install -g dotnet-serve
dotnet publish ./clypse.portal/clypse.portal.csproj -c Release -r browser-wasm --self-contained
dotnet serve -d ./clypse.portal/bin/Release/net8.0/publish/wwwroot -p 7153 --tls
```

> Please note that you must have CORS configured for the S3 bucket for 'https://localhost:7153' otherwise all requests to S3 will fail.