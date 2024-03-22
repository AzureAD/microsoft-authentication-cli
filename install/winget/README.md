# Install wix
`dotnet tool install --global wix --version 5.0.0-rc.1`

Navigate to `microsoft-authentication-cli\install\winget` folder.

# Run wix to generate MSI
After running publish, rename `dist` to `0.8.5` and copy contents underneath it to a folder structure as `microsoft-authentication-cli\install\winget\Programs\AzureAuth\0.8.5`. The `Product.wxs` reads contents of this path to generate a MSI.

`wix build -src Product.wxs`

This will create a `Product.msi`

# Local testing 
## Validate local manifests
`winget validate manifests` where `manifests` is folder where all the YAML manifests live

## Run python
`Microsoft.AzureAuth.installer.yaml` requires the `InstallerUrl` to start with `http` or `https`.

Run `python -m http.server 8081` to redirect local traffic to 8081 as the `InstallerUrl` is set as `http://localhost:8081/Product.msi`

## Test AzureAuth installation
`winget install -m manifests`

# Future steps
1. Signing the MSI
2. Setting the environment variable correctly.
3. Warning as "no upgrade found" if the latest version already exists.
4. Automate CI.