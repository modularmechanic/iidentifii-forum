# Configure email and rate limits

Who this is for: whoever runs the forum.
What you'll get: the settings that decide where mail goes and how hard the API can be pushed.

## Email

```json
{
  "Email": {
    "Enabled": true,
    "Host": "localhost",
    "Port": 1025,
    "FromAddress": "no-reply@forum.local",
    "FromName": "iiDENTIFii Forum",
    "Username": null,
    "Password": null,
    "UseStartTls": false
  }
}
```

| Setting | Meaning |
| --- | --- |
| `Enabled` | `false` drops messages instead of sending them, and logs nothing about them |
| `Host`, `Port` | Where to hand mail over. Locally this is Mailpit on 1025 |
| `Username`, `Password` | Give both or neither; a local mail catcher wants neither |
| `UseStartTls` | Turn on for a real mail service |

Locally, everything the forum sends is readable at http://localhost:8025.

## Links and codes

```json
{
  "Tokens": {
    "Pepper": "at least 32 characters, from a secret store",
    "LinkLifetime": "01:00:00",
    "CodeLifetime": "00:10:00",
    "ResendCooldown": "00:01:00"
  }
}
```

`Pepper` is mixed into every stored token hash, so a leaked database is not enough to forge a
token. It is required, must be at least 32 characters, and is checked when the application starts:
a missing or short value stops it rather than weakening it quietly. Development supplies its own,
which is published in the repository and must never be used anywhere else.

The three durations are checked at startup too, and each must be longer than zero: a lifetime of
zero issues links nobody can use, and a cooldown of zero removes the resend guard. The emails quote
whatever is configured here, so changing a lifetime changes what the message says.

## Rate limits

```json
{
  "RateLimiting": {
    "GlobalPermitsPerMinute": 120,
    "AuthenticationPermitsPerMinute": 10
  }
}
```

Both count requests per caller address per minute. The stricter one applies to every route under
`/api/v1/auth`, because each of those either checks a secret or sends mail. A refused request
returns 429 with `Retry-After`.

## Where the links point

```json
{
  "App": {
    "PublicUrl": "http://localhost:5173"
  }
}
```

The address a person's browser uses, which is not always where the API listens. Confirmation and
reset links are built from it.
