# Shelling out to AzureAuth in Python

In Python shelling out to AzureAuth is easy. We recommend using `subprocess.run` with the `capture_output=True` option as shown in [azureauth.py](./azureauth.py).

You can test this out in this repo by running

```
bin\publish.cmd
```

and then

```
python examples\python\azureauth.py src\AzureAuth\dist\win10-x64\AzureAuth.exe
```