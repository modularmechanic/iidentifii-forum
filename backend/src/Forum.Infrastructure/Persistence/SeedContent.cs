namespace Forum.Infrastructure.Persistence;

/// <summary>The sample discussions the seeder writes, so the data itself stays out of the logic.</summary>
internal static class SeedContent
{
    internal sealed record SeedPost(
        string AuthorUsername,
        string Title,
        string Body,
        int DaysAgo,
        int LikeCount,
        bool IsFlagged,
        string[] Replies);

    /// <summary>
    /// Ordered newest to oldest: a larger <c>DaysAgo</c> is further in the past. Like counts are assigned round-robin from the
    /// members who are not the author, so nobody likes their own discussion.
    /// </summary>
    internal static readonly SeedPost[] Posts =
    [
        new("bob", "Liveness webhook retries: should the callback carry an idempotency key?",
            "We see the same verification-complete callback delivered twice under a network partition. Before we build deduplication on our side: does the platform guarantee at-least-once or exactly-once delivery?\n\nThe delivery identifier looks stable across both attempts, so we could key on it, but I would rather confirm than assume.",
            1, 14, true,
            ["At least once. We deduplicate on the delivery identifier with a 24 hour window and it has been solid for a year.",
             "Same here. Retries back off exponentially up to six attempts, so your window needs to cover roughly two hours.",
             "Worth adding: the identifier is stable per delivery, not per verification. Two verifications for the same person get different ones."]),

        new("carol", "SDK v4 on iOS 18: the camera permission prompt never appears",
            "Capture fails silently on a clean install. Adding the camera usage description after first launch appears to fix it, which does not match the integration guide.\n\nHas anyone confirmed this is required? Our QA cannot reproduce it on iOS 17.",
            2, 2, true,
            ["The usage description has always been required at build time, not after first launch. If it works after, something else is caching the permission state.",
             "Check whether your build strips the property list entry in release configuration. That bit us."]),

        new("alice", "Rate limits on the sandbox verification endpoint",
            "The sandbox starts returning 429 after roughly thirty calls a minute, which is well below the documented figure. Is the documented number production only?",
            3, 5, false,
            ["Sandbox is deliberately tighter. Production matches the documentation.",
             "You can request a temporary raise for load testing, but not on sandbox."]),

        new("bob", "Document capture confidence threshold: 0.85 or 0.9 for smart identity cards?",
            "Sharing false-reject numbers across four thousand verifications after lowering the threshold. Short version: 0.85 halved manual review without a measurable rise in fraud.\n\nHappy to share the breakdown by document type if it is useful.",
            6, 31, false,
            ["This matches what we saw. The gain drops off sharply below 0.8 though.",
             "Please do share the breakdown. We are mid-review on exactly this.",
             "Did you split it by capture device? Older Android cameras skewed ours."]),

        new("carol", "What is the minimal set of fields worth keeping for an audit trail?",
            "We currently store the full response payload, which feels heavier than it needs to be under our retention policy. Is there a recommended minimal set?",
            7, 3, false,
            ["Reference, outcome, timestamp and the document type carried us through two audits.",
             "Keep the reference above all. Everything else can be re-derived from the platform if you still have it."]),

        new("alice", "Signature verification in Go: a working example",
            "The documented example is in Node. Here is the equivalent in Go, including the constant-time comparison, since a naive equality check leaks timing.",
            9, 9, false,
            ["Useful, thank you. The constant-time detail catches people out.",
             "Consider publishing this as a gist so it can be linked from the integration guide."]),

        new("bob", "Passive against active liveness on low-end Android devices",
            "On devices below two gigabytes of memory, active liveness times out more often than it completes. Has anyone measured passive on the same class of hardware?",
            11, 7, false,
            ["Passive is far more forgiving there. We switched below a hardware score threshold and completion rose noticeably."]),

        new("carol", "Sandbox keys do not work in production",
            "Posting this because it cost us an afternoon: sandbox and production keys are entirely separate, and the failure is a generic 401 rather than anything that says which environment you are in.",
            13, 11, false,
            ["The error could name the environment. Worth raising as feedback.",
             "We prefix the key in configuration with the environment name to make this obvious at a glance."]),

        new("alice", "Handling a verification that stays pending",
            "Roughly one in a thousand verifications sits in pending for longer than an hour. What is the recommended action: poll, wait for the callback, or treat it as failed?",
            15, 4, false,
            ["Wait for the callback. Polling adds load without changing the outcome.",
             "We treat anything beyond four hours as needing a human, and that has been about right."]),

        new("bob", "Testing the integration without burning real documents",
            "Which sample documents are safe to use repeatedly in sandbox without tripping duplicate detection?",
            18, 6, false,
            ["The published sample set is exempt from duplicate detection in sandbox.",
             "Generate a fresh reference per run and you will not hit it anyway."]),

        new("carol", "Retry storms after a platform incident",
            "During last month's incident our queue built up and then retried everything at once when the platform recovered, which made the recovery slower for us.",
            21, 8, false,
            ["Jitter your backoff. A fixed schedule guarantees a thundering herd.",
             "We cap concurrent retries at a fraction of normal throughput during recovery."]),

        new("alice", "Which fields are safe to log?",
            "Trying to keep useful diagnostics without logging anything that would be a problem under our data policy.",
            24, 2, false,
            ["Reference and outcome are safe. Never log the payload or the captured images.",
             "We hash the reference in logs and keep the mapping in the database only."]),

        new("bob", "Confirming a callback actually came from the platform",
            "Beyond the signature, is there a source address range worth allowing, or is the signature considered sufficient?",
            27, 5, false,
            ["The signature is sufficient. Address ranges change and pinning them will page you at some point."]),

        new("carol", "Onboarding drop-off at the document capture step",
            "About a fifth of our users abandon at document capture. Curious what others see and whether guidance copy moved the number.",
            30, 12, false,
            ["Ours dropped by roughly a third after adding a worked example of a good capture.",
             "Lighting guidance before the camera opens made the biggest difference for us.",
             "Watch the retake loop. Two failed attempts and most people give up."]),

        new("alice", "Verification reference format",
            "Is the reference format guaranteed, or should we treat it as an opaque string of unknown length?",
            34, 3, false,
            ["Treat it as opaque. The format has changed once already."]),

        new("bob", "Callbacks arriving before the API response",
            "We occasionally receive the callback before the original request has returned, which our code did not expect.",
            38, 7, false,
            ["This is expected under load. Make the callback handler independent of the request path.",
             "We write a placeholder row before calling out, so the callback always has something to update."]),

        new("carol", "Recommended timeout for the verification call",
            "The default client timeout of thirty seconds seems short for a slow mobile connection.",
            42, 4, false,
            ["Sixty seconds is the usual figure. Below thirty you will see avoidable failures."]),

        new("alice", "Keeping the integration guide and the API in step",
            "Is there a changelog for the API separate from the SDK releases? We were caught by a field becoming optional.",
            47, 6, false,
            ["There is a changelog, though it is easy to miss. Subscribe to it rather than watching releases."]),

        new("bob", "Load testing against sandbox",
            "What is considered acceptable load against sandbox before it becomes antisocial?",
            52, 2, false,
            ["Ask first for anything sustained. Short bursts are fine."]),

        new("carol", "Storing the captured image on our side",
            "Our compliance team wants a copy of the captured image. Is that supported, and is it wise?",
            58, 9, true,
            ["Supported, but think hard about it. Holding the image moves a lot of obligation onto you.",
             "We store a reference only, and pull the image on demand during a dispute. Far less to protect."]),
    ];
}
