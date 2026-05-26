# Privacy Policy

**Last updated:** May 26, 2026

This Privacy Policy explains what data **SyncMemoBot** (the "Bot") collects, how
it is used, and how it is stored. By using the Bot, you agree to the practices
described here.

## 1. Data We Collect

The Bot collects only the minimum data needed to schedule and deliver reminders:

- **Discord user ID** — to identify who scheduled a reminder and where to deliver
  a direct-message reminder.
- **Discord channel ID** — when a reminder is scheduled for a channel, to deliver
  the reminder to that channel.
- **Reminder content** — the text you provide for the reminder.
- **Scheduled time** — the date and time at which the reminder should be sent.
- **Locale** — your Discord locale is read at command time to choose the response
  language (English or Portuguese). It is not stored separately.

The Bot does **not** collect message history, does not read messages other than
the slash commands you explicitly issue, and does not collect email addresses,
payment information, or any special categories of personal data.

## 2. How We Use Your Data

Collected data is used solely to:

- Schedule a reminder and deliver it at the requested time.
- Address the reminder to the correct user or channel.
- Respond in your preferred language.

Your data is **not** sold, rented, shared with third parties for marketing, or
used for advertising or profiling.

## 3. Data Storage and Retention

Reminder data is stored only as part of the scheduled job until the reminder is
delivered. Job data is persisted locally (in a SQLite-backed job store) on the
infrastructure operated by the Bot's owner.

- Pending reminders are retained until they are delivered.
- After a reminder is delivered, the associated job data is retained only for as
  long as the job store keeps completed/expired job records, after which it is
  removed.
- The Bot does not maintain a separate long-term database of user profiles.

## 4. Data Sharing

Reminder content is delivered through Discord to the destination you choose (your
direct messages or a channel). Once delivered through Discord, the content is
subject to Discord's own data handling and retention. The Bot relies on the
Discord platform; your use of Discord is governed by Discord's
[Privacy Policy](https://discord.com/privacy).

## 5. Data Security

Reasonable measures are taken to protect stored data, but no method of
transmission or storage is completely secure. The Bot is a personal project and
is provided without guarantees regarding security or availability.

## 6. Your Choices and Rights

- You can avoid providing data by not issuing reminder commands.

## 7. Children's Privacy

The Bot is not directed at children below the minimum age required to use Discord
in their country. Do not use the Bot if you do not meet Discord's age
requirements.

## 8. Changes to This Policy

This Privacy Policy may be updated from time to time. The "Last updated" date
above reflects the most recent revision. Continued use of the Bot after changes
take effect constitutes acceptance of the revised Policy.

## 9. Contact

For privacy questions, contact the owner:

- **Owner:** Gabriel Azevedo
- **Email:** azevedogabicodes@gmail.com
