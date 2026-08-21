# Test accounts and sample content

Who this is for: testers, assessors, and anyone who wants something to click on straight away.
What you'll get: every account the seeder creates, what it can do, and exactly what content
exists.

## The accounts

Eleven accounts are written into an empty database. All are already confirmed, so they can sign in
without an inbox.

| Username | Email | Role | Writes discussions |
| --- | --- | --- | --- |
| `alice` | `alice@forum.local` | Member | 6 |
| `bob` | `bob@forum.local` | Member | 7 |
| `carol` | `carol@forum.local` | Member | 7 |
| `dave` | `dave@forum.local` | Member | no |
| `erin` | `erin@forum.local` | Member | no |
| `frank` | `frank@forum.local` | Member | no |
| `grace` | `grace@forum.local` | Member | no |
| `heidi` | `heidi@forum.local` | Member | no |
| `ivan` | `ivan@forum.local` | Member | no |
| `judy` | `judy@forum.local` | Member | no |
| `mod` | `mod@forum.local` | **Moderator** | no |

Every one of them uses the password:

```
Password123!
```

The seven members who write nothing supply the replies and the likes, so like counts differ from
one discussion to the next and ordering by popularity is worth looking at.

**These accounts exist for development and assessment only.** They share one published password,
which is why the API refuses to seed at all outside the Development environment, and logs the
refusal when asked.

Signing in still takes two steps for a seeded account. Read the code from Mailpit at
http://localhost:8025.

## What the seeder writes

| Thing | Count |
| --- | --- |
| Users | 11 |
| Discussions | 20 |
| Replies | 37 |
| Likes | 122 |
| Moderation flags | 3 |

Facts worth knowing when you write a test against it:

- Discussions are dated between 1 and 58 days ago, so date filtering has something to bite on.
- Likes per discussion run from 2 to 10. A member never likes their own, which is the same rule
  the API enforces, so the seed data does not contradict the domain.
- The number of members who are not the author caps a like count at 10, whatever the seed asks
  for.
- Three discussions carry `MisleadingOrFalse`, applied by `mod`.
- Every timestamp is relative to the moment of seeding, so a freshly seeded database always has
  recent content.

## When it runs

Seeding needs **both** conditions:

| Condition | Where it comes from |
| --- | --- |
| `Database:SeedOnStartup` is true | `appsettings.Development.json`, or the compose file |
| The environment is Development | `ASPNETCORE_ENVIRONMENT` |

It also does nothing at all once any user exists, so restarting never duplicates the content. Two
instances starting together are decided by the unique index on usernames: one wins and the other
finds nothing left to do.

## Starting over

```bash
docker compose down -v
docker compose up -d
```

The `-v` throws the database volume away, so the next start migrates and seeds from scratch.

## Accounts you make yourself

An account you register is *not* confirmed, and cannot sign in until you follow the link from
Mailpit. See
[create an account, confirm it, and reset a forgotten password](../how-to/create-account-verify-and-reset-password.md).

The Postman collection registers a fresh account on every run, using a generated username and
address, so it never collides with one you made by hand.
