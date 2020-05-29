# ADOBlame
Find out who changed a tag or field in AzureDevOps workitems

Setup:
* Create a file called `adoblame.json`:
```json
{
    "Uri": "https://myCompany.visualstudio.com/",
    "Username": "myUsername@myCompany.com",
    "OAuthToken": "optional OAuth token (will use integrated AAD if absent)",
    "ShowDetails": false
}
```

Usage:
* `adoblame /id 12345 /tag MyTag` - Searches for the last person who touched the tag `MyTag` in workitem #12345
* `adoblame /id 12345 /tag System.Title="theTitle"` - Searches for the last person who set the `Title` field to `theTitle`

