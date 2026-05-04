# Device Profiles

Device profiles configure the viewport resolution and device characteristics for UI testing.

## Profile Format

Each profile is a JSON file with the following structure:

```json
{
  "Name": "profile-name",
  "Description": "Human-readable description",
  "ViewportWidth": 1440,
  "ViewportHeight": 3120,
  "DeviceScaleFactor": 3.0
}
```

- **Name**: Identifier for the profile (should match filename without .json)
- **Description**: Optional description of the device
- **ViewportWidth**: Physical screen width in pixels
- **ViewportHeight**: Physical screen height in pixels  
- **DeviceScaleFactor**: Device pixel ratio (physical pixels / CSS pixels)

The CSS viewport is automatically calculated as: `Physical Resolution / DeviceScaleFactor`

## Using Profiles

Set the `CLYPSE_UITEST_PROFILE` environment variable to the profile name (without .json extension):

### PowerShell
```powershell
$env:CLYPSE_UITEST_PROFILE = "s24ultra"
dotnet test
```

### Command Prompt
```cmd
set CLYPSE_UITEST_PROFILE=s24ultra
dotnet test
```

### VS Code launch.json
```json
{
  "env": {
    "CLYPSE_UITEST_PROFILE": "s24ultra"
  }
}
```

If not set, the default profile `s24ultra` is used.

## Available Profiles

- **s24ultra**: Samsung Galaxy S24 Ultra (1440x3120, 3x DPR, portrait)

## Screenshot Organization

Screenshots are automatically organized by profile:
```
TestResults/
  Screenshots/
    s24ultra/
      TestName_01_Navigation_LoginPage.png
      TestName_02_Action_ClickedButton.png
      ...
```

## Adding New Profiles

1. Create a new JSON file in the `Profiles` folder
2. Define the device characteristics
3. Set `TEST_DEVICE_PROFILE` to the new profile name
4. Run tests

Example for iPhone 15 Pro:
```json
{
  "Name": "iphone15pro",
  "Description": "iPhone 15 Pro in portrait orientation",
  "ViewportWidth": 1179,
  "ViewportHeight": 2556,
  "DeviceScaleFactor": 3.0
}
```
