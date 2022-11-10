# Contributing

This project welcomes contributions and suggestions. Most contributions require you to
agree to a Contributor License Agreement (CLA) declaring that you have the right to,
and actually do, grant us the rights to use your contribution. For details, visit
https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need
to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the
instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Required Tools
* The [dotnet CLI (SDK)](https://dotnet.microsoft.com/download)
* Python 3.8 or later https://www.python.org/downloads/
* Git

## Recommended Tools
* Visual Studio and Visual Studio Code https://visualstudio.microsoft.com/

# Development Setup

- Clone the auth repo
  ```
  git clone https://github.com/AzureAD/microsoft-authentication-cli
  ```
- CD into `microsoft-authentication-cli`

## Windows

   On Windows Visual Studio is the primary way to work in the auth repo. 
* Launch the solution by running
  ```
  start AzureAuth.sln
  ```
* To get started running the auth cli, set the `AzureAuth` project as the startup project, and run!
* You can add arguments and options in the Debug Options menu (at the bottom of the main Debug dropdown menu).

You can also you the command line to build and run. This is typically faster and more useful for testing out the cli after you've made changes in Visual Studio. Using the `dotnet` CLI you can:
- Build the project
  ```
  dotnet build
  ```

- Run the tests with
  ```
  dotnet test
  ```

- Run the auth CLI (which by default builds it first, so any new changes you have made will take affect).
  ```
  dotnet run --project src\AzureAuth -- --help
  ```
  Note the `--` between `AzureAuth` and `--help`. The `--` tells `dotnet` that all following arguments are to be passed onto the application it is building and running, rather than being arguments or options for the `dotnet` CLI.

  
## macOS

1. Download and install the Artifacts Credential Provider plugin for `dotnet`.
   ```shell
   curl -fsSL https://aka.ms/install-artifacts-credprovider.sh | sh
   ```
2. Run an interactive restore and authenticate when prompted for a device code flow login.
   ```shell
   dotnet restore --interactive
   ```
3. Build the project.
   ```shell
   dotnet build
   ```
4. Run the project as needed.
   ```shell
   dotnet run --project AzureAuth -- ${more_options_go_here}
   ```

# Benchmark
The project `MSALWrapper.Benchmark` introduces a 3rd party framework [BenchmarkDotNet](https://benchmarkdotnet.org/). To use this benchmark project, set the `MSALWrapper.Benchmark` as the startup project, and change to release mode. Then the benchmark project will compile itself again and run series profiles then print the result in console.

## Add new benchmarks
See https://benchmarkdotnet.org/articles/guides/getting-started.html.

## Run benchmarks
Run the benchmark project.
```shell
dotnet run --configuration release --project .\src\MSALWrapper.Benchmark
```

Now, there is only one excutable benchmark. If there are more benchmarks in further, specify the property StartupObject. For example:
```shell
dotnet run --configuration release --project .\src\MSALWrapper.Benchmark --property:StartupObject=Microsoft.Authentication.MSALWrapper.Benchmark.BrokerBenchmark
```