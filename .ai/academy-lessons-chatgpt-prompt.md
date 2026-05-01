# ChatGPT Prompt: Food Diary Academy Lessons

Use this prompt to generate lessons for the Food Diary academy import.

```text
You are writing nutrition education lessons for the Food Diary app.

Return only valid JSON. Do not wrap it in Markdown. Do not include comments or explanatory text.

Output schema:
{
  "version": 1,
  "lessons": [
    {
      "title": "string, max 256 characters",
      "content": "string, HTML fragment, max 65536 characters",
      "summary": "string or null, max 512 characters",
      "locale": "ru or en",
      "category": "NutritionBasics | Macronutrients | Micronutrients | MealTiming | MindfulEating | WeightManagement | Hydration | FoodQuality | CookingTips",
      "difficulty": "Beginner | Intermediate | Advanced",
      "estimatedReadMinutes": 1,
      "sortOrder": 0
    }
  ]
}

Content rules:
- Generate {NUMBER_OF_LESSONS} lessons in {LOCALE}.
- Audience: adults who want practical, non-medical nutrition guidance.
- Tone: clear, calm, practical, not salesy.
- Use only these HTML tags in content: <h2>, <h3>, <p>, <ul>, <ol>, <li>, <strong>, <em>.
- Do not include <html>, <body>, scripts, styles, iframes, images, inline event handlers, or external links.
- Do not make medical claims, diagnoses, or treatment recommendations.
- Add a short disclaimer inside the content only when the lesson touches medical conditions, pregnancy, eating disorders, supplements, or chronic disease.
- Keep each lesson self-contained and useful without referring to other lessons.
- Summary should be one concise sentence.
- Sort order should start at {START_SORT_ORDER} and increase by 10.

Lesson brief:
{DESCRIBE_TOPICS_HERE}
```

Example request:

```text
NUMBER_OF_LESSONS = 5
LOCALE = ru
START_SORT_ORDER = 100
DESCRIBE_TOPICS_HERE =
- Basics of balanced meals
- Protein at breakfast
- Hydration habits
- Reading food labels
- Mindful eating at dinner
```
