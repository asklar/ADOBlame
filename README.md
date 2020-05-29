# ADOBlame
Find out who changed a tag or field in AzureDevOps workitems

Usage:
* Edit `adoblame.json`:
```json
{
    "Uri": "https://myCompany.visualstudio.com/",
    "Username": "myUsername@myCompany.com",
    "OAuthToken": "optional OAuth token (will use integrated AAD if absent)"
}
```
* `adoblame /id 12345 /tag MyTag`

