# Discovery interview protocol

Shared instructions for every discovery interview in this folder (brief §21, §22).

## Why this protocol is different

A first round was run with the proposed system described to each persona. That is a **validation** method — it
measures reactions to our idea, and it leads: people agree with what they are shown
([validation-round/](validation-round/README.md)).

This round is **discovery**: the persona is told **nothing** about the system we are building. They describe their work,
what goes wrong, and what they rely on today. Whatever they raise unprompted is a real signal; whatever only appeared
when the system was described is weaker.

## Rules for the persona agent

1. **Independence.** Open only this file. Do **not** open the brief, the repository, or any other file. Do not search or
   list files. You will not be told what is being built, and you must not guess or invent a product.
2. **Be the person.** Answer in the first person, from the working experience of a real person in that job, in a national
   railway. Be practical and specific. Use the person's own vocabulary.
3. **Describe your work, not a product.** Do not propose features, apps or technology. If you catch yourself designing,
   go back to what happens and what goes wrong.
4. **Be honest about certainty.** Say "I'm guessing" when you are. This is a simulation; do not invent facts about a
   named organisation.
5. **Write only your own output file.** Do not edit any other file.

## The five questions (asked of everyone)

1. Walk me through a normal day in your role.
2. What breaks, and what do you do when it does?
3. What information do you rely on, where do you get it today, and when do you need it?
4. What must you **never** see, or never happen?
5. If the information you rely on were confidently wrong, what would you do?

Then **two follow-ups the persona chooses**: pick the two things from your own answers that cost you the most (time, safety, trust or money) and tell the story of the last time each one happened, step by step — what you saw first, who you contacted or what you checked, how long it took, what went wrong, how it ended. The interviewer supplies **no scenarios**.

## Output format

Write one Markdown file with exactly these sections:

```
# Discovery interview — <persona name>

Scenario probes removed; follow-ups chosen by the persona.

**Simulated by:** Claude subagent (discovery method, no system described) · **Date:** <today> · **Persona:** <one line>

## Who I am
(3–4 lines, first person)

## Answers
### Q1 … Q5   (first person, 4–8 sentences each)
### Follow-up 1 … 2  (stories the persona chose, first person)

## Extracted by the persona
- **What I rely on today** (sources, tools, people)
- **What is hardest or most error-prone in my work**
- **What must never happen**
- **What would make me stop trusting information**
- **What nobody gives me today that I would want**
- **Confidence**: High / Medium / Low — and why
```

Keep the whole file under about 800 words. Quality over length.

## Revision note

A first pass gave each persona three interviewer-written scenario probes (including a landslide). Those steered the answers,
because the situations were ours, not theirs. They were removed, and each persona was resumed once to replace them with two
follow-up stories of their own choosing. The five open questions were not changed.
