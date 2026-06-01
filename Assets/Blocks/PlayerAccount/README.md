# Unity Building Block - Player Account

A Unity Gaming Services player account and authentication system with cloud save data management.
This is intended to help accelerate the use of Unity services.

## Services and Usage

This block primarily uses three services
* [Authentication][auth]
* [Cloud Save][cloud-save]
* [Cloud Code][cloud-code]

**Authentication**

The [Authentication service][auth] is required by any usage of the services as it provides a unique identity
to the player, which will allow their data and experience to be personalized.

All blocks will require the authentication service, though this block simplifies the set up of
more advanced forms of authentication.

This block provides an authentication flow including Unity Authentication, Username and Password,
or Anonymous login. These advanced authentication flows must be explicitly enabled either in the dashboard
or via the project settings in the editor.

**Cloud Save**

[Cloud Save][cloud-save] is a service that provides the capacity to persist JSON data, and game files.
It also allows to save arbitrary game data that can be read by any user, which can be leveraged to create
features such as clans, guilds, and other global game state.

**Cloud Code**

[Cloud Code][cloud-code] is not required for the use of any of these block default features.
It is instead used to showcase the use of advanced Cloud Save features.

Cloud Save provides three types of [access to player data][access-class], and protected game data.
Some of these access classes cannot be written to except by a server authority, and Cloud Code provides
that functionality.

[cloud-save]:https://docs.unity.com/ugs/en-us/manual/cloud-save/manual
[auth]:https://docs.unity.com/ugs/en-us/manual/authentication/manual/get-started
[cloud-code]: https://docs.unity.com/ugs/en-us/manual/cloud-code/manual
[access-class]: https://docs.unity.com/ugs/en-us/manual/cloud-save/manual/concepts/player-data#access-classes

## Prerequisites

Here are a few prerequisites in order to have a proper on-boarding experience with the Player Account block.

### Project linked to a dashboard project

The project must be linked to a Unity Cloud project. To do so:

1. Open the project settings
2. Navigate to the `Services` section
3. If no project is linked here, please link it to an existing project or create a new project in your organization.

### Environment set

When using Unity Cloud functionalities in general, the application will target a specific environment
within the Cloud project linked. To select a target environment:

1. Make sure a project is linked (see [above](#project-linked-to-a-dashboard-project))
2. Open the project settings
3. Navigate to the `Services/Environments` section
4. Select the desired environment

Environment management such as creation and deletion must be done from the Cloud dashboard.

### Cloud Code Module Setup

The BlocksGameModule must be deployed to run the test-scene. 

Deployment is the act of creating or updating service configuration which can be found in the Unity Cloud.
This an important operation in managing environments with [configuration as code][env-mgmt].

It is **NOT** required for using the block.
The module can be found in `Assets/CloudCode/BlocksGameModule.ccmr`,
and deployed using the Deployment Window under **Services** > **Deployment**.

More details on the [Cloud Code module below](#cloud-code-module).

[env-mgmt]:https://docs.unity.com/ugs/manual/overview/manual/service-environments#environment-management-tooling

### Identity Provider Setup
The Username & Password and the Unity Authentication must be set up in the Editor through 
**Project Settings** > **Services** > **Authentication** or via the dashboard via
**Player Authentication** > **Identity Providers**.

Failing to do so, only "Anonymous" authentication will work. 

## Block Contents

**Custom UXML Elements:**
The custom UXML elements can be found under `Assets/Blocks/PlayerAccounts/Scripts/Runtime/UI/`.
They can be added to a scene's uxml by referencing them by name as such: 
```xml
<Blocks.PlayerAccount.PlayerIdLabel />
```

- `PlayerSignIn` - Authentication interface with sign-in flow, supporting Anonymous, Unity and Password authentication
- `PlayerIdLabel` - Reusable label that displays and tracks the player ID with clipboard integration
- `PlayerNameLabel` - Reusable data-bound label that tracks the player name with edit-in-place functionality
- `PlayerDataLabel` - Reusable label that loads Public, Default or Protected player data
- `PlayerEditableDataLabel` - Specialized PlayerDataLabel that allows editing player data (except for Protected)
- `PlayerLoadDataControl` - Debug control that displays all available default access class keys and loads the value of one on demand for the player

**Core Scripts:**
Located under `Assets/Blocks/PlayerAccounts/Scripts/Runtime/`, these scripts provide
the majority of the business logic for the block and can be re-used as-is.
- `AuthenticationObserver` - Handles sign-in events and authentication state, and allows registering callbacks, can be used independently of the UXML elements
- `CloudDataContainer` - Manages async player data loading and binding, can be used independently of the UXML elements

**Resources:**
Located under `Assets/Blocks/PlayerAccounts/Scripts/Runtime/UI/`
- UI icons (edit, copy, etc.)
- USS stylesheets for the block

**Test Scene**
Located under `Assets/Blocks/PlayerAccount/TestScenes`
- Sample UXML layouts demonstrating integration with the labels and a MonoBehaviour controller

By default, when you first load the scene, there will be no player data.
Hit the "Populate Player Data" to give your player some default content.

## Test Scene

The test scene demonstrates the complete player account flow:

1. **Initial State** - Shows sign-in interface with authentication flows
2. **Data Display** - Displays player data as declared by `PlayerAccountScene.uxml` labels
3. **Cloud Integration** - Displays shared global score managed by the cloud-code module

Launch the test scene and follow the UI prompts to follow the authentication flow 
and player data usage.

## Add Player Account Controls to a scene

In order to add the controls to a new or existing scene you will need the following
1) Make sure Unity Services are initialized and Authentication service has been signed in
    1) The `UnityServices` prefab at `Assets/Blocks/Common/UnityServices.prefab` will initialize on start and sign in anonymously
    2) You can also do this via code with `UnityServices.Instance.InitializeAsync()` and `AuthenticationService.Instance.SignInAnonymouslyAsync()`
2) Add a UI Document component to your scene if one does not exist. (see also how to [get started with UI Toolkit][uitk])
   1) Assign a valid UXML asset if one does not exist
3) Add the uxml tags that you want to use from above

[uitk]: https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-get-started-with-runtime-ui.html
### Cloud Code Module

The cloud code module manages game-level shared data sample of a global score, using a
write lock mechanism.
The cloud code module is shared across blocks, but the relevant file this block introduces is
`Assets/CloudCode/BlocksGameModule~/Project/GlobalScoreClient.cs`.
The client is generated under `Assets/CloudCode/GeneratedModuleBindings/BlocksGameModule/GlobalScoreClientBindings.cs`
This client is exclusively used for the test-scene.
