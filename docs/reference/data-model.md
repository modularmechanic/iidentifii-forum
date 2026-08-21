# Data model

Who this is for: developers and testers.
What you'll get: what is stored, how it relates, and which rules the database itself enforces.

## Tables

### Users

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | Primary key, time-ordered |
| `Username` | citext, max 32 | Unique, compared without regard to case |
| `Email` | citext, max 254 | Unique, compared without regard to case |
| `PasswordHash` | text | PBKDF2, never the password itself |
| `Role` | text | `Member` or `Moderator`, stored as a name |
| `EmailVerifiedAt` | timestamptz, null | Null until the address is confirmed |
| `CreatedAt` | timestamptz | |

### UserTokens

One-time secrets sent by email. Only the hash is stored.

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | Primary key |
| `UserId` | uuid | Cascades on delete |
| `Purpose` | text | `EmailVerification`, `TwoFactor` or `PasswordReset` |
| `SecretHash` | text, max 128 | |
| `ExpiresAt` | timestamptz | |
| `ConsumedAt` | timestamptz, null | Set once spent |
| `FailedAttempts` | integer | Five wrong guesses spend the token |

### Posts

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | Primary key |
| `AuthorId` | uuid | Cascades on delete |
| `Title` | text, max 200 | |
| `Body` | text, max 10000 | |
| `CreatedAt` | timestamptz | Indexed; the list orders by it |
| `UpdatedAt` | timestamptz, null | Null while unedited |

### Comments

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | uuid | Primary key |
| `PostId` | uuid | Cascades on delete |
| `AuthorId` | uuid | Cascades on delete |
| `Body` | text, max 2000 | |
| `CreatedAt` | timestamptz | Indexed with `PostId` |
| `UpdatedAt` | timestamptz, null | |

### PostLikes

| Column | Type | Notes |
| --- | --- | --- |
| `PostId`, `UserId` | uuid | **Composite primary key** |
| `CreatedAt` | timestamptz | |

### PostTags

| Column | Type | Notes |
| --- | --- | --- |
| `PostId`, `Tag` | uuid, text | **Composite primary key** |
| `TaggedByUserId` | uuid | Restricted on delete, so history survives |
| `CreatedAt` | timestamptz | |

## Rules the database enforces

| Rule | How |
| --- | --- |
| One like per member per discussion | Composite key on `PostLikes` |
| One flag of a kind per discussion | Composite key on `PostTags` |
| Usernames and addresses are unique regardless of case | `citext` plus a unique index |
| Deleting a discussion removes its replies, likes and flags | Cascading foreign keys |

A unique-index violation is translated into a conflict response rather than being avoided by
reading first, which would still race under concurrent requests.

## Indexes

| Table | Index | Serves |
| --- | --- | --- |
| Posts | `CreatedAt` | Ordering the list |
| Posts | `AuthorId` | Filtering by author |
| Comments | `PostId, CreatedAt` | Reading one discussion's replies in order |
| PostTags | `Tag` | Filtering by flag |
| UserTokens | `UserId, Purpose` | Finding the newest usable token |
| Users | `Username`, `Email` | Sign-in and uniqueness |

## Changing it

Schema changes are made by editing the entities and their configurations, then generating a
migration with the Entity Framework tools:

```bash
dotnet ef migrations add <Name> --project backend/src/Forum.Infrastructure --startup-project backend/src/Forum.Api --output-dir Persistence/Migrations
```

Migrations are never hand-written, and never contain hand-written SQL.
