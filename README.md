## Disclaimer
This project was originally designed to be used by our internal projects that all use our ADFS server to login. The project was made in mind of this, therefor some things may look hardcoded but that is on purpose.

## What is SSO-Package?
SSO-Package lets you setup SSO with identity, in about 5 minutes, it only requires you to install a nuget package, setup the configuration inside `program.cs`, and add the roles you want aswell as the connectionstring for your database. <br />
It gives you enough flexability to change the functionality of the controllers used, you are able to change the routes, aswell as adding custom policies.


*<u>You do not need to create new migrations, if you are connecting to an already existing database!</u>*
*If you have to create the migrations, Then you need to add the following to the project:*

*You might be able to use newer versions, but has not been tested. Remember the package runs .NET 8 for now.*
| Nuget Package | Version | Description |
| ------------- | ------- | ----------- |
| [Microsoft.EntityFrameworkCore.Design](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/8.0.12)           | `8.0.12` | Allows you to create migrations. |
| [Microsoft.EntityFrameworkCore.Tools](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Tools/8.0.12_src=template)               | `8.0.12` | Adds migration tools for `EF`. |

### Table of contents
  - [What is SSO-Package](#what-is-sso-package)
    - [What does the package setup for you?](#what-does-the-package-setup-for-you)
  - [Getting started](#getting-started)
    - [Program.cs](#programcs)
    - [Appsettings.json](#appsettingsjson)
    - [LaunchSettings.json](#launchsettingsjson)
    - [User state](#user-state)
    - [Production](#production)


### What does the package setup for you?
You can read about the different endpoints and how the methods are constructed.

<details> <summary>AddSSO</summary>
This is a part of your builder, and can be added anywhere before you build your builder.

---
This method gives alot of overrided methods that you can use where all of them takes different parameters.
All of the methods are calling depending on one another, meaning one has to be the "parent" where all the logic is, the parent here is the method with the most generic parameters.

| Generic types                 | Parameters                                                    |
| ----------------------------- | ------------------------------------------------------------- |
| None                          | `Action<WsFederationOptions> adfsSettings`, `Action<AuthorizationOptions> authOptions` |
| `TContext:IdentityDbContext`  | `Action<WsFederationOptions> adfsSettings`, `Action<AuthorizationOptions> authOptions` |
| None                          | `string connectionString`, `Action<WsFederationOptions> adfsSettings`, `Action<AuthorizationOptions> authOptions` |
| `TContext:IdentityDbContext`  | `string connectionString`, `Action<WsFederationOptions> adfsSettings`, `Action<AuthorizationOptions> authOptions` |
| `TContext:IdentityDbContext`, `TIdentityUser:class`, `TIdentityRole:class` | `string connectionString`, `Action<WsFederationOptions> adfsSettings`, `Action<AuthorizationOptions> authOptions` |
 
Adds identity, with IdentityUser and IdentityRole if you haven't defined any. <br />
*Depending on how you construct the custom classes, it might not work.*
```cs
provider.Services.AddIdentity<TIdentityUser, TIdentityRole>()
    .AddEntityFrameworkStores<TContext>();
```

Adds Authentication and WsFederation with <u>adfsSettings</u>. <br />
```cs
provider.Services.AddAuthentication()
    .AddWsFederation(adfsSettings);
```

<u>adfsSettings</u> is of type `WsFederationOptions` and should look something like this:
```cs
adfsSettings: x =>
{
    x.MetadataAddress = "https://`Your Hostname`/FederationMetadata/2007-06/FederationMetadata.xml";
    x.Wtrealm = "https://localhost:7003";
};
```

authOptions is not required, but it allows you to create policies that the user should adhere.
Though if you dont care about policies etc and just leaves it empty, it fills the delegate to ensure it is not empty.
```cs
authOptions ??= x => { };
provider.Services.AddAuthorization(authOptions);
```

connectionString is not required either, but if you do not pass a connectionstring it will take the default string from your `appsettings.json`. It also adds the context as a service for you to use. </br >
There is a built-in context named `BuiltInContext` that `AddSSO` will use by default if no context has been passed to the method.
```cs
connectionString ??= provider.Configuration.GetConnectionString("DefaultConnection");
provider.Services.AddDbContext<TContext>(options => options.UseSqlServer(string.IsNullOrEmpty(connectionString) ? connectionString : throw new Exception("Connectionstring was not found")));
```

---

</details>

<details> <summary>ConfigureApiRoutes</summary>
This is a part of your services, and can be added anywhere before you build your builder.

---
This method maps all of the controllers for our minimalAPI, that the nuget package uses, where you are able to change the different routes for the different controllers.

| Controller    | Default route       | End route       |
| ------------- | ------------------- | --------------- |
| Redirect      | `/api/sso/redirect` | Callback route  |
| CallbackAsync | `/api/sso/callback` | `/`             |
| Logout        | `/api/sso/logout`   | `/`             |
---

</details>

<details> <summary>ConfigureMinimalAPIServices</summary>
This is a part of your application, and have to be added after `app.UseRouting()`. </br >
Create 2 scoped services.

---
This method allows you to choose wether you want to use the built in functionality or you need to add some more functionality for the methods.

If you want to create a 'custom' service for the controllers, you can either inherit from the interface or class as mentioned in the table.

| Base Class           | Interface |Controller    |
| --------------- | --------- | ------------- |
| [CallbackService](https://github.com/whysoshy/sso-package/-/blob/master/SSO-Package/MinimalAPI_Service/CallbackService.cs?ref_type=heads) | `ICallBackService` | CallbackAsync |
| [LogoutService](https://github.com/whysoshy/sso-package/-/blob/master/SSO-Package/MinimalAPI_Service/LogoutService.cs?ref_type=heads) | `ILogoutService` | Logout |

You are able to tell the method, that it should create a new scope of the class that you have created.
If you havn't created any, you dont have to pass any class.

---

</details>



## Getting started

### Program.cs
---

**Your project needs to be running .NET 8 beacuse of WSFed.** <br />

Inside your builder, you should add the SSO and configure the minimalAPI services. <br />
If you have to create a new database, you have to send the assembly name to AddSSO. Assembly name is basically just the project name.

*Remember there is alot of overrited methods for AddSSO() and some for ConfigureMinimalAPIServices(), use the one that suits your usecase best.*

```cs
builder.AddSSO(typeof(Program).Assembly.FullName); // This can change, depending on which overrited method you want to use.
builder.Services.ConfigureMinimalAPIServices();
```

Inside your app, Add the `ConfigureApiRoutes()` after `UseRouting()`
```cs
app.ConfigureApiRoutes(); // This can change depending on the routes you want to use.
```

### Appsettings.json
---

Inside your appsettings you should add your connectionstring. <br />
If you are already using the **DefaultConnection** for another context, you can always change it to something else. <br />
*Remember, if you change this you have to tell AddSSO() that you are using a different connectionstring.*
```json
"ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DATABASE;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

#### Adding roles
If you want to add roles, you can add it to the `Appsettings.json` and it will automatically create it and add it to the user.
```json
  "Roles": {
    "Name": "Role name"
  }
```

### Launchsettings.json
You can find the .json file under `Properties`

---

Beacuse we dont want to remote into our server and add a ned WSFed user, we are just using the same over and over. <br />

Change the <u>profiles -> https -> applicationUrl</u> to `https://localhost:7003;http://localhost:HTTP-PORT` <br />
*Just to be sure the HTTP-PORT to the port you are using at <u>profiles -> http -> applicationUrl</u>*

*It should look something like this*
```json
"https": {
    "commandName": "Project",
    "dotnetRunMessages": true,
    "launchBrowser": true,
    "applicationUrl": "https://localhost:7003;http://localhost:HTTP-PORT",
    "environmentVariables": {
    "ASPNETCORE_ENVIRONMENT": "Development"
    }
},
```

### User state
---

There is 3 methods that you need to know, when working with identity those are:
| Method                      |
| --------------------------- |
| AuthenticationStateProvider |
| UserManager<IdentityClass>  |
| RoleManager<IdentityClass>  |

There is provided examples of using them for both razor pages and Blazor WebAssembly/Server

<details> <summary>Examples for Blazor WebAssembly/Server</summary>

First of all you need to inject `AuthenticationStateProvider` into your component,

`ClaimsPrincipal` provides the following claims:
| Claim Type     | Short desc                                |
| -------------- | ----------------------------------------- |
| nameidentifier | id of the user                            |
| name           | username of the user                      |
| emailaddress   | email of the user                         |
| SecurityStamp  | Security stamp                            |
| role           | there may be more than 1 role of the user |
|                |                                           |

#### *Get the authentication state example*
```cs
@using System.Security.Claims
@using Microsoft.AspNetCore.Identity
@inject AuthenticationStateProvider authprovider

@code {
    ClaimsPrincipal? User { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Get the authentication state and set our User to the found User.
        User = (await authprovider.GetAuthenticationStateAsync()).User;
    }
}
```
<u>AuthenticationStateProvider</u> provides you with the user that is logged in (if any), you can check wether or not the user is logged in and get their claims using it.

#### *Get user from database example*
```cs
@using System.Security.Claims
@using Microsoft.AspNetCore.Identity
@inject AuthenticationStateProvider provider
@inject UserManager<IdentityUser> userManager

@code {
    private ClaimsPrincipal? User { get; set; }

    protected override async Task OnInitializedAsync()
    {
        User = (await provider.GetAuthenticationStateAsync()).User;

        IdentityUser? foundUser = await userManager.GetUserAsync(User);
    }
}
```
<u>UserManager</u> provides a lot of methods, though you have to tell it what class you are using, so if you made a custom class instead of the built in IdentityUser, you should it the custom class.


#### *Check if role exists example*
```cs
@using System.Security.Claims
@using Microsoft.AspNetCore.Identity
@inject AuthenticationStateProvider provider
@inject RoleManager<IdentityRole> roleManager

@code {
    private ClaimsPrincipal? User { get; set; }

    protected override async Task OnInitializedAsync()
    {
        User = (await provider.GetAuthenticationStateAsync()).User;

        bool doesExist = await roleManager.RoleExistsAsync("RoleName");
    }
}
```
<u>RoleManager</u> provides a lot of methods, though you have to tell it what class you are using, so if you made a custom class instead of the built in IdentityRole, you should give it the custom class.

</details>

### Production
---

If you to put your project into production with the nuget package, then you can change to Wtrealm.
This is done to identify what site is trying to access the adfs server.

```cs
builder.AddSSO(adfsSettings: x =>
{
    x.MetadataAddress = "https://`Your Hostname`/FederationMetadata/2007-06/FederationMetadata.xml";
    x.Wtrealm = YOUR_URL;
});
```

Remote to our ADFS server using the IP `Your ADFS Server IP` and open `AD FS Management`. </br >
Open the folder `Relying party trusts` and click on `Add relying party trust...` under Actions, then click next. </br >
When the wizard has opened choose `Claims aware`, then choose `Enter data about the-...` then click next.</br >
Give it a name, that fits the name of your project then click next. </br >
Continue until you reach the step <u>Configure URL</u> and Enable support for WS-Federation, and enter the url that the project is going to use, then click next until you finish the wizard. </br >

Set Claim rule template to `Send LDAP Attributes as Claims` and click next. </br >
Add a new rule named `SSO`. And add the following configuration. </br >
Set Attribute store to `Active Directory`.
| LDAP Attribute                             | Outgoing Claim |
| ------------------------------------------ | -------------- |
| E-Mail-Addresses                           | E-Mail Address |
| SAM-Account-Name                           | Name ID        |
| Given-Name                                 | Given Name     |
| Surname                                    | Surname        |
| Token-Groups - Qualified by Domain Name    | Group          |
</br >

Set Claim rule template to `Send LDAP Attributes as Claims` and click next. </br >
Add a new rule named `EmailSetup`. And add the following configuration.</br >
Set Attribute store to `Active Directory`. </br >
| LDAP Attribute   | Outgoing Claim |
| ---------------- | -------------- |
| E-Mail-Addresses | E-Mail Address |
| SAM-Account-Name | Name ID        |
| Given-Name       | Given Name     |
| Surname          | Surname        |


