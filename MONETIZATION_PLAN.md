# FoodDiary Monetization Plan

## Goal
Define a practical first monetization model for FoodDiary based on the current product direction:

- food logging
- goals and macros
- weight tracking
- hydration tracking
- fasting tracking
- adaptive coaching
- dietologist collaboration
- future AI-powered guidance

The monetization model should protect the core daily habit in the free version and place interpretation, personalization, and higher-value support into the paid version.

## Product Positioning

### Free
FoodDiary helps users build a daily nutrition tracking habit and monitor core progress.

### Premium
FoodDiary helps users understand their data, get personalized recommendations, and improve results faster.

Core principle:

- `Free` shows the data
- `Premium` explains what to do next

## Free vs Premium

### Free
The free tier should remain useful enough for habit formation.

Recommended free features:

- food logging
- meal diary
- daily calories and macros
- basic nutrition goals
- weight tracking
- hydration tracking
- basic fasting tracking
- basic history
- simple dashboard for today and recent progress

The free version should let users:

- log consistently
- see their daily totals
- track core body metrics
- understand basic progress without advanced interpretation

### Premium
The premium tier should contain the features that transform raw tracking into coaching and decision support.

Recommended premium features:

- adaptive coach
- TDEE insights
- suggested calorie target adjustments
- weekly check-in with recommendations
- fasting insights
- advanced analytics and trend interpretation
- long-range progress views
- dietologist features
- AI summaries and AI recommendations

The premium version should answer questions like:

- is my target still working
- should I increase or decrease calories
- what trend matters right now
- what fasting patterns are repeating
- what actions should I take next

## Feature Classification

### Keep in Free
These features support the core habit and should not be paywalled aggressively:

- add and edit food entries
- browse daily diary
- see daily calorie total
- see daily macros
- set basic goals
- add and view weight entries
- add and view water entries
- start and track fasting sessions
- view basic recent history

### Move to Premium
These features create clear upgrade value and are strong monetization candidates:

- adaptive recommendations
- coaching explanations
- weekly review summaries
- suggested goal changes
- fasting pattern insights
- symptom-based fasting warnings and recurring patterns
- extended analytics windows
- richer dashboards and trends
- dietologist invitation and client sharing
- recommendation inbox for coaching workflows
- AI-generated summaries and suggestions

## Dietologist Positioning
Dietologist functionality should be treated as a premium feature.

Reasons:

- it has high perceived value
- it is not needed by every user
- it feels like a professional or semi-professional upgrade
- it strengthens the premium package beyond generic analytics

Recommended premium dietologist scope:

- invite dietologist
- configure sharing permissions
- client dashboard access for dietologist
- recommendations exchange
- dietologist-specific notifications

## AI Positioning
AI should not be the only reason to pay.

Recommended approach:

- include AI inside `Premium`
- position AI as an enhancement on top of real user data
- avoid a standalone "AI subscription" at launch

Good AI premium use cases:

- weekly AI summary
- AI explanation of progress trends
- AI explanation of calorie target changes
- AI review of recent diary patterns
- AI suggestions based on weight, intake, goals, and fasting data

Weak positioning to avoid:

- charging only for chat
- selling AI without a strong data-driven product foundation

## Paywall Strategy

### Principle
Do not block the user from forming a tracking habit.

Use the paywall where the user is already close to insight, recommendation, or higher-value support.

### Best Paywall Entry Points

#### 1. After initial data accumulation
Show a paywall after the user has enough data for meaningful analysis.

Recommended trigger:

- after `5-7` days of logging
- or when enough data exists for adaptive coach insight

Recommended message:

- you now have enough data for a personalized analysis
- unlock FoodDiary Premium to get recommendations based on your real progress

#### 2. Adaptive Coach card
Show a teaser card even to free users.

Recommended free teaser:

- show that an insight exists
- hide the full recommendation and action

Example:

- your recent progress suggests your calorie target may need adjustment
- unlock Premium to see your recommendation

#### 3. Weekly Check-In
Let free users see a lightweight summary, but reserve deeper interpretation for Premium.

Free:

- basic weekly overview

Premium:

- recommendation action
- reasoning
- suggested calorie adjustment

#### 4. Fasting page
Keep fasting logging free, but reserve interpretation for Premium.

Free:

- track sessions
- view history

Premium:

- repeated symptom insights
- tolerance patterns
- caution insights for current session

#### 5. Dietologist flow
The paywall can appear when the user:

- tries to invite a dietologist
- tries to access shared-client workflows
- tries to open coaching collaboration features

## Soft vs Hard Limits

### Soft Limits
Prefer soft restrictions where possible because they preserve trust and showcase value.

Recommended soft limits:

- show a locked recommendation preview
- show that analytics exist but blur deeper interpretation
- limit analytics to shorter time windows in free
- allow a basic weekly summary but lock detailed coaching output

### Hard Limits
Use hard restrictions for premium features with clear business value.

Recommended hard limits:

- adaptive coach actions
- suggested calorie target
- fasting insights
- dietologist features
- AI features
- advanced long-range trend analysis

## Initial Packaging

### Recommended Free Package

- food logging
- daily calories and macros
- basic goals
- weight tracking
- hydration tracking
- basic fasting tracking
- recent history
- simple dashboard

### Recommended Premium Package

- adaptive coach
- TDEE insight
- weekly check-in recommendations
- fasting insights
- advanced analytics
- AI summaries and suggestions
- dietologist tools

At launch, keep this as a single premium tier.

Avoid:

- multiple confusing subscription tiers
- a separate AI-only paid plan
- over-fragmenting premium into tiny upsells

## Pricing Recommendation
Suggested starting offer for the current product direction:

- `$7.99/month`
- `$49.99/year`
- `7-day free trial`

Why this range fits:

- below the common `$10/month` psychological threshold
- strong enough to signal real product value
- room for future price increases after feature maturity improves

## Messaging Direction

### Free Message
Track your nutrition and progress every day.

### Premium Message
Get personalized insights, recommendations, and guidance based on your real data.

### Sample Paywall Headline
Understand not only what you eat, but what to change next.

### Sample Paywall Subheadline
FoodDiary Premium analyzes your nutrition, weight, goals, and fasting patterns to help you make better decisions with more confidence.

### Premium Benefit Bullets

- adaptive coach and TDEE insights
- weekly progress review with recommendations
- fasting insights and symptom patterns
- advanced analytics and trend interpretation
- dietologist collaboration tools
- AI summaries based on your real data

## Launch Rules

### Do

- keep core diary behavior free
- monetize insight and guidance
- show premium value only after enough data exists
- use premium as a performance upgrade, not just a feature bundle

### Do Not

- paywall basic logging
- make AI the only reason to subscribe
- show aggressive paywalls before habit formation
- split the first paid offer into too many plans

## Recommended Next Steps

1. Convert this plan into explicit feature flags for frontend and backend.
2. Define exact premium entry points in the Angular UI.
3. Create paywall copy in both English and Russian.
4. Decide which upcoming MVP features must launch as premium-only.
5. Instrument conversion events for paywall views, trial starts, and subscription purchases.
